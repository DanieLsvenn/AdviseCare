# Loads the schema + the full mock script into SQLite (FK, CHECK, UNIQUE, filtered unique indexes, NVARCHAR lengths)
# and decrypts every encrypted value again like AesGcmDataEncryptor.Decrypt. Usage: validate_full_mock.py schema.sql mock.sql appsettings.json
import sys, re, sqlite3, json, base64, datetime as dt
from collections import Counter, defaultdict
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.kdf.hkdf import HKDF
from cryptography.hazmat.primitives import hashes
from sql_inserts import parse_inserts

schema = open(sys.argv[1], encoding="utf-8-sig").read()
mock = open(sys.argv[2], encoding="utf-8-sig").read()
app = json.load(open(sys.argv[3], encoding="utf-8-sig"))
masters = {int(k): base64.b64decode(v) for k, v in app["Encryption"]["MasterKeys"].items()}

ddl = schema[schema.index("/* ---------- 3. Create tables"):schema.index("/* ---------- 4. Seed")]
lengths = defaultdict(dict)
for m in re.finditer(r"CREATE TABLE \[dbo\]\.\[(\w+)\] \((.*?)\n\);", ddl, re.S):
    for c, n in re.findall(r"^\s+\[(\w+)\] NVARCHAR\((\d+)\)", m.group(2), re.M):
        lengths[m.group(1)][c] = int(n)
s = ddl.replace("[dbo].", "")
s = re.sub(r"CONSTRAINT \[DF_\w+\] DEFAULT NEWSEQUENTIALID\(\)", "", s)
s = re.sub(r"DEFAULT SYSUTCDATETIME\(\)", "DEFAULT CURRENT_TIMESTAMP", s)
s = re.sub(r"\bN'", "'", s)
s = re.sub(r"UNIQUEIDENTIFIER|NVARCHAR\((\d+|MAX)\)|DATETIME2\(3\)|TIME\(0\)", "TEXT", s)
s = s.replace("VARBINARY(MAX)", "BLOB").replace("BIGINT", "INTEGER").replace("TINYINT", "INTEGER").replace(" BIT ", " INTEGER ")
s = re.sub(r"DECIMAL\(\d+,\d+\)", "NUMERIC", s).replace(" INT ", " INTEGER ")
s = re.sub(r"LEN\(LTRIM\(RTRIM\(", "length(trim((", s)
s = s.replace("[", '"').replace("]", '"')
s = "\n".join(l for l in s.splitlines() if l.strip() != "GO" and not l.strip().startswith("--"))
db = sqlite3.connect(":memory:")
db.execute("PRAGMA foreign_keys=ON")
db.executescript(s)

data, order = parse_inserts(mock)
def conv(x, col_type=None):
    if x is None: return None
    k, v = x
    if k == "bin": return bytes.fromhex(v[2:])
    if k == "num": return float(v) if "." in v else int(v)
    return v
problems = []
for t in order:
    cols = data[t]["cols"]
    for r in data[t]["rows"]:
        for c, n in lengths[t].items():
            if c in r and r[c] is not None and len(r[c][1]) > n:
                problems.append(f"{t}.{c} longer than {n}")
        try:
            db.execute(f'INSERT INTO "{t}" ({",".join(chr(34) + c + chr(34) for c in cols)}) VALUES ({",".join("?" * len(cols))})',
                       [conv(r[c]) for c in cols])
        except sqlite3.Error as e:
            problems.append(f"{t}: {e}: {[(c, r[c]) for c in cols][:4]}")
print("tables loaded:", len(order), "rows:", sum(len(data[t]["rows"]) for t in order))
print("insert problems:", len(problems)); print("\n".join(problems[:20]))
print("FK check:", db.execute("PRAGMA foreign_key_check").fetchall()[:5])

q = lambda sql, *a: db.execute(sql, a).fetchall()
# ---- decrypt every value ----
keys = {r[0]: r for r in q('select attributeKeyId, studentId, dataAttributeId, wrappedKey, masterKeyVersion, status from ATTRIBUTEKEYS')}
def n_(g): return g.replace("-", "").lower()
def unwrap(k):
    ctx = f"{n_(k[0])}:{n_(k[1])}:{n_(k[2])}"
    info = ("AdviseCare:wrap:v1:" + ctx).encode()
    wk = HKDF(algorithm=hashes.SHA256(), length=32, salt=None, info=info).derive(masters[k[4]])
    raw = base64.b64decode(k[3])
    return AESGCM(wk).decrypt(raw[:12], raw[28:] + raw[12:28], info)
def dec(key_id, student, attr, row_id, field, payload):
    k = keys[key_id]; assert k[1] == student and k[2] == attr
    assert payload[0] == 1
    p = payload[1:]
    aad = f"AdviseCare:value:v1:{n_(key_id)}:{n_(student)}:{n_(attr)}:{n_(row_id)}:{field}".encode()
    return AESGCM(unwrap(k)).decrypt(p[:12], p[28:] + p[12:28], aad).decode()
