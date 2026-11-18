# -*- coding: utf-8 -*-
"""
AdviseCare - full mock data generator (18/11/2026)

Builds AdviseCare_Mock_Data_Full.sql: one script that empties the 52 tables of AdviseCareDB and loads a complete,
consistent mock data set in one transaction. Run it AFTER AdviseCare_Database_Script.sql (same schema).

Sources (all in the repo):
  - section 4 of Documentation/AdviseCare_Database_Script.sql: the team's curated seed and mock rows (kept as they are);
  - AdviseCare_Mock_Data/mock_studentdata_summer2026.xlsx, _fall2026.xlsx, _fall2026_update.xlsx, _student_gmail.csv:
    replayed exactly like POST /api/StudentData/import (values encrypted like AesGcmDataEncryptor, missing rows added);
  - AdviseCare_Mock_Data/Transcripts/{grade,attendance,invalid}/*.pdf (text extracted to JSON beforehand):
    replayed like the transcript import (preview -> commit), with the real SHA-256 of every PDF;
  - AdviseCare_API/AdviseCare.API/appsettings.json: Encryption:MasterKeys / CurrentVersion.
Added on top: INDICATORFLAGS, CORRECTIONREQUESTS (pending/approved/rejected/withdrawn), CASEMESSAGES on every case
with an advisor, CASEMESSAGEATTACHMENTS (real small files), CASE_ATTRIBUTE, access logs, notifications, activity logs.

Usage (from this generator folder; pip install openpyxl cryptography pdfplumber):
  python extract_transcripts.py ../Transcripts transcripts_extract.json
  python generate_full_mock_sql.py ../../AdviseCare_Database_Script.sql .. transcripts_extract.json ^
         ../../../AdviseCare_API/AdviseCare.API/appsettings.json ../AdviseCare_Mock_Data_Full.sql
  python validate_full_mock.py ../../AdviseCare_Database_Script.sql ../AdviseCare_Mock_Data_Full.sql ../../../AdviseCare_API/AdviseCare.API/appsettings.json
With another master key in appsettings.json, run it again: the values are encrypted with whatever key the file holds.
"""
import sys, json, uuid, random, re, base64, csv, io, struct, zlib, datetime as dt
from collections import defaultdict, Counter
import openpyxl
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.kdf.hkdf import HKDF
from cryptography.hazmat.primitives import hashes
from sql_inserts import parse_inserts
from transcripts import load as load_transcripts

SCHEMA, MOCK_DIR, EXTRACT, APPSETTINGS, OUT = sys.argv[1:6]
REF = dt.datetime(2026, 11, 18, 3, 0)          # reference date of the data (UTC)
NS = uuid.UUID("6f1c2a4e-9d1b-4c55-8f0e-a1d2c3b4e5f6")
rnd = random.Random(20261118)


def gid(*parts):
    return str(uuid.uuid5(NS, "|".join(str(p) for p in parts))).upper()


# ---------- typed SQL values ----------
def S(v): return None if v is None else ("str", str(v))
def T(d): return None if d is None else ("lit", d.strftime("%Y-%m-%dT%H:%M:%S.") + f"{d.microsecond // 1000:03d}")
def N(n): return None if n is None else ("num", str(n))
def B(b): return None if b is None else ("bin", "0x" + b.hex().upper())
def BIT(b): return ("num", "1" if b else "0")
def val(x): return None if x is None else x[1]
def tval(x): return None if x is None else dt.datetime.strptime(x[1][:19], "%Y-%m-%dT%H:%M:%S")


# ---------- schema: table order and columns ----------
schema_text = open(SCHEMA, encoding="utf-8-sig").read()
create_order, table_cols = [], {}
for m in re.finditer(r"CREATE TABLE \[dbo\]\.\[(\w+)\] \((.*?)\n\);", schema_text, re.S):
    create_order.append(m.group(1))
    table_cols[m.group(1)] = re.findall(r"^\s+\[(\w+)\] [A-Z]", m.group(2), re.M)
assert len(create_order) == 52, len(create_order)

seed, _ = parse_inserts(schema_text[schema_text.index("/* ---------- 4. Seed"):])
TB = {t: [] for t in create_order}
for t, d in seed.items():
    TB[t].extend(d["rows"])


def add(table, **cols):
    for c in cols:
        assert c in table_cols[table], (table, c)
    TB[table].append(cols)
    return cols


def rows(t): return TB[t]


# ---------- lookups from the seed ----------
acc = {val(r["accountId"]): r for r in rows("ACCOUNTS")}
by_user = {val(r["userName"]): val(r["accountId"]) for r in rows("ACCOUNTS")}
by_email = {val(r["email"]).lower(): val(r["accountId"]) for r in rows("ACCOUNTS")}
name_of = lambda a: val(acc[a]["fullName"])
roles = {val(r["roleName"]): val(r["roleId"]) for r in rows("ROLES")}
role_of = defaultdict(set)
for r in rows("ACCOUNT_ROLE"):
    role_of[val(r["accountId"])].add(val(r["roleId"]))
student_ids = [a for a in acc if roles["Student"] in role_of[a]]
profiles = {val(r["studentId"]): r for r in rows("STUDENTPROFILES")}
code_of = {s: val(p["studentCode"]) for s, p in profiles.items()}
by_code = {v.lower(): k for k, v in code_of.items()}
attrs = {val(r["attributeName"]): r for r in rows("DATAATTRIBUTES")}
attr_id = {k: val(r["dataAttributeId"]) for k, r in attrs.items()}
attr_name = {v: k for k, v in attr_id.items()}
attr_type = {val(r["dataAttributeId"]): val(r["dataType"]) for r in rows("DATAATTRIBUTES")}
terms = {val(r["termName"]): r for r in rows("ACADEMICTERMS")}
term_id = {k: val(r["termId"]) for k, r in terms.items()}
purposes = {val(r["purposeName"]): val(r["consentPurposeId"]) for r in rows("CONSENTPURPOSES")}
purpose_attrs = defaultdict(set)
for r in rows("PURPOSE_ATTRIBUTE"):
    purpose_attrs[val(r["consentPurposeId"])].add(val(r["dataAttributeId"]))
