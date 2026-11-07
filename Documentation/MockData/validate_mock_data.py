#!/usr/bin/env python3
"""Kiểm tra AdviseCare_Mock_Data.sql mà không cần SQL Server: nạp phần schema của AdviseCare_Database_Script.sql + dữ liệu mock vào SQLite
(FK, CHECK, unique có lọc), kiểm tra các quy tắc nghiệp vụ và giải mã lại mọi giá trị đã mã hoá.
Usage: python validate_mock_data.py ../AdviseCare_Database_Script.sql AdviseCare_Mock_Data.sql ../../AdviseCare_API/AdviseCare.API/appsettings.json"""
import base64, json, re, sqlite3, sys, datetime as dt
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.kdf.hkdf import HKDF

schema_path, data_path, appsettings = sys.argv[1], sys.argv[2], sys.argv[3]
NOW = '2026-11-05T03:00:00.000'
schema = open(schema_path, encoding='utf-8').read()
schema = schema[:schema.index('/* ---------- 4. Seed and mock data')]


def tsql_to_sqlite(text):
    out, i, n = [], 0, len(text)
    while i < n:
        ch = text[i]
        if ch == "'" or (ch == 'N' and i + 1 < n and text[i + 1] == "'" and (i == 0 or not text[i - 1].isalnum())):
            if ch == 'N':
                i += 1
            j = i + 1
            while True:
                if text[j] == "'" and j + 1 < n and text[j + 1] == "'":
                    j += 2
                    continue
                if text[j] == "'":
                    break
                j += 1
            out.append(text[i:j + 1])
            i = j + 1
        elif ch == '0' and text[i:i + 2] == '0x' and (i == 0 or not text[i - 1].isalnum()):
            j = i + 2
            while j < n and text[j] in '0123456789ABCDEFabcdef':
                j += 1
            out.append("X'" + text[i + 2:j] + "'")
            i = j
        elif ch in '[]':
            i += 1
        else:
            out.append(ch)
            i += 1
    return ''.join(out)


db = sqlite3.connect(':memory:')
db.execute('PRAGMA foreign_keys = ON')
# --- schema
for m in re.finditer(r"CREATE TABLE \[dbo\]\.\[(\w+)\] \((.*?)\n\);", schema, re.S):
    body = re.sub(r"--[^\n]*", "", m.group(2))
    body = re.sub(r"CONSTRAINT \[DF_\w+\] DEFAULT [^,\n]+?(?=,\n|\n|$)", '', body)
    body = tsql_to_sqlite(body)
    body = re.sub(r"UNIQUEIDENTIFIER|NVARCHAR\(\w+\)|DATETIME2\(3\)|TIME\(0\)", 'TEXT', body)
    body = re.sub(r"VARBINARY\(MAX\)", 'BLOB', body)
    body = re.sub(r"\bBIT\b|\bBIGINT\b|\bINT\b", 'INTEGER', body)
    body = body.replace('dbo.', '')
    db.execute(f"CREATE TABLE {m.group(1)} ({body}\n)")
for m in re.finditer(r"CREATE (UNIQUE )?INDEX (.*?);", schema):
    db.execute(tsql_to_sqlite('CREATE ' + (m.group(1) or '') + 'INDEX ' + m.group(2)).replace('dbo.', ''))
# --- data
data = open(data_path, encoding='utf-8-sig').read()
stmts = re.findall(r"^    (INSERT INTO \[dbo\]\.\[\w+\] .*?\);)$", data, re.S | re.M)
for s in stmts:
    try:
        db.execute(tsql_to_sqlite(s).replace('dbo.', ''))
    except Exception as e:
        print('INSERT FAILED:', e, s[:300])
        raise
fk = db.execute('PRAGMA foreign_key_check').fetchall()
print('statements', len(stmts), 'fk violations', len(fk))
assert not fk, fk[:5]
problems = []


def q(sql, *a):
    return db.execute(sql, a).fetchall()