ok = 0; vals = {}
for sid, kid, st, at, val in q("select studentDataId, attributeKeyId, studentId, dataAttributeId, dataValue from STUDENTDATA where dataValue is not null"):
    vals[sid] = dec(kid, st, at, sid, "dataValue", val); ok += 1
for cid, sdid, st, cur, prop in q("select correctionRequestId, studentDataId, studentId, currentValue, proposedValue from CORRECTIONREQUESTS where currentValue is not null"):
    kid, at = q("select attributeKeyId, dataAttributeId from STUDENTDATA where studentDataId=?", sdid)[0]
    c = dec(kid, st, at, cid, "currentValue", cur); dec(kid, st, at, cid, "proposedValue", prop); ok += 2
    assert c == vals[sdid], (c, vals[sdid])
print("decrypted values:", ok)

# ---- business rules ----
rules = []
def rule(name, cond):
    rules.append((name, bool(cond)))
rule("one active key per student/attribute", not q("select studentId,dataAttributeId from ATTRIBUTEKEYS where status='active' group by 1,2 having count(*)>1"))
rule("value rows use an active key of the same student/attribute", not q("""select 1 from STUDENTDATA d join ATTRIBUTEKEYS k on k.attributeKeyId=d.attributeKeyId
    where d.erasedAt is null and (k.status<>'active' or k.studentId<>d.studentId or k.dataAttributeId<>d.dataAttributeId)"""))
rule("missing rows only for enrolled students", not q("""select 1 from STUDENTDATA d join STUDENTPROFILES p on p.studentId=d.studentId
    where d.isMissing=1 and d.transactionFrom>'2026-05-01' and p.academicStatus<>'enrolled'"""))
rule("no missing row hides an earlier value", not q("""select 1 from STUDENTDATA m join STUDENTDATA v on v.studentId=m.studentId and v.dataAttributeId=m.dataAttributeId
    and v.termId=m.termId and v.isMissing=0 and v.transactionFrom<m.transactionFrom where m.isMissing=1"""))
fall = q("select termId from ACADEMICTERMS where termName='Fall 2026'")[0][0]
fall_attrs = [a for (a,) in q("select distinct dataAttributeId from STUDENTDATA where termId=?", fall)]
ea = [s_ for (s_,) in q("select p.studentId from STUDENTPROFILES p join ACCOUNTS a on a.accountId=p.studentId where p.academicStatus='enrolled' and a.status='active'")]
have = set(q("select studentId, dataAttributeId from STUDENTDATA where termId=?", fall))
rule("every enrolled active student has a full Fall 2026 row set (value or missing)", all((s_, a) in have for s_ in ea for a in fall_attrs))
rule("flag rows point to a value row of the same student and Fall 2026", not q("""select 1 from INDICATORFLAGS f join STUDENTDATA d on d.studentDataId=f.studentDataId
    where d.studentId<>f.studentId or d.termId<>f.termId or d.isMissing=1"""))
OPS = {"lessThan": lambda x, y: x < y, "greaterOrEqual": lambda x, y: x >= y, "equal": lambda x, y: x == y, "greaterThan": lambda x, y: x > y}
def hit(dtype, op, th, v):
    return OPS[op](float(v), float(th)) if dtype == "numeric" else OPS[op](v.lower(), th.lower())
bad = 0
for st, ia, dtype, op, th, status in q("""select f.studentId, i.dataAttributeId, da.dataType, i.comparisonOperator, i.thresholdValue, f.status from INDICATORFLAGS f
        join INDICATORS i on i.indicatorId=f.indicatorId join DATAATTRIBUTES da on da.dataAttributeId=i.dataAttributeId"""):
    last = q("""select studentDataId from STUDENTDATA where studentId=? and dataAttributeId=? and termId=? and isMissing=0
        order by validFrom desc, transactionFrom desc limit 1""", st, ia, fall)[0][0]
    h = hit(dtype, op, th, vals[last])
    if (status in ("open", "requested", "caseOpened", "dismissed") and not h) or (status == "cleared" and h): bad += 1
rule("flag status matches the latest value (open/requested/dismissed hit, cleared no longer hits)", bad == 0)
rule("case messages only by the case's student or its advisor", not q("""select 1 from CASEMESSAGES m join CASES c on c.caseId=m.caseId
    where m.authorId not in (c.studentId, coalesce(c.advisorId,'-'))"""))
rule("no message after a case was closed", not q("select 1 from CASEMESSAGES m join CASES c on c.caseId=m.caseId where c.closedAt is not null and m.createdAt>c.closedAt"))
rule("messages after the case was opened", not q("select 1 from CASEMESSAGES m join CASES c on c.caseId=m.caseId where m.createdAt<c.openedAt"))
rule("attachment uploader = message author, size = bytes", not q("""select 1 from CASEMESSAGEATTACHMENTS a join CASEMESSAGES m on m.caseMessageId=a.caseMessageId
    where a.uploadedBy<>m.authorId or a.fileSize<>length(a.fileContent)"""))