cats = {val(r["supportCategoryId"]): r for r in rows("SUPPORTCATEGORIES")}
subjects = {val(r["subjectCode"]).lower(): r for r in rows("SUBJECTS")}
faculty_code = {val(r["facultyId"]): val(r["facultyCode"]) for r in rows("FACULTIES")}
fac_id = {v: k for k, v in faculty_code.items()}
ADMIN = by_user["admin02"]; STEWARD = by_user["steward02"]
PH_SE = by_user["aa.khoavd"]; PH_BA = by_user["ph.quanpm"]
AA_HUNG = by_user["aa.hungpt"]; AA_LAN = by_user["aa.lannt"]; AA_MAI = by_user["aa.maidt"]

consents = rows("CONSENTS")


def granted_consent(student, purpose, attribute, at=None):
    for c in consents:
        if val(c["studentId"]) == student and val(c["consentPurposeId"]) == purpose and val(c["dataAttributeId"]) == attribute \
                and val(c["status"]) == "granted" and (at is None or tval(c["grantedAt"]) <= at) \
                and (c["expiresAt"] is None or tval(c["expiresAt"]) > (at or REF)):
            return c
    return None


def activity(account, kind, detail, at):
    add("ACTIVITYLOGS", activityLogId=S(gid("log", account, kind, detail, at)), accountId=S(account),
        activityDetail=S(detail), activityType=S(kind), occurredAt=T(at))


def notify(account, title, message, url, at, read):
    add("NOTIFICATIONS", notificationId=S(gid("ntf", account, title, url, at)), accountId=S(account), message=S(message),
        redirectUrl=S(url), title=S(title), createdAt=T(at), isRead=BIT(read))


# ---------- encryption, same as AesGcmDataEncryptor + AttributeKeyService ----------
appsettings = json.load(open(APPSETTINGS, encoding="utf-8-sig"))
MK_VERSION = int(appsettings["Encryption"]["CurrentVersion"])
MASTER = base64.b64decode(appsettings["Encryption"]["MasterKeys"][str(MK_VERSION)])
WRAP = "AdviseCare:wrap:v1:"
nonce_rng = random.Random(7)


def n_(g): return g.replace("-", "").lower()          # Guid.ToString("N")


def key_context(key_id, student, attribute): return f"{n_(key_id)}:{n_(student)}:{n_(attribute)}"


def seal(key, plain, aad):
    nonce = nonce_rng.randbytes(12)
    ct = AESGCM(key).encrypt(nonce, plain, aad)        # ciphertext | tag
    return nonce + ct[-16:] + ct[:-16]                  # nonce | tag | ciphertext


def wrapping_key(ctx):
    return HKDF(algorithm=hashes.SHA256(), length=32, salt=None, info=(WRAP + ctx).encode()).derive(MASTER)


active_keys = {}   # (student, attribute) -> dict(id, data_key)


def get_key(student, attribute, at):
    k = active_keys.get((student, attribute))
    if k: return k
    key_id = gid("attrkey", student, attribute)
    data_key = nonce_rng.randbytes(32)
    ctx = key_context(key_id, student, attribute)
    wrapped = base64.b64encode(seal(wrapping_key(ctx), data_key, (WRAP + ctx).encode())).decode()
    add("ATTRIBUTEKEYS", attributeKeyId=S(key_id), dataAttributeId=S(attribute), studentId=S(student), wrappedKey=S(wrapped),
        masterKeyVersion=N(MK_VERSION), createdAt=T(at), destroyedAt=None, status=S("active"))
    k = active_keys[(student, attribute)] = dict(id=key_id, data_key=data_key)
    return k


def encrypt(student, attribute, row_id, field, plain, at):
    k = get_key(student, attribute, at)
    aad = f"AdviseCare:value:v1:{n_(k['id'])}:{n_(student)}:{n_(attribute)}:{n_(row_id)}:{field}"
    return k["id"], b"\x01" + seal(k["data_key"], plain.encode(), aad.encode())


# ================================================================
# 1. STUDENTDATA: replay the 4 import files like POST /api/StudentData/import
# ================================================================
def read_xlsx(path):
    ws = openpyxl.load_workbook(path).active
    it = ws.iter_rows(values_only=True)
    head = [str(h).strip() for h in next(it)]
    return [dict(zip(head, ["" if v is None else str(v).strip() for v in r])) for r in it]


def read_csv(path):
    return list(csv.DictReader(open(path, encoding="utf-8-sig")))


def find_student(ident):
    i = ident.lower()
    for table in (by_email, by_user, by_code):
        if i in table and table[i] in student_ids:
            return table[i]
    for a in student_ids:
        if val(acc[a]["userName"]).lower() == i: return a
    raise KeyError(ident)


enrolled_active = [s for s in student_ids
                   if val(profiles[s]["academicStatus"]) == "enrolled" and val(acc[s]["status"]) == "active"]