def check(name, rows):
    if rows:
        problems.append((name, rows[:5], len(rows)))


# --- generic: no timestamp after NOW except known future columns
FUTURE_OK = {('CONSENTS', 'expiresAt'), ('EMERGENCYACCESSREQUESTS', 'scopeExpiresAt'), ('EXTERNALREFERRALS', 'followUpDueAt'),
             ('INTERVENTIONS', 'interventionDate'), ('ACADEMICTERMS', 'endDate'), ('ACADEMICTERMS', 'startDate'), ('INGESTIONJOBS', 'expectedStartTime')}
for (t,) in q("SELECT name FROM sqlite_master WHERE type='table'"):
    for col in [r[1] for r in q(f"PRAGMA table_info({t})")]:
        if (t, col) in FUTURE_OK:
            continue
        bad = q(f"SELECT {col} FROM {t} WHERE {col} LIKE '20__-__-__T%' AND {col} > ?", NOW)
        check(f'future {t}.{col}', bad)
# --- seed accounts are clean
SEEDS = ('D2AA2929-437D-50E2-A61D-40EA2B09010E', '4FA71983-548F-5C26-BEFE-0E089DF4956F', '16C05C9C-133D-5FAC-B287-37C15764B71E', 'B6944BEC-089A-5878-9C34-EED3583DAABF',
         '0CAA0436-8CFC-5AB5-B05E-025D8C0280C5', '6341514A-BC92-5729-9218-7DA322CDCD4E', '3978D886-B5A9-5108-89A0-5DD87AA154B4')
ph = ','.join('?' * 7)
for t, col in [('NOTIFICATIONS', 'accountId'), ('ACTIVITYLOGS', 'accountId'), ('SESSIONS', 'accountId'), ('CASES', 'studentId'), ('CASES', 'advisorId'),
               ('CONSENTS', 'studentId'), ('ACCESSLOGS', 'accessorId'), ('STUDENTDATA', 'studentId')]:
    check(f'seed in {t}.{col}', q(f"SELECT {col} FROM {t} WHERE {col} IN ({ph})", *SEEDS))
# --- cases
check('closed status/closedAt', q("SELECT caseId FROM CASES WHERE (status='closed') <> (closedAt IS NOT NULL) OR (status='closed') <> (currentStage='closed')"))
check('pending with advisor', q("SELECT caseId FROM CASES WHERE (currentStage='pending') <> (advisorId IS NULL)"))
check('history last != current', q("""SELECT c.caseId, c.currentStage, h.toStage FROM CASES c JOIN CASESTAGEHISTORIES h ON h.caseId=c.caseId
    WHERE h.changedAt = (SELECT MAX(changedAt) FROM CASESTAGEHISTORIES x WHERE x.caseId=c.caseId) AND h.toStage <> c.currentStage"""))
check('history chain broken', q("""SELECT a.caseId, a.toStage, b.fromStage FROM CASESTAGEHISTORIES a JOIN CASESTAGEHISTORIES b ON a.caseId=b.caseId
    AND b.changedAt = (SELECT MIN(changedAt) FROM CASESTAGEHISTORIES x WHERE x.caseId=a.caseId AND x.changedAt > a.changedAt) WHERE a.toStage <> b.fromStage"""))
check('history dup time', q("SELECT caseId, changedAt, COUNT(*) FROM CASESTAGEHISTORIES GROUP BY caseId, changedAt HAVING COUNT(*)>1"))
check('history before open', q("SELECT h.caseId FROM CASESTAGEHISTORIES h JOIN CASES c ON c.caseId=h.caseId WHERE h.changedAt < c.openedAt"))
check('first history not from pending', q("""SELECT caseId FROM CASESTAGEHISTORIES h WHERE changedAt=(SELECT MIN(changedAt) FROM CASESTAGEHISTORIES x WHERE x.caseId=h.caseId) AND fromStage<>'pending'"""))
check('referred without referral', q("""SELECT caseId FROM CASES c WHERE currentStage='referred' AND NOT EXISTS (SELECT 1 FROM REFERRALS r WHERE r.caseId=c.caseId AND (r.status='pending' OR (r.status='accepted' AND r.toAccountId IN
    (SELECT accountId FROM ACCOUNT_ROLE WHERE roleId='2843CEE3-64AE-5054-A8D9-A77F17AEB6F5'))))"""))