rule("CASE_ATTRIBUTE: attribute in the purpose of the case's category", not q("""select 1 from CASE_ATTRIBUTE ca join CASES c on c.caseId=ca.caseId
    join SUPPORTCATEGORIES s on s.supportCategoryId=c.supportCategoryId
    left join PURPOSE_ATTRIBUTE pa on pa.consentPurposeId=s.consentPurposeId and pa.dataAttributeId=ca.dataAttributeId where pa.dataAttributeId is null"""))
rule("CASE_ATTRIBUTE: a consent exists for it", not q("""select 1 from CASE_ATTRIBUTE ca join CASES c on c.caseId=ca.caseId
    join SUPPORTCATEGORIES s on s.supportCategoryId=c.supportCategoryId
    where not exists (select 1 from CONSENTS k where k.studentId=c.studentId and k.consentPurposeId=s.consentPurposeId and k.dataAttributeId=ca.dataAttributeId)"""))
rule("correction requests: pending ones have no decision, decided ones a date", not q("""select 1 from CORRECTIONREQUESTS where (status='pending' and decidedAt is not null)
    or (status<>'pending' and decidedAt is null)"""))
rule("one pending correction per value", not q("select studentDataId from CORRECTIONREQUESTS where status='pending' group by 1 having count(*)>1"))
rule("approved Fall corrections have a correction row", len(q("select 1 from STUDENTDATA where status='correction' and termId=?", fall)) ==
     len(q("select 1 from CORRECTIONREQUESTS c join STUDENTDATA d on d.studentDataId=c.studentDataId where c.status='approved' and d.termId=?", fall)))
rule("committed files: rowCount = parsed rows", all(len(json.loads(p)) == n for p, n in q("select parsedRows, rowCount from TRANSCRIPTIMPORTFILES where parsedRows is not null")))
rule("one grade + one attendance file per student in the committed batch", not q("""select studentId, importType from TRANSCRIPTIMPORTFILES f join TRANSCRIPTIMPORTBATCHES b
    on b.importBatchId=f.importBatchId where b.status='committed' group by 1,2 having count(*)>1"""))
rule("results reference committed files of the same student", not q("""select 1 from STUDENTSUBJECTRESULTS r join TRANSCRIPTIMPORTFILES f
    on f.importFileId in (r.gradeImportFileId, r.attendanceImportFileId) where f.studentId<>r.studentId or f.status<>'committed'"""))
rule("every profile has a curriculum code", not q("select 1 from STUDENTPROFILES where curriculumCode is null"))
rule("only one current term", len(q("select 1 from ACADEMICTERMS where status='current'")) == 1)
rule("notification urls use lowercase case ids", not q("select 1 from NOTIFICATIONS where redirectUrl like '/cases/%' and redirectUrl <> lower(redirectUrl)"))
rule("seed @gmail.com staff have no cases, consents, sessions or new logs", not q("""select 1 from ACCOUNTS a where a.email like '%@gmail.com' and a.email<>'student@gmail.com' and
    (exists(select 1 from CASES c where a.accountId in (c.studentId, c.advisorId)) or exists(select 1 from SESSIONS s where s.accountId=a.accountId)
     or exists(select 1 from ACTIVITYLOGS l where l.accountId=a.accountId and l.activityType in ('caseMessageSent','correctionReviewed','indicatorFlagRequested','indicatorFlagDecided','studentDataImported','academicResultsImported')))"""))
for n, okk in rules: print(("OK  " if okk else "FAIL"), n)

print("\nflags:", q("select i.indicatorName, f.status, count(*) from INDICATORFLAGS f join INDICATORS i on i.indicatorId=f.indicatorId group by 1,2"))
print("student@gmail.com Fall values:", sorted((a, vals[i]) for i, a in q("""select d.studentDataId, da.attributeName from STUDENTDATA d join DATAATTRIBUTES da
    on da.dataAttributeId=d.dataAttributeId join ACCOUNTS ac on ac.accountId=d.studentId where ac.email='student@gmail.com' and d.isMissing=0
    and d.termId=(select termId from ACADEMICTERMS where termName='Fall 2026') and da.attributeName in ('GPA','Attendance rate','Tuition overdue')""")))
print("results by status:", q("select status, count(*) from STUDENTSUBJECTRESULTS group by 1"))
print("SE180001 Fall 2026 results:", q("""select s.subjectCode, r.grade, r.attendanceRate, r.status from STUDENTSUBJECTRESULTS r join SUBJECTS s on s.subjectId=r.subjectId
    join STUDENTPROFILES p on p.studentId=r.studentId join ACADEMICTERMS t on t.termId=r.termId where p.studentCode='SE180001' and t.termName='Fall 2026'"""))
if "--debug" in sys.argv:
    print(q("select redirectUrl, createdAt from NOTIFICATIONS where redirectUrl like '/cases/%' and redirectUrl <> lower(redirectUrl) limit 5"))
    print(q("select a.email, l.activityType, l.occurredAt from ACTIVITYLOGS l join ACCOUNTS a on a.accountId=l.accountId where a.email like '%@gmail.com' and a.email<>'student@gmail.com' limit 10"))
    print(len(q("select 1 from NOTIFICATIONS where redirectUrl like '/cases/%' and redirectUrl <> lower(redirectUrl)")))