sd_rows = []         # our own view of STUDENTDATA values: dicts with plain value
imports = [
    ("mock_studentdata_summer2026.xlsx", dt.datetime(2026, 8, 26, 2, 15)),
    ("mock_studentdata_fall2026.xlsx", dt.datetime(2026, 10, 20, 2, 10)),
    ("mock_studentdata_fall2026_update.xlsx", dt.datetime(2026, 10, 30, 2, 5)),
    ("mock_studentdata_student_gmail.csv", dt.datetime(2026, 11, 5, 3, 20)),
]
missing_done = set()
flag_events = []   # (time) after each import -> evaluate indicators
for file_name, at in imports:
    path = f"{MOCK_DIR}/{file_name}"
    data = read_csv(path) if file_name.endswith(".csv") else read_xlsx(path)
    inserted, triples, pairs = 0, set(), {}
    for r in data:
        s = find_student(r["Student"]); a = attr_id[r["Data attribute"]]; t = term_id[r["Term"]]
        assert val(attrs[r["Data attribute"]]["status"]) == "active"
        vf = dt.datetime.strptime(r["Valid from"][:10], "%Y-%m-%d")
        row_id = gid("sd", file_name, s, a, t)
        key_id, payload = encrypt(s, a, row_id, "dataValue", r["Value"], at)
        add("STUDENTDATA", studentDataId=S(row_id), attributeKeyId=S(key_id), dataAttributeId=S(a), dataSourceId=None, studentId=S(s),
            termId=S(t), dataValue=B(payload), provenanceNote=S(f"Imported from {file_name} on {at:%Y-%m-%d}."), erasedAt=None,
            transactionFrom=T(at), validFrom=T(vf), isMissing=BIT(False), status=S("original"))
        sd_rows.append(dict(id=row_id, student=s, attr=a, term=t, value=r["Value"], tx=at, vf=vf, status="original", key=key_id, file=file_name))
        triples.add((s, a, t)); pairs[(a, t)] = 1; inserted += 1
    missing = 0
    for s in enrolled_active:
        for (a, t) in pairs:
            if (s, a, t) in triples: continue
            # the API also adds a missing row when the student already has a value from an earlier import (it then hides that
            # value in the system-time view); the mock data only records a pair as missing when it has no row yet
            if any(r["student"] == s and r["attr"] == a and r["term"] == t for r in sd_rows) or (s, a, t) in missing_done: continue
            missing_done.add((s, a, t))
            term_start = tval(terms[[k for k, v in term_id.items() if v == t][0]]["startDate"])
            add("STUDENTDATA", studentDataId=S(gid("sdmiss", file_name, s, a, t)), attributeKeyId=None, dataAttributeId=S(a), dataSourceId=None,
                studentId=S(s), termId=S(t), dataValue=None,
                provenanceNote=S(f"Expected in {file_name}; no value was supplied. Recorded {at:%Y-%m-%d}."), erasedAt=None,
                transactionFrom=T(at), validFrom=T(term_start), isMissing=BIT(True), status=S("original"))
            missing += 1
    activity(ADMIN, "studentDataImported",
             f"Imported {inserted} value(s) from {file_name}; recorded {missing} missing value(s); left out 0 row(s) of archived data attributes", at)
    flag_events.append(at)
    print(file_name, inserted, "values,", missing, "missing")

# ================================================================
# 2. CORRECTIONREQUESTS on Fall 2026 values (encrypted current/proposed values)
# ================================================================
FALL = term_id["Fall 2026"]
AADV = purposes["Academic advising"]


def latest(student, attribute, term=FALL, at=REF):
    cand = [r for r in sd_rows if r["student"] == student and r["attr"] == attribute and r["term"] == term and r["tx"] <= at]
    if not cand: return None
    return max(cand, key=lambda r: (r["vf"], r["tx"]))


corr_plan = [  # (status, attribute, proposed, explanation, submitted, decided, reviewer, notes)
    ("pending", "GPA", "+0.4", "My GPA does not include the PRN212 retake result that was published last week.", dt.datetime(2026, 11, 12, 8, 30), None, None, None),
    ("pending", "Absent slots", "-3", "Three absences were recorded on days I had an approved medical leave; I have the leave form.", dt.datetime(2026, 11, 14, 2, 10), None, None, None),
    ("pending", "Credits attempted", "+0", "I dropped one subject in week 2; the registration office confirmed it by email.", dt.datetime(2026, 11, 15, 13, 45), None, None, None),
    ("pending", "Attendance rate", "+6", "The attendance of my Saturday class was not synchronised for two weeks.", dt.datetime(2026, 11, 16, 9, 5), None, None, None),
    ("approved", "Late submissions", "-2", "Two assignments were marked late because the LMS was down on 2026-10-12.", dt.datetime(2026, 11, 3, 4, 0), dt.datetime(2026, 11, 4, 7, 20), AA_HUNG, "Confirmed with IT: the LMS outage covered both deadlines. Corrected."),
    ("approved", "Credits earned", "+3", "My summer internship credits were not counted yet.", dt.datetime(2026, 11, 6, 6, 0), dt.datetime(2026, 11, 7, 9, 40), AA_LAN, "The internship report was graded on 2026-10-25; the credits count."),
    ("rejected", "GPA", "+1.0", "I think my GPA should be higher.", dt.datetime(2026, 11, 8, 15, 30), dt.datetime(2026, 11, 10, 2, 15), AA_HUNG, "The academic records office confirms the imported GPA; no grade change was published."),
    ("withdrawn", "Attendance rate", "+4", "Attendance seems low for my DBI202 class.", dt.datetime(2026, 11, 9, 10, 0), dt.datetime(2026, 11, 9, 18, 30), None, None),
]
used_students = set()
for i, (status, an, delta, expl, sub, dec, reviewer, notes) in enumerate(corr_plan):
    a = attr_id[an]
    cand = [s for s in enrolled_active if s not in used_students and code_of[s].startswith("SE")
            and granted_consent(s, AADV, a, sub) and latest(s, a, at=sub)]
    s = cand[(i * 3) % len(cand)]
    used_students.add(s)
    src = latest(s, a, at=sub)
    cur = src["value"]
    if delta.startswith(("+", "-")):
        num = float(cur) + float(delta)
        num = max(0, num)
        prop = (f"{num:.1f}" if "." in cur else str(int(round(num))))
        if an == "Credits attempted": prop = str(int(float(cur)) - 3)
        if an == "Attendance rate": prop = str(min(100, int(round(num))))
    cr_id = gid("corr", i, s)
    _, cur_payload = encrypt(s, a, cr_id, "currentValue", cur, sub)
    _, prop_payload = encrypt(s, a, cr_id, "proposedValue", prop, sub)
    add("CORRECTIONREQUESTS", correctionRequestId=S(cr_id), consentPurposeId=S(AADV), reviewedBy=S(reviewer), studentDataId=S(src["id"]),
        studentId=S(s), currentValue=B(cur_payload), decisionNotes=S(notes), explanation=S(expl), proposedValue=B(prop_payload),
        decidedAt=T(dec), submittedAt=T(sub), status=S(status))
    activity(s, "correctionRequested", f"Asked to correct {an} (Fall 2026)", sub)
    if status == "withdrawn":
        activity(s, "correctionRequestWithdrawn", f"Withdrew the correction request of {an} (Fall 2026)", dec)
    if status in ("approved", "rejected"):
        activity(reviewer, "correctionReviewed", f"{status.capitalize()} the correction request of {an} (Fall 2026) of {name_of(s)}", dec)
        notify(s, f"Correction request {status}", f"Your request to correct {an} (Fall 2026) was {status}.", "/me/data", dec + dt.timedelta(minutes=1), status == "rejected")
        add("ACCESSLOGS", accessLogId=S(gid("al-corr", cr_id)), accessorId=S(reviewer), consentPurposeId=S(AADV), recordedUserId=S(s),
            studentDataId=S(src["id"]), accessedResource=S(f"Correction request: {an} (Fall 2026)"), accessedAt=T(dec - dt.timedelta(minutes=12)),
            isFlagged=BIT(False), status=None)
    if status == "approved":
        new_id = gid("sdcorr", cr_id)
        key_id, payload = encrypt(s, a, new_id, "dataValue", prop, dec)
        add("STUDENTDATA", studentDataId=S(new_id), attributeKeyId=S(key_id), dataAttributeId=S(a), dataSourceId=None, studentId=S(s), termId=S(FALL),
            dataValue=B(payload), provenanceNote=S(f"Correction of the value imported on {src['tx']:%Y-%m-%d}, approved on {dec:%Y-%m-%d}."),
            erasedAt=None, transactionFrom=T(dec), validFrom=T(src["vf"]), isMissing=BIT(False), status=S("correction"))
        sd_rows.append(dict(id=new_id, student=s, attr=a, term=FALL, value=prop, tx=dec, vf=src["vf"], status="correction", key=key_id, file=None))