check('pending referral but not referred', q("SELECT r.caseId FROM REFERRALS r JOIN CASES c ON c.caseId=r.caseId WHERE r.status='pending' AND c.currentStage<>'referred'"))
check('2 pending referrals', q("SELECT caseId FROM REFERRALS WHERE status='pending' GROUP BY caseId HAVING COUNT(*)>1"))
check('2 pending change requests', q("SELECT caseId FROM CASECHANGEREQUESTS WHERE status='pending' GROUP BY caseId HAVING COUNT(*)>1"))
check('change request decided incomplete', q("SELECT caseChangeRequestId FROM CASECHANGEREQUESTS WHERE (status='pending') <> (decidedAt IS NULL) OR (status='pending') <> (programmeHeadId IS NULL)"))
check('approved change: advisor', q("""SELECT r.caseId FROM CASECHANGEREQUESTS r WHERE status='approved' AND NOT EXISTS (SELECT 1 FROM CASESTAGEHISTORIES h WHERE h.caseId=r.caseId AND h.reason LIKE 'Advisor changed from%')"""))
check('advisor type vs category', q("""SELECT c.caseId FROM CASES c JOIN SUPPORTCATEGORIES k ON k.supportCategoryId=c.supportCategoryId WHERE c.advisorId IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM ACCOUNT_ROLE ar WHERE ar.accountId=c.advisorId AND ar.roleId = CASE k.supportType WHEN 'academic' THEN '3ED661F9-DC4F-5353-A6E9-C4224A296BF1' ELSE 'BF84B96D-99E0-5D30-B618-F7B8196B56B5' END)
    AND NOT EXISTS (SELECT 1 FROM REFERRALS r WHERE r.caseId=c.caseId AND r.status='accepted' AND r.toAccountId=c.advisorId)"""))
check('note author not advisor', q("""SELECT n.caseNoteId FROM CASENOTES n WHERE NOT EXISTS (SELECT 1 FROM ACCOUNT_ROLE ar WHERE ar.accountId=n.authorId AND ar.roleId IN ('3ED661F9-DC4F-5353-A6E9-C4224A296BF1','BF84B96D-99E0-5D30-B618-F7B8196B56B5'))"""))
check('subject with wellbeing category', q("SELECT c.caseId FROM CASES c JOIN SUPPORTCATEGORIES k ON k.supportCategoryId=c.supportCategoryId WHERE k.supportType='wellbeing' AND c.subjectId IS NOT NULL"))
check('archived category/subject used in Fall', q("""SELECT c.caseId FROM CASES c JOIN SUPPORTCATEGORIES k ON k.supportCategoryId=c.supportCategoryId LEFT JOIN SUBJECTS s ON s.subjectId=c.subjectId
    WHERE k.status='archived' OR s.status='archived'"""))