# ================================================================
# 3. INDICATORFLAGS: evaluate the active indicators on Fall 2026 after each import / correction
# ================================================================
indicators = [r for r in rows("INDICATORS") if val(r["status"]) == "active"]
OPS = {"lessThan": lambda x, y: x < y, "lessOrEqual": lambda x, y: x <= y, "greaterThan": lambda x, y: x > y,
       "greaterOrEqual": lambda x, y: x >= y, "equal": lambda x, y: x == y, "notEqual": lambda x, y: x != y}


def hits(ind, value):
    dtp = attr_type[val(ind["dataAttributeId"])]
    th = val(ind["thresholdValue"]); op = OPS[val(ind["comparisonOperator"])]
    if dtp == "numeric": return op(float(value), float(th))
    return op(value.strip().lower(), th.strip().lower())


open_flags = {}
eval_times = sorted(set(flag_events + [r["tx"] for r in sd_rows if r["status"] == "correction"]))
for at in eval_times:
    for s in enrolled_active:
        for ind in indicators:
            a = val(ind["dataAttributeId"])
            cur = latest(s, a, at=at)
            key = (s, val(ind["indicatorId"]))
            hit = cur is not None and hits(ind, cur["value"])
            if hit and key not in open_flags:
                f = add("INDICATORFLAGS", indicatorFlagId=S(gid("flag", s, key[1], at)), caseId=None, indicatorId=S(key[1]), requestedBy=None,
                        resolvedBy=None, studentDataId=S(cur["id"]), studentId=S(s), termId=S(FALL), dismissReason=None, flaggedAt=T(at),
                        requestedAt=None, resolvedAt=None, priority=ind["priority"], severity=ind["severity"], status=S("open"))
                open_flags[key] = f
            elif not hit and key in open_flags:
                f = open_flags.pop(key)
                f["status"] = S("cleared"); f["resolvedAt"] = T(at)
# give a few open flags a Programme Head / advisor story
case_rows = rows("CASES")
open_list = sorted(open_flags.values(), key=lambda f: (val(f["flaggedAt"]), val(f["studentId"])))
ph_of = lambda s: PH_SE if faculty_code.get(val(acc[s]["facultyId"])) == "SE" else PH_BA
stories = 0
for f in open_list:
    s = val(f["studentId"])
    if s == by_email["student@gmail.com"]: continue
    fl_at = tval(f["flaggedAt"])
    if stories == 0:      # advisor asked to take the student on
        adv = AA_HUNG if code_of[s].startswith("SE") else AA_MAI
        f.update(status=S("requested"), requestedBy=S(adv), requestedAt=T(fl_at + dt.timedelta(days=2, hours=3)))
        activity(adv, "indicatorFlagRequested", f"Asked to take on {name_of(s)} ({code_of[s]}) flagged by an early-support indicator", fl_at + dt.timedelta(days=2, hours=3))
    elif stories == 1:
        adv = AA_LAN if code_of[s].startswith("SE") else AA_MAI
        f.update(status=S("requested"), requestedBy=S(adv), requestedAt=T(fl_at + dt.timedelta(days=4)))
        activity(adv, "indicatorFlagRequested", f"Asked to take on {name_of(s)} ({code_of[s]}) flagged by an early-support indicator", fl_at + dt.timedelta(days=4))
    elif stories == 2:    # dismissed by the Programme Head
        ph = ph_of(s)
        f.update(status=S("dismissed"), resolvedBy=S(ph), resolvedAt=T(fl_at + dt.timedelta(days=3)),
                 dismissReason=S("Already followed by the student's lecturer; the student has a make-up plan for the missed slots."))
        activity(ph, "indicatorFlagDecided", f"Dismissed a flag of {name_of(s)} ({code_of[s]})", fl_at + dt.timedelta(days=3))
    elif stories == 3:    # turned into a case: link to an outreach case of that student if there is one
        own = [c for c in case_rows if val(c["studentId"]) == s and val(c["status"]) != "closed"]
        if not own: continue
        c = own[0]; ph = ph_of(s)
        f.update(status=S("caseOpened"), caseId=c["caseId"], requestedBy=c["advisorId"] or S(AA_HUNG),
                 requestedAt=T(fl_at + dt.timedelta(days=1)), resolvedBy=S(ph), resolvedAt=T(fl_at + dt.timedelta(days=1, hours=5)))
        activity(ph, "indicatorFlagDecided", f"Approved the request on a flag of {name_of(s)} ({code_of[s]}); linked to case {val(c['caseId']).lower()}", fl_at + dt.timedelta(days=1, hours=5))
    else:
        break
    stories += 1

# ================================================================
# 4. CASE_ATTRIBUTE, CASEMESSAGES, CASEMESSAGEATTACHMENTS
# ================================================================
ca_existing = {(val(r["caseId"]), val(r["dataAttributeId"])) for r in rows("CASE_ATTRIBUTE")}
share_reasons = {
    "GPA": "My grades dropped this term, so you can see where I stand.",
    "Attendance rate": "I missed classes because of my work shifts.",
    "Part-time work hours": "I work evenings to pay my rent.",
    "Commute time": "I travel more than an hour each way.",
    "Accommodation status": "I am looking for a cheaper room near campus.",
    "Financial difficulty": "My family had a difficult year.",
    "Tuition outstanding": "I could not pay the second tuition instalment yet.",
    "Late submissions": None,
    "Failed subjects": "I failed two subjects last term and want a plan.",
    "Absent slots": None,
    "Scholarship status": "I lost part of my scholarship.",
}
seed_msg_cases = {val(r["caseId"]) for r in rows("CASEMESSAGES")}
stage_hist = defaultdict(list)
for h in rows("CASESTAGEHISTORIES"):
    stage_hist[val(h["caseId"])].append(h)
change_req = defaultdict(list)
for c in rows("CASECHANGEREQUESTS"):
    if val(c["status"]) == "approved": change_req[val(c["caseId"])].append(tval(c["decidedAt"]))

ca_count = 0
for c in case_rows:
    cid, s = val(c["caseId"]), val(c["studentId"])
    if val(c["openedBy"]) != s: continue                         # outreach cases: nothing shared by the student
    p = val(cats[val(c["supportCategoryId"])]["consentPurposeId"])
    opened = tval(c["openedAt"])
    picked = 0
    for an, reason in share_reasons.items():
        a = attr_id[an]
        if a not in purpose_attrs[p] or (cid, a) in ca_existing: continue
        if not granted_consent(s, p, a, opened + dt.timedelta(minutes=5)) and not granted_consent(s, p, a): continue
        add("CASE_ATTRIBUTE", caseId=S(cid), dataAttributeId=S(a), shareReason=S(reason), sharedAt=T(opened))
        ca_existing.add((cid, a)); ca_count += 1; picked += 1
        if picked >= 3: break

THREADS = {
    "academic": [
        ("adv", "Hi {s}, I read your request. Can we meet on Teams this Thursday at 2 pm?"),
        ("stu", "Thursday 2 pm is fine for me. Thank you!"),
        ("adv", "Before we meet, please send me your progress-test results and the list of topics you find hardest."),
        ("stu", "Here they are. The hardest part for me is the practical exam."),
        ("adv", "Thanks. I booked two tutoring slots for you next week (Tue and Fri, room 305). Tell me after the first one how it went."),
        ("stu", "The first tutoring slot helped a lot, I will keep going."),
    ],
    "wellbeing": [
        ("adv", "Hi {s}, thank you for reaching out. Would you like to talk in person or online?"),
        ("stu", "In person please, maybe after my afternoon class."),
        ("adv", "Let us meet at the student services room on the 2nd floor, Wednesday 4:30 pm."),
        ("stu", "OK, see you then."),
        ("adv", "As we discussed, here is the short plan for the next two weeks. Write to me any time if something changes."),
        ("stu", "Thank you, I feel a bit better already."),
    ],
    "money": [
        ("adv", "Hi {s}, I received your request about money matters. Do you have the tuition notice with you?"),
        ("stu", "Yes, I attached the notice and my part-time payslip."),
        ("adv", "Thanks. I asked the welfare office about an instalment plan; they will contact you this week."),
        ("stu", "The welfare officer called me, we are preparing the paperwork."),
    ],
}


def ascii_text(t):
    import unicodedata
    return unicodedata.normalize("NFKD", t.replace("đ", "d").replace("Đ", "D")).encode("ascii", "ignore").decode()


def tiny_pdf(lines):
    lines = [ascii_text(l) for l in lines]
    content = "BT /F1 12 Tf 50 780 Td 16 TL " + " ".join(f"({l}) Tj T*" for l in lines) + " ET"
    objs = ["<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            f"<< /Length {len(content)} >>\nstream\n{content}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"]
    out = b"%PDF-1.4\n"; offs = []
    for i, o in enumerate(objs, 1):
        offs.append(len(out)); out += f"{i} 0 obj\n{o}\nendobj\n".encode("latin-1")
    xref = len(out)
    out += f"xref\n0 {len(objs) + 1}\n0000000000 65535 f \n".encode() + b"".join(f"{o:010d} 00000 n \n".encode() for o in offs)
    out += f"trailer\n<< /Size {len(objs) + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n".encode()
    return out


def tiny_png(w=64, h=40, color=(59, 130, 246)):
    raw = b"".join(b"\x00" + bytes(color) * w for _ in range(h))
    chunk = lambda t, d: struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b"")


ATTACH = {
    "academic": [("progress_tests.pdf", "application/pdf", lambda s: tiny_pdf(["Progress test results", f"Student: {s}", "PT1: 6.5   PT2: 5.0   PT3: 7.0"]))],
    "wellbeing": [("two_week_plan.txt", "text/plain", lambda s: f"Two-week plan for {s}\n- Sleep before midnight on weekdays\n- One short walk every day\n- Message the advisor on Friday\n".encode())],
    "money": [("tuition_notice.png", "image/png", lambda s: tiny_png()),
              ("payslip_october.csv", "text/csv", lambda s: "date,hours,amount_vnd\n2026-10-05,20,500000\n2026-10-12,18,450000\n2026-10-19,22,550000\n".encode())],
}
ATTACH_AT = {"academic": 3, "wellbeing": 4, "money": 1}