check('intervention on case without accepted welfare referral', q("""SELECT i.caseId FROM INTERVENTIONS i WHERE NOT EXISTS (SELECT 1 FROM REFERRALS r WHERE r.caseId=i.caseId AND r.status='accepted' AND r.toAccountId=i.officerId)"""))
check('closed intervention flag', q("SELECT interventionId FROM INTERVENTIONS WHERE isClosed <> (status IN ('completed','cancelled'))"))
check('overdue flag', q("SELECT externalReferralId FROM EXTERNALREFERRALS WHERE isOverdue=1 AND NOT (status='sent' AND followUpDueAt <= ?)", NOW))
check('should be overdue', q("SELECT externalReferralId FROM EXTERNALREFERRALS WHERE isOverdue=0 AND status='sent' AND followUpDueAt <= ?", NOW))
check('escalation response', q("SELECT escalationId FROM ESCALATIONS WHERE (status='responded') <> (response IS NOT NULL)"))
# --- consents / erasure / keys
check('consent withdrawn fields', q("SELECT consentId FROM CONSENTS WHERE (status='withdrawn') <> (withdrawnAt IS NOT NULL)"))
check('consent history count', q("""SELECT c.consentId FROM CONSENTS c WHERE (SELECT COUNT(*) FROM CONSENTHISTORIES h WHERE h.consentId=c.consentId) <> CASE c.status WHEN 'withdrawn' THEN 2 ELSE 1 END"""))
check('erasure completed but key active', q("SELECT e.erasureRequestId FROM ERASUREREQUESTS e JOIN ATTRIBUTEKEYS k ON k.attributeKeyId=e.attributeKeyId WHERE e.status='completed' AND k.status<>'destroyed'"))
check('erasure pending but key destroyed', q("SELECT e.erasureRequestId FROM ERASUREREQUESTS e JOIN ATTRIBUTEKEYS k ON k.attributeKeyId=e.attributeKeyId WHERE e.status='pending' AND k.status<>'active'"))
check('erasure consent not withdrawn', q("SELECT e.erasureRequestId FROM ERASUREREQUESTS e JOIN CONSENTS c ON c.consentId=e.consentId WHERE c.status<>'withdrawn'"))
check('erasure consent/key mismatch', q("SELECT e.erasureRequestId FROM ERASUREREQUESTS e JOIN CONSENTS c ON c.consentId=e.consentId JOIN ATTRIBUTEKEYS k ON k.attributeKeyId=e.attributeKeyId WHERE c.studentId<>k.studentId OR c.dataAttributeId<>k.dataAttributeId"))
check('destroyed key still holding values', q("SELECT d.studentDataId FROM STUDENTDATA d JOIN ATTRIBUTEKEYS k ON k.attributeKeyId=d.attributeKeyId WHERE k.status='destroyed' AND (d.dataValue IS NOT NULL OR d.erasedAt IS NULL)"))
check('active key without wrapped key', q("SELECT attributeKeyId FROM ATTRIBUTEKEYS WHERE (status='active') <> (wrappedKey IS NOT NULL)"))
check('value row without value', q("SELECT studentDataId FROM STUDENTDATA d JOIN ATTRIBUTEKEYS k ON k.attributeKeyId=d.attributeKeyId WHERE k.status='active' AND d.dataValue IS NULL"))
check('row key belongs elsewhere', q("SELECT d.studentDataId FROM STUDENTDATA d JOIN ATTRIBUTEKEYS k ON k.attributeKeyId=d.attributeKeyId WHERE k.studentId<>d.studentId OR k.dataAttributeId<>d.dataAttributeId"))
check('pending erasure that cannot run (another grant)', [])
# --- corrections
check('correction pending with reviewer', q("SELECT correctionRequestId FROM CORRECTIONREQUESTS WHERE status IN ('pending','withdrawn') AND (reviewedBy IS NOT NULL OR decidedAt IS NOT NULL)"))
check('correction decided without reviewer', q("SELECT correctionRequestId FROM CORRECTIONREQUESTS WHERE status IN ('approved','rejected') AND (reviewedBy IS NULL OR decidedAt IS NULL OR decisionNotes IS NULL)"))
check('approved correction without new row', q("""SELECT r.correctionRequestId FROM CORRECTIONREQUESTS r JOIN STUDENTDATA s ON s.studentDataId=r.studentDataId WHERE r.status='approved'
    AND NOT EXISTS (SELECT 1 FROM STUDENTDATA n WHERE n.status='correction' AND n.studentId=s.studentId AND n.dataAttributeId=s.dataAttributeId AND n.termId=s.termId AND n.transactionFrom=r.decidedAt AND n.validFrom=s.validFrom)"""))