msg_count = att_count = 0
student_gmail = by_email["student@gmail.com"]
for c in case_rows:
    cid, s, adv = val(c["caseId"]), val(c["studentId"]), val(c["advisorId"])
    if adv is None or cid in seed_msg_cases: continue
    cat = cats[val(c["supportCategoryId"])]
    kind = "money" if val(cat["categoryName"]) == "Money" else val(cat["supportType"])
    hist = sorted(stage_hist[cid], key=lambda h: tval(h["changedAt"]))
    start = next((tval(h["changedAt"]) for h in hist if val(h["toStage"]) == "assigned"), tval(c["openedAt"]) + dt.timedelta(hours=6))
    if change_req[cid]: start = max(start, max(change_req[cid]))
    end = tval(c["closedAt"]) if c["closedAt"] else REF - dt.timedelta(hours=2)
    stage = val(c["currentStage"])
    n_msgs = {"assigned": 2, "contacted": 4, "referred": 4, "followUp": 6, "closed": 5}.get(stage, 2)
    script = THREADS[kind][:n_msgs]
    span = (end - start).total_seconds()
    if span <= 3600: continue
    first = name_of(s).split()[-1]
    for i, (who, text) in enumerate(script):
        at = start + dt.timedelta(seconds=span * (i + 1) / (len(script) + 1))
        at = at.replace(second=0, microsecond=0)
        author = adv if who == "adv" else s
        mid = gid("msg", cid, i)
        add("CASEMESSAGES", caseMessageId=S(mid), authorId=S(author), caseId=S(cid), messageText=S(text.format(s=first)), createdAt=T(at))
        msg_count += 1
        files = ATTACH[kind] if i == ATTACH_AT[kind] and (msg_count % 2 == 0 or kind == "money") else []
        for j, (fname, ctype, make) in enumerate(files):
            content = make(name_of(s))
            add("CASEMESSAGEATTACHMENTS", caseMessageAttachmentId=S(gid("att", mid, j)), caseMessageId=S(mid), uploadedBy=S(author),
                contentType=S(ctype), fileContent=B(content), fileName=S(fname), fileSize=N(len(content)), createdAt=T(at))
            att_count += 1
        activity(author, "caseMessageSent",
                 f"Wrote a message on case {cid.lower()}" if not files else
                 f"Wrote a message with {len(files)} file(s) on case {cid.lower()}: {', '.join(f[0] for f in files)}", at)
        recipient = s if who == "adv" else adv
        is_last = i == len(script) - 1
        if who == "adv":
            notify(recipient, "New message from your advisor", "Your advisor wrote a message on your support case. Open the case to read it and reply.",
                   f"/cases/{cid.lower()}", at, not (is_last and val(c["status"]) != "closed"))
        else:
            notify(recipient, "New message from your student", "The student of one of your cases wrote a message. Open the case to read it and reply.",
                   f"/cases/{cid.lower()}", at, not (is_last and val(c["status"]) != "closed"))

# ================================================================
# 5. Transcript import: committed batch, cancelled batch (invalid files), one previewed re-upload
# ================================================================
tfiles = load_transcripts(EXTRACT)
term_meta = {}


def term_start(name):
    season, year = name.split(); year = int(year)
    return dt.datetime(year, {"Spring": 1, "Summer": 5, "Fall": 9}[season], 1)


def term_end(name):
    season, year = name.split(); year = int(year)
    m = {"Spring": 4, "Summer": 8, "Fall": 12}[season]
    nd = dt.datetime(year + (m == 12), m % 12 + 1, 1) - dt.timedelta(days=1)
    return nd


def jnum(v):
    return None if v is None else (float(v) if "." in v else int(v))


def parsed_json(rows_):
    return json.dumps([{"rowNumber": r["rowNumber"], "isInCurriculum": r["isInCurriculum"], "curriculumTerm": r["curriculumTerm"],
                        "termName": r["termName"], "subjectCode": r["subjectCode"], "subjectName": r["subjectName"],
                        "value": jnum(r["value"]), "status": r["status"]} for r in rows_], ensure_ascii=False, separators=(",", ":"))


def check_json(errors, warnings, create, update, skipped):
    return json.dumps({"errors": errors, "warnings": warnings, "createCount": create, "updateCount": update, "skippedRowCount": skipped},
                      ensure_ascii=False, separators=(",", ":"))


B_COMMIT = gid("tbatch", "commit"); B_CANCEL = gid("tbatch", "cancel"); B_PREVIEW = gid("tbatch", "preview")
up_commit = dt.datetime(2026, 11, 17, 2, 30); commit_at = dt.datetime(2026, 11, 17, 2, 41)
up_cancel = dt.datetime(2026, 11, 17, 1, 50); cancel_at = dt.datetime(2026, 11, 17, 1, 58)
up_prev = dt.datetime(2026, 11, 18, 1, 40)
add("TRANSCRIPTIMPORTBATCHES", importBatchId=S(B_CANCEL), importedBy=S(ADMIN), createdAt=T(up_cancel), committedAt=None, status=S("cancelled"))
add("TRANSCRIPTIMPORTBATCHES", importBatchId=S(B_COMMIT), importedBy=S(ADMIN), createdAt=T(up_commit), committedAt=T(commit_at), status=S("committed"))
add("TRANSCRIPTIMPORTBATCHES", importBatchId=S(B_PREVIEW), importedBy=S(ADMIN), createdAt=T(up_prev), committedAt=None, status=S("previewed"))

valid = [f for f in tfiles if "invalid/" not in f["path"]]
invalid = [f for f in tfiles if "invalid/" in f["path"]]
# cancelled batch: the 3 files with problems; parsed rows cleared by the cancel
for f in invalid:
    sid = by_code.get(f["studentCode"].lower())
    errs = list(f["errors"])
    if sid is None:
        errs = [f"Student {f['studentCode']} has no student profile in AdviseCare."]
    add("TRANSCRIPTIMPORTFILES", importFileId=S(gid("tfile", B_CANCEL, f["fileName"])), importBatchId=S(B_CANCEL), studentId=S(sid),
        fileName=S(f["fileName"]), fileHash=S(f["sha"]), importType=S(f["importType"]), studentCodeInFile=S(f["studentCode"]),
        fullNameInFile=S(f["fullName"]), curriculumCode=S(f["curriculum"]), rowCount=N(0), parsedRows=None,
        validationResult=S(check_json(errs, f["warnings"], 0, 0, f["skipped"] if not errs or sid else 0)), uploadedAt=T(up_cancel), status=S("invalid"))
activity(ADMIN, "academicResultsImportCancelled", f"Cancelled a transcript import of {len(invalid)} file(s)", cancel_at)

# committed batch: every grade and attendance PDF
res = {}       # (student, subjectCode, termName) -> result dict
new_subjects, new_terms = {}, set()
file_ids = {}
for f in sorted(valid, key=lambda f: (0 if f["importType"] == "grade" else 1, f["fileName"])):
    sid = by_code[f["studentCode"].lower()]
    assert not f["errors"], f["fileName"]
    fid = gid("tfile", B_COMMIT, f["fileName"]); file_ids[f["fileName"]] = fid
    for r in f["rows"]:
        if r["subjectCode"].lower() not in subjects and r["subjectCode"].lower() not in new_subjects:
            new_subjects[r["subjectCode"].lower()] = (r["subjectCode"], r["subjectName"], val(acc[sid]["facultyId"]))
        if r["termName"] not in term_id: new_terms.add(r["termName"])
    add("TRANSCRIPTIMPORTFILES", importFileId=S(fid), importBatchId=S(B_COMMIT), studentId=S(sid), fileName=S(f["fileName"]), fileHash=S(f["sha"]),
        importType=S(f["importType"]), studentCodeInFile=S(f["studentCode"]), fullNameInFile=S(f["fullName"]), curriculumCode=S(f["curriculum"]),
        rowCount=N(len(f["rows"])), parsedRows=S(parsed_json(f["rows"])),
        validationResult=S(check_json([], f["warnings"], len(f["rows"]), 0, f["skipped"])), uploadedAt=T(up_commit), status=S("committed"))
    f["_sid"], f["_fid"] = sid, fid

for code_l, (code, name, fac) in sorted(new_subjects.items()):
    sub = add("SUBJECTS", subjectId=S(gid("subject", code_l)), facultyId=S(fac), subjectCode=S(code), subjectName=S(name or code), status=S("active"))
    subjects[code_l] = sub
for tn in sorted(new_terms, key=term_start):
    tid = gid("term", tn)
    add("ACADEMICTERMS", termId=S(tid), endDate=T(term_end(tn)), startDate=T(term_start(tn)), termName=S(tn), status=S("past"))
    term_id[tn] = tid
assert all(term_end(t) < commit_at for t in new_terms)

for f in sorted(valid, key=lambda f: (0 if f["importType"] == "grade" else 1, f["fileName"])):
    is_grade = f["importType"] == "grade"
    for r in f["rows"]:
        k = (f["_sid"], r["subjectCode"].lower(), r["termName"])
        x = res.setdefault(k, dict(student=f["_sid"], subject=val(subjects[r["subjectCode"].lower()]["subjectId"]), term=term_id[r["termName"]],
                                   inCurr=True, ct=None, grade=None, att=None, status=None, gfile=None, afile=None))
        x["inCurr"] = r["isInCurriculum"]
        if r["curriculumTerm"] is not None: x["ct"] = r["curriculumTerm"]
        if r["status"] is not None and (is_grade or x["gfile"] is None): x["status"] = r["status"]
        if is_grade: x["grade"], x["gfile"] = r["value"], f["_fid"]
        else: x["att"], x["afile"] = r["value"], f["_fid"]
for k, x in sorted(res.items()):
    add("STUDENTSUBJECTRESULTS", studentSubjectResultId=S(gid("ssr", *k)), studentId=S(x["student"]), subjectId=S(x["subject"]), termId=S(x["term"]),
        curriculumTerm=N(x["ct"]), isInCurriculum=BIT(x["inCurr"]), grade=N(x["grade"]), attendanceRate=N(x["att"]), status=S(x["status"]),
        gradeImportFileId=S(x["gfile"]), attendanceImportFileId=S(x["afile"]),
        gradeUpdatedAt=T(commit_at if x["gfile"] else None), attendanceUpdatedAt=T(commit_at if x["afile"] else None))
for f in valid:
    profiles[f["_sid"]]["curriculumCode"] = S(f["curriculum"])
for p in rows("STUDENTPROFILES"):
    p.setdefault("curriculumCode", None)
gcount = sum(1 for f in valid if f["importType"] == "grade")
activity(ADMIN, "academicResultsImported",
         f"Imported {len(valid)} transcript file(s) ({gcount} grade, {len(valid) - gcount} attendance): {len(res)} subject result(s) created, 0 updated, "
         f"{len(new_subjects)} subject(s) and {len(new_terms)} term(s) added; 0 invalid file(s) left out", commit_at)

# previewed batch: SE180001's attendance PDF uploaded again (same hash -> warning, every row is an update)
f = next(x for x in valid if x["fileName"] == "Attendance_SE180001.pdf")
add("TRANSCRIPTIMPORTFILES", importFileId=S(gid("tfile", B_PREVIEW, f["fileName"])), importBatchId=S(B_PREVIEW), studentId=S(f["_sid"]),
    fileName=S(f["fileName"]), fileHash=S(f["sha"]), importType=S("attendance"), studentCodeInFile=S(f["studentCode"]), fullNameInFile=S(f["fullName"]),
    curriculumCode=S(f["curriculum"]), rowCount=N(len(f["rows"])), parsedRows=S(parsed_json(f["rows"])),
    validationResult=S(check_json([], [f"This exact file was already imported on {up_commit:%Y-%m-%d}; importing it again changes nothing."] + f["warnings"],
                                  0, len(f["rows"]), f["skipped"])),
    uploadedAt=T(up_prev), status=S("valid"))

# ================================================================
# 6. Access logs of advisors reading Fall 2026 data and academic results of their case students
# ================================================================
GPA, ATT = attr_id["GPA"], attr_id["Attendance rate"]
al = 0
for c in case_rows:
    cid, s, adv = val(c["caseId"]), val(c["studentId"]), val(c["advisorId"])
    if adv is None or val(c["status"]) == "closed": continue
    p = val(cats[val(c["supportCategoryId"])]["consentPurposeId"])
    base = max(tval(c["openedAt"]), dt.datetime(2026, 10, 21)) + dt.timedelta(days=1, hours=2)
    for a in (GPA, ATT):
        if a not in purpose_attrs[p] or not granted_consent(s, p, a, base): continue
        cur = latest(s, a, at=base)
        if not cur: continue
        add("ACCESSLOGS", accessLogId=S(gid("al", cid, a)), accessorId=S(adv), consentPurposeId=S(p), recordedUserId=S(s), studentDataId=S(cur["id"]),
            accessedResource=S(f"Student data: {attr_name[a]} (Fall 2026)"), accessedAt=T(base), isFlagged=BIT(False), status=None)
        al += 1
    if granted_consent(s, p, GPA, commit_at) and p in (purposes["Academic advising"],):
        n_rows = sum(1 for x in res.values() if x["student"] == s and (x["grade"] is not None or x["status"]))
        add("ACCESSLOGS", accessLogId=S(gid("al-res", cid)), accessorId=S(adv), consentPurposeId=S(p), recordedUserId=S(s), studentDataId=None,
            accessedResource=S(f"Academic results: grades of {n_rows} subject(s)"), accessedAt=T(commit_at + dt.timedelta(hours=7)),
            isFlagged=BIT(False), status=None)
        al += 1