check('correction on other student row', q("SELECT r.correctionRequestId FROM CORRECTIONREQUESTS r JOIN STUDENTDATA s ON s.studentDataId=r.studentDataId WHERE s.studentId<>r.studentId OR s.isMissing=1"))
check('pending correction not on current row', q("""SELECT r.correctionRequestId FROM CORRECTIONREQUESTS r JOIN STUDENTDATA s ON s.studentDataId=r.studentDataId WHERE r.status='pending'
    AND EXISTS (SELECT 1 FROM STUDENTDATA o WHERE o.studentId=s.studentId AND o.dataAttributeId=s.dataAttributeId AND o.termId=s.termId AND o.transactionFrom > s.transactionFrom)"""))
check('two pending corrections on a row', q("SELECT studentDataId FROM CORRECTIONREQUESTS WHERE status='pending' GROUP BY studentDataId HAVING COUNT(*)>1"))
check('pending correction student without open case', q("""SELECT r.correctionRequestId FROM CORRECTIONREQUESTS r WHERE r.status='pending' AND NOT EXISTS
    (SELECT 1 FROM CASES c WHERE c.studentId=r.studentId AND c.status<>'closed' AND c.advisorId IS NOT NULL)"""))
# --- access logs
check('access log row of another student', q("SELECT a.accessLogId FROM ACCESSLOGS a JOIN STUDENTDATA d ON d.studentDataId=a.studentDataId WHERE d.studentId<>a.recordedUserId"))
check('access log before the row existed', q("SELECT a.accessLogId FROM ACCESSLOGS a JOIN STUDENTDATA d ON d.studentDataId=a.studentDataId WHERE d.transactionFrom > a.accessedAt"))
check('access log flag/status', q("SELECT accessLogId FROM ACCESSLOGS WHERE (isFlagged=1) <> (status IS NOT NULL)"))
check('emergency log without approval', q("""SELECT a.accessLogId FROM ACCESSLOGS a WHERE a.consentPurposeId IS NULL AND NOT EXISTS (SELECT 1 FROM EMERGENCYACCESSREQUESTS e WHERE e.requestedBy=a.accessorId
    AND e.studentId=a.recordedUserId AND e.status='approved' AND a.accessedAt >= e.decidedAt AND a.accessedAt < e.scopeExpiresAt)"""))
check('consent log without consent', q("""SELECT a.accessLogId FROM ACCESSLOGS a JOIN STUDENTDATA d ON d.studentDataId=a.studentDataId WHERE a.consentPurposeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM CONSENTS c
    WHERE c.studentId=a.recordedUserId AND c.dataAttributeId=d.dataAttributeId AND c.consentPurposeId=a.consentPurposeId AND c.grantedAt <= a.accessedAt
    AND (c.withdrawnAt IS NULL OR c.withdrawnAt > a.accessedAt) AND (c.expiresAt IS NULL OR c.expiresAt > a.accessedAt))"""))
check('emergency decided fields', q("""SELECT emergencyAccessRequestId FROM EMERGENCYACCESSREQUESTS WHERE (status='pending') <> (stewardId IS NULL) OR (status='approved') <> (scopeExpiresAt IS NOT NULL)
    OR (status='rejected') <> (rejectionReason IS NOT NULL)"""))