# one late-night bulk read, flagged for the steward to review
s_any = enrolled_active[5]
add("ACCESSLOGS", accessLogId=S(gid("al-flag-results")), accessorId=S(AA_LAN), consentPurposeId=S(AADV), recordedUserId=S(s_any), studentDataId=None,
    accessedResource=S("Academic results: attendance of 46 subject(s)"), accessedAt=T(dt.datetime(2026, 11, 17, 18, 47)), isFlagged=BIT(True),
    status=S("unreviewed"))
activity(STEWARD, "accessLogEntryFlagged", f"Flagged an access log entry of {name_of(AA_LAN)} on {name_of(s_any)} (academic results read at night)", dt.datetime(2026, 11, 18, 1, 15))

# notification links to a case use the lowercase id (Guid.ToString()), as the API writes them and the unread count matches them
for r in rows("NOTIFICATIONS"):
    u = val(r["redirectUrl"])
    if u and u.startswith("/cases/"): r["redirectUrl"] = S(u.lower())

# ================================================================
# 7. write the script
# ================================================================
def lit(x):
    if x is None: return "NULL"
    k, v = x
    if k == "str": return "N'" + v.replace("'", "''") + "'"
    if k == "lit": return "'" + v + "'"
    return v


def counts():
    return {t: len(TB[t]) for t in create_order}


out = []
w = out.append
total = sum(counts().values())
w("/* =====================================================================")
w("   AdviseCare - FULL mock data (18/11/2026) for the current AdviseCareDB schema (52 tables)")
w("   Run AFTER Documentation/AdviseCare_Database_Script.sql. This script EMPTIES all 52 tables and reloads them")
w(f"   in one transaction ({total:,} rows). Re-running it gives the same data.")
w("   Generated by AdviseCare_Mock_Data/generator/generate_full_mock_sql.py - edit the generator, not this file.")
w("")
w("   What it contains")
w("   - Everything of section 4 of the database script (accounts, password 123456; master data; consents; cases; welfare; logs).")
w("   - STUDENTDATA values of Summer 2026 and Fall 2026, exactly what POST /api/StudentData/import would write for")
w("     mock_studentdata_summer2026.xlsx, _fall2026.xlsx, _fall2026_update.xlsx and _student_gmail.csv (missing rows included),")
w(f"     ENCRYPTED with master key version {MK_VERSION} of AdviseCare_API/AdviseCare.API/appsettings.json (AES-256-GCM, HKDF-wrapped keys).")
w("     The API must run with that same master key; with another key the values cannot be decrypted (regenerate with your key).")
w("     Do NOT import those 4 files again after this script (the values would be doubled); the invalid/archived files can still be imported.")
w("   - Pending / approved / rejected / withdrawn CORRECTIONREQUESTS on Fall 2026 values (encrypted like the API does).")
w("   - INDICATORFLAGS raised by the active indicators on Fall 2026 (open, requested, caseOpened, dismissed, cleared).")
w("   - CASEMESSAGES on every case with an advisor, CASEMESSAGEATTACHMENTS (real small PDF/TXT/PNG/CSV files), more CASE_ATTRIBUTE rows.")
w("   - Transcript import: 1 committed batch (the 100 PDFs in AdviseCare_Mock_Data/Transcripts with their real SHA-256),")
w("     1 cancelled batch (the 3 PDFs in Transcripts/invalid), 1 previewed batch (Attendance_SE180001.pdf uploaded again);")
w("     STUDENTSUBJECTRESULTS, the subjects and past terms the commit creates, curriculumCode on every student profile.")
w("   Reference date: 2026-11-18 (Fall 2026 is the current term). Times are UTC.")
w("   ===================================================================== */")
w("")
w("USE [AdviseCareDB];")
w("GO")
w("SET NOCOUNT ON;")
w("SET XACT_ABORT ON;")
w("BEGIN TRY")
w("BEGIN TRANSACTION;")
w("")
w("/* ---------- 1. Empty every table (children first) ---------- */")
for t in reversed(create_order):
    w(f"DELETE FROM [dbo].[{t}];")
w("")
w("/* ---------- 2. Insert (parents first) ---------- */")
CH = 500
for t in create_order:
    rs = TB[t]
    if not rs:
        w(f"-- {t}: 0 rows"); w(""); continue
    cols = [c for c in table_cols[t] if any(c in r for r in rs)]
    w(f"-- {t}: {len(rs)} rows")
    for i in range(0, len(rs), CH):
        w(f"INSERT INTO [dbo].[{t}] ({', '.join('[' + c + ']' for c in cols)}) VALUES")
        chunk = rs[i:i + CH]
        w(",\n".join("    (" + ", ".join(lit(r.get(c)) for c in cols) + ")" for r in chunk) + ";")
    w("")
w("COMMIT TRANSACTION;")
w("PRINT N'AdviseCare full mock data loaded.';")
w("END TRY")
w("BEGIN CATCH")
w("    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;")
w("    THROW;")
w("END CATCH;")
w("GO")
open(OUT, "w", encoding="utf-8-sig", newline="\r\n").write("\n".join(out) + "\n")

summary = dict(counts=counts(), flags=Counter(val(f["status"]) for f in rows("INDICATORFLAGS")),
               corrections=Counter(val(r["status"]) for r in rows("CORRECTIONREQUESTS")), messages=msg_count, attachments=att_count,
               case_attributes_added=ca_count, access_logs_added=al, new_subjects=len(new_subjects), new_terms=sorted(new_terms, key=term_start),
               results=len(res), keys_active=len(active_keys))

print(json.dumps(summary, indent=1, default=str))