# --- sessions
check('active session with logout', q("SELECT sessionId FROM SESSIONS WHERE isActive=1 AND logoutAt IS NOT NULL"))
check('active session of inactive account', q("SELECT s.sessionId FROM SESSIONS s JOIN ACCOUNTS a ON a.accountId=s.accountId WHERE s.isActive=1 AND a.status<>'active'"))
check('logout before login', q("SELECT sessionId FROM SESSIONS WHERE logoutAt < loginAt"))
check('linked without google', q("SELECT accountId FROM ACCOUNTS WHERE linkStatus='linked' AND googleId IS NULL AND email NOT LIKE '%@gmail.com'"))
check('student role without profile', q("""SELECT ar.accountId FROM ACCOUNT_ROLE ar WHERE ar.roleId='6ED15B52-C53F-58EE-95F1-8B9941C5868E' AND NOT EXISTS (SELECT 1 FROM STUDENTPROFILES p WHERE p.studentId=ar.accountId)"""))
check('case for non-student', q("SELECT caseId FROM CASES c WHERE NOT EXISTS (SELECT 1 FROM STUDENTPROFILES p WHERE p.studentId=c.studentId)"))
check('consent attr outside purpose', q("SELECT consentId FROM CONSENTS c WHERE NOT EXISTS (SELECT 1 FROM PURPOSE_ATTRIBUTE pa WHERE pa.consentPurposeId=c.consentPurposeId AND pa.dataAttributeId=c.dataAttributeId)"))
check('consent request attr outside purpose', q("SELECT consentRequestId FROM CONSENTREQUESTS c WHERE NOT EXISTS (SELECT 1 FROM PURPOSE_ATTRIBUTE pa WHERE pa.consentPurposeId=c.consentPurposeId AND pa.dataAttributeId=c.dataAttributeId)"))
check('consent on inactive purpose', q("SELECT c.consentId FROM CONSENTS c JOIN CONSENTPURPOSES p ON p.consentPurposeId=c.consentPurposeId WHERE p.status<>'active'"))

# --- decrypt every value straight from the SQL file
s = json.loads(re.sub(r'^\s*//.*$', '', open(appsettings, encoding='utf-8-sig').read(), flags=re.M))['Encryption']
master = base64.b64decode(s['MasterKeys'][str(s['CurrentVersion'])])
N = lambda g: g.replace('-', '').lower()
keys = {}
for kid, sid, aid, wrapped in q("SELECT attributeKeyId, studentId, dataAttributeId, wrappedKey FROM ATTRIBUTEKEYS WHERE status='active'"):
    ctx = f"{N(kid)}:{N(sid)}:{N(aid)}"
    info = ("AdviseCare:wrap:v1:" + ctx).encode()
    wk = HKDF(algorithm=hashes.SHA256(), length=32, salt=b'', info=info).derive(master)   # empty salt, like .NET HKDF.DeriveKey(salt: default)
    raw = base64.b64decode(wrapped)
    keys[kid] = AESGCM(wk).decrypt(raw[:12], raw[28:] + raw[12:28], info)


def dec(kid, sid, aid, rid, field, payload):
    aad = f"AdviseCare:value:v1:{N(kid)}:{N(sid)}:{N(aid)}:{N(rid)}:{field}".encode()
    assert payload[0] == 1
    return AESGCM(keys[kid]).decrypt(payload[1:13], payload[29:] + payload[13:29], aad).decode()


n = 0
types = dict(q("SELECT dataAttributeId, dataType FROM DATAATTRIBUTES"))
for rid, kid, sid, aid, val in q("SELECT studentDataId, attributeKeyId, studentId, dataAttributeId, dataValue FROM STUDENTDATA WHERE dataValue IS NOT NULL"):
    v = dec(kid, sid, aid, rid, 'dataValue', val)
    if types[aid] == 'numeric':
        float(v)
    elif types[aid] == 'boolean':
        assert v in ('true', 'false'), v
    n += 1
for crid, sid, kid, aid, cur, prop in q("""SELECT r.correctionRequestId, r.studentId, d.attributeKeyId, d.dataAttributeId, r.currentValue, r.proposedValue
        FROM CORRECTIONREQUESTS r JOIN STUDENTDATA d ON d.studentDataId=r.studentDataId WHERE r.currentValue IS NOT NULL"""):
    dec(kid, sid, aid, crid, 'currentValue', cur)
    dec(kid, sid, aid, crid, 'proposedValue', prop)
    n += 2
print('decrypted values', n)
for p in problems:
    print('PROBLEM', p[0], p[2], p[1])
print('OK' if not problems else f'{len(problems)} problem types')
