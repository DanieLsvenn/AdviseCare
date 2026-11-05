#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AdviseCare - full mock data generator
=====================================
Writes AdviseCare_Mock_Data.sql: a re-runnable script that clears every AdviseCare table and fills all 43 tables
with one coherent, time-consistent story (reference date 2026-11-05), including REAL encrypted STUDENTDATA values.

Why a generator: STUDENTDATA.dataValue, ATTRIBUTEKEYS.wrappedKey and CORRECTIONREQUESTS values are AES-256-GCM
envelopes tied to the API master key (AesGcmDataEncryptor). This script reproduces that format exactly, so the API
can decrypt the seeded values as long as it runs with the same Encryption:MasterKeys as the one used here.

Usage (from AdviseCare/Documentation/MockData):
    pip install cryptography
    python generate_mock_data.py                                   # reads ../../AdviseCare_API/AdviseCare.API/appsettings.json
    python generate_mock_data.py --appsettings path/to/appsettings.json --out AdviseCare_Mock_Data.sql
    python generate_mock_data.py --master-key <base64 32 bytes> --master-key-version 1

Run order in SQL Server: AdviseCare_Database_Script.sql (creates the tables) -> AdviseCare_Mock_Data.sql.
"""
import argparse
import base64
import datetime as dt
import json
import os
import random
import re
import sys
import unicodedata
import uuid
from collections import defaultdict

try:
    from cryptography.hazmat.primitives import hashes
    from cryptography.hazmat.primitives.ciphers.aead import AESGCM
    from cryptography.hazmat.primitives.kdf.hkdf import HKDF
except ImportError:  # pragma: no cover
    sys.exit("Missing package: pip install cryptography")

# =====================================================================================================================
# 0. Settings and helpers
# =====================================================================================================================
NOW = dt.datetime(2026, 11, 5, 3, 0)          # 10:00 Vietnam time, reference "today" of the mock data
SEED = 20261105
rng = random.Random(SEED)
NS = uuid.UUID("6f1c2a7e-3b9d-4c55-9a1e-2b7d0c8f4a10")
MOCK_PASSWORD_HASH = "$2a$11$1qow.v9rCZj8gaUToiAd8.VTPgfzBFEzWw6SK6vHa.kWx9LPPmzw."   # BCrypt of 123456

COLS = {
    'FACULTIES': ['facultyId', 'facultyCode', 'facultyName', 'status'],
    'ACCOUNTS': ['accountId', 'facultyId', 'avatarUrl', 'email', 'fullName', 'googleId', 'authProvider', 'passwordHash', 'resetToken', 'resetTokenExpiresAt', 'userName', 'createdAt', 'linkStatus', 'preferredLanguage', 'status'],
    'STUDENTPROFILES': ['studentProfileId', 'studentId', 'studentCode', 'academicStatus'],
    'ROLES': ['roleId', 'roleName'],
    'ACCOUNT_ROLE': ['accountId', 'roleId', 'assignedAt'],
    'SESSIONS': ['sessionId', 'accountId', 'deviceInfo', 'loginAt', 'logoutAt', 'isActive'],
    'NOTIFICATIONS': ['notificationId', 'accountId', 'message', 'redirectUrl', 'title', 'createdAt', 'isRead'],
    'NOTIFICATIONPREFERENCES': ['notificationPreferenceId', 'accountId', 'channel', 'isEnabled'],
    'ACTIVITYLOGS': ['activityLogId', 'accountId', 'activityDetail', 'activityType', 'occurredAt'],
    'PROGRAMMES': ['programmeId', 'facultyId', 'programmeCode', 'programmeName', 'status'],
    'ACADEMICTERMS': ['termId', 'endDate', 'startDate', 'termName', 'status'],
    'DATAATTRIBUTES': ['dataAttributeId', 'attributeDescription', 'attributeName', 'dataType', 'status'],
    'ATTRIBUTEKEYS': ['attributeKeyId', 'dataAttributeId', 'studentId', 'wrappedKey', 'masterKeyVersion', 'createdAt', 'destroyedAt', 'status'],
    'SUBJECTS': ['subjectId', 'facultyId', 'subjectCode', 'subjectName', 'status'],
    'DATASOURCES': ['dataSourceId', 'createdBy', 'dataAttributeId', 'connectionConfig', 'fieldMapping', 'sourceName', 'sourceType', 'status'],
    'INGESTIONSCHEDULES': ['ingestionScheduleId', 'dataSourceId', 'scheduledStartTime', 'ingestionFrequency', 'lastRun', 'numberOfSuccesses', 'numberOfFails', 'onFailure', 'failureRetryInterval', 'numberOfRetries', 'status'],
    'INGESTIONJOBS': ['ingestionJobId', 'ingestionScheduleId', 'expectedStartTime', 'completedAt', 'errorDetails', 'status'],
    'STUDENTDATA': ['studentDataId', 'attributeKeyId', 'dataAttributeId', 'dataSourceId', 'studentId', 'termId', 'dataValue', 'provenanceNote', 'erasedAt', 'transactionFrom', 'validFrom', 'isMissing', 'status'],
    'ACCOUNT_SUBJECT': ['accountId', 'subjectId', 'assignedAt'],
    'CONSENTPURPOSES': ['consentPurposeId', 'createdBy', 'description', 'purposeName', 'status'],
    'SUPPORTCATEGORIES': ['supportCategoryId', 'consentPurposeId', 'createdBy', 'categoryName', 'description', 'supportType', 'status'],
    'CASES': ['caseId', 'advisorId', 'openedBy', 'studentId', 'subjectId', 'supportCategoryId', 'requestReason', 'closedAt', 'openedAt', 'currentStage', 'priority', 'status'],
    'CASECHANGEREQUESTS': ['caseChangeRequestId', 'caseId', 'programmeHeadId', 'proposedAdvisorId', 'requestedBy', 'decisionNotes', 'reason', 'decidedAt', 'requestedAt', 'status'],
    'CASENOTES': ['caseNoteId', 'authorId', 'caseId', 'noteText', 'createdAt', 'editedAt', 'isFinalized'],
    'CASESTAGEHISTORIES': ['caseStageHistoryId', 'caseId', 'changedBy', 'reason', 'changedAt', 'fromStage', 'toStage'],
    'REFERRALS': ['referralId', 'caseId', 'fromAccountId', 'toAccountId', 'referralReason', 'acceptedAt', 'createdAt', 'priority', 'status'],
    'CORRECTIONREQUESTS': ['correctionRequestId', 'reviewedBy', 'studentDataId', 'studentId', 'currentValue', 'decisionNotes', 'explanation', 'proposedValue', 'decidedAt', 'submittedAt', 'status'],
    'INTERVENTIONS': ['interventionId', 'caseId', 'officerId', 'notes', 'outcome', 'interventionDate', 'interventionType', 'isClosed', 'status'],
    'SUPPORTGRANTS': ['supportGrantId', 'interventionId', 'grantDescription', 'grantType', 'issuedAt', 'status'],
    'EXTERNALSERVICES': ['externalServiceId', 'contactInfo', 'serviceName', 'serviceType', 'isActive'],
    'EXTERNALREFERRALS': ['externalReferralId', 'caseId', 'externalServiceId', 'referredBy', 'followUpDueAt', 'followUpOutcome', 'referredAt', 'isOverdue', 'status'],
    'ESCALATIONS': ['escalationId', 'caseId', 'escalatedBy', 'escalatedTo', 'reason', 'response', 'escalatedAt', 'status'],
    'PURPOSE_ATTRIBUTE': ['consentPurposeId', 'dataAttributeId'],
    'CONSENTS': ['consentId', 'consentPurposeId', 'dataAttributeId', 'studentId', 'expiresAt', 'grantedAt', 'withdrawnAt', 'status'],
    'CONSENTHISTORIES': ['consentHistoryId', 'changedBy', 'consentId', 'changedAt', 'newStatus', 'previousStatus'],
    'CONSENTREQUESTS': ['consentRequestId', 'advisorId', 'consentPurposeId', 'dataAttributeId', 'studentId', 'purposeStated', 'requestedAt', 'respondedAt', 'status'],
    'ACCESSPOLICIES': ['accessPolicyId', 'consentPurposeId', 'createdBy', 'roleId', 'status'],
    'ACCESSLOGS': ['accessLogId', 'accessorId', 'consentPurposeId', 'recordedUserId', 'studentDataId', 'accessedResource', 'accessedAt', 'isFlagged', 'status'],
    'EMERGENCYACCESSREQUESTS': ['emergencyAccessRequestId', 'requestedBy', 'stewardId', 'studentId', 'justification', 'rejectionReason', 'requestedAt', 'decidedAt', 'scopeExpiresAt', 'status'],
    'ERASUREREQUESTS': ['erasureRequestId', 'attributeKeyId', 'consentId', 'executedBy', 'executedAt', 'requestedAt', 'status'],
    'REPORTS': ['reportId', 'generatedBy', 'termId', 'filePath', 'reportType', 'generatedAt', 'status'],
    'BACKUPS': ['backupId', 'createdBy', 'backupTimestamp', 'sizeBytes', 'status'],
    'RESTOREOPERATIONS': ['restoreOperationId', 'backupId', 'executedBy', 'integrityCheckResult', 'executedAt', 'status'],
}
TABLE_ORDER = list(COLS.keys())   # parents first (same order as AdviseCare_Database_Script.sql)

DB = defaultdict(list)


def add(table, **row):
    missing = set(COLS[table]) - set(row)
    extra = set(row) - set(COLS[table]) - {k for k in row if k.startswith('_')}
    if missing or extra:
        raise ValueError(f"{table}: missing {missing}, extra {extra}")
    DB[table].append(row)
    return row


def gid(*parts):
    return str(uuid.uuid5(NS, '|'.join(str(p) for p in parts))).upper()


def T(s):
    """'2026-10-05 14:30' -> datetime"""
    return dt.datetime.strptime(s, '%Y-%m-%d %H:%M') if ' ' in s else dt.datetime.strptime(s, '%Y-%m-%d')


def H(h):
    return dt.timedelta(hours=h)


def D(d):
    return dt.timedelta(days=d)


def minute(t):
    return t.replace(second=0, microsecond=0)


def rand_between(a, b):
    return minute(a + (b - a) * rng.random())


def office(t):
    """Move a time into Vietnam office-ish hours (07:30-21:30 VN = 00:30-14:30 UTC) without going past NOW."""
    if 0 <= t.hour < 15 and not (t.hour == 0 and t.minute < 30):
        return minute(t)
    nt = (t + D(1)).replace(hour=rng.randint(1, 9), minute=rng.randint(0, 59)) if t.hour >= 15 else t.replace(hour=rng.randint(1, 9), minute=rng.randint(0, 59))
    return minute(nt) if nt < NOW - H(1) else minute(t)


def unaccent(s):
    s = s.replace('đ', 'd').replace('Đ', 'D')
    return ''.join(c for c in unicodedata.normalize('NFD', s) if unicodedata.category(c) != 'Mn')


def pick(seq):
    return seq[rng.randrange(len(seq))]


def wpick(pairs):
    total = sum(w for _, w in pairs)
    r = rng.random() * total
    for v, w in pairs:
        r -= w
        if r <= 0:
            return v
    return pairs[-1][0]


def clamp(v, a, b):
    return max(a, min(b, v))


# ---------------------------------------------------------------------------------------------------------------------
# Encryption (exact copy of AesGcmDataEncryptor + AttributeKeyService formats)
# ---------------------------------------------------------------------------------------------------------------------
WRAP_PREFIX = "AdviseCare:wrap:v1:"


class Crypto:
    def __init__(self, master_key_b64, version):
        self.master = base64.b64decode(master_key_b64)
        if len(self.master) != 32:
            raise ValueError("The master key must be 32 bytes in base64")
        self.version = version

    @staticmethod
    def n(g):  # Guid.ToString("N")
        return g.replace('-', '').lower()

    def key_context(self, key_id, student_id, attr_id):
        return f"{self.n(key_id)}:{self.n(student_id)}:{self.n(attr_id)}"

    @staticmethod
    def seal(key, plaintext, aad):
        nonce = os.urandom(12)
        ct_tag = AESGCM(key).encrypt(nonce, plaintext, aad)
        return nonce + ct_tag[-16:] + ct_tag[:-16]          # nonce | tag | ciphertext

    @staticmethod
    def open(key, data, aad):
        nonce, tag, ct = data[:12], data[12:28], data[28:]
        return AESGCM(key).decrypt(nonce, ct + tag, aad)

    def wrapping_key(self, ctx):
        return HKDF(algorithm=hashes.SHA256(), length=32, salt=None,
                    info=(WRAP_PREFIX + ctx).encode()).derive(self.master)

    def new_wrapped_key(self, ctx):
        data_key = os.urandom(32)
        wrapped = base64.b64encode(self.seal(self.wrapping_key(ctx), data_key, (WRAP_PREFIX + ctx).encode())).decode()
        return wrapped, data_key

    def unwrap(self, wrapped, ctx):
        return self.open(self.wrapping_key(ctx), base64.b64decode(wrapped), (WRAP_PREFIX + ctx).encode())

    def value_aad(self, key_id, student_id, attr_id, row_id, field):
        return f"AdviseCare:value:v1:{self.n(key_id)}:{self.n(student_id)}:{self.n(attr_id)}:{self.n(row_id)}:{field}".encode()

    def encrypt_value(self, data_key, key_id, student_id, attr_id, row_id, field, plaintext):
        return bytes([1]) + self.seal(data_key, plaintext.encode('utf-8'), self.value_aad(key_id, student_id, attr_id, row_id, field))

    def decrypt_value(self, data_key, key_id, student_id, attr_id, row_id, field, payload):
        assert payload[0] == 1
        return self.open(data_key, payload[1:], self.value_aad(key_id, student_id, attr_id, row_id, field)).decode('utf-8')


# ---------------------------------------------------------------------------------------------------------------------
# Notifications / activity logs (collected from every event, emitted at the end)
# ---------------------------------------------------------------------------------------------------------------------
SEED_IDS = set()


GUID_RE = re.compile(r'[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}')


def lower_guids(text):
    """The API formats Guids in text with Guid.ToString() = lowercase (e.g. /cases/{caseId}); keep the same form so URL matching works."""
    return GUID_RE.sub(lambda m: m.group(0).lower(), text) if text else text


def notify(account_id, title, message, url, t):
    if account_id in SEED_IDS:
        return
    url, message = lower_guids(url), lower_guids(message)
    age = (NOW - t).total_seconds() / 86400
    is_read = age > 6 and rng.random() < 0.9 or 2 < age <= 6 and rng.random() < 0.6 or age <= 2 and rng.random() < 0.2
    add('NOTIFICATIONS', notificationId=gid('notif', len(DB['NOTIFICATIONS'])), accountId=account_id, message=message,
        redirectUrl=url, title=title, createdAt=minute(t), isRead=is_read)


def log(account_id, activity_type, detail, t):
    if account_id in SEED_IDS:
        return
    detail = lower_guids(detail)
    add('ACTIVITYLOGS', activityLogId=gid('log', len(DB['ACTIVITYLOGS'])), accountId=account_id,
        activityDetail=detail, activityType=activity_type, occurredAt=minute(t))


# =====================================================================================================================
# 1. Master data
# =====================================================================================================================
ROLE = {
    'admin': ('10D24AC4-229F-57BE-9C5C-454C870504B4', 'System Administrator'),
    'steward': ('B09F7D95-9D34-5DB1-BF1A-A4F55D21C3DA', 'Data Protection Steward'),
    'aa': ('3ED661F9-DC4F-5353-A6E9-C4224A296BF1', 'Academic Advisor'),
    'wa': ('BF84B96D-99E0-5D30-B618-F7B8196B56B5', 'Wellbeing Advisor'),
    'wo': ('2843CEE3-64AE-5054-A8D9-A77F17AEB6F5', 'Student Welfare Officer'),
    'student': ('6ED15B52-C53F-58EE-95F1-8B9941C5868E', 'Student'),
    'ph': ('7F0590EB-855C-5C59-A566-4C9A7E55C8AE', 'Programme Head'),
}
for rid, rname in ROLE.values():
    add('ROLES', roleId=rid, roleName=rname)

FAC = {
    'SE': ('7FFCF736-935D-5867-B983-374AEC2D6242', 'Software Engineering', 'active'),
    'BA': ('30F2B5EB-34A3-575F-8D08-F31A87D60F16', 'Business Administration', 'active'),
    'AI': (gid('fac', 'AI'), 'Artificial Intelligence', 'active'),
    'IA': (gid('fac', 'IA'), 'Information Assurance', 'active'),
    'GD': (gid('fac', 'GD'), 'Graphic Design', 'active'),
    'JP': (gid('fac', 'JP'), 'Japanese Language', 'archived'),
}
for code, (fid, name, st) in FAC.items():
    add('FACULTIES', facultyId=fid, facultyCode=code, facultyName=name, status=st)

for code, fac, name, st, pid in [
    ('BSE', 'SE', 'Bachelor of Software Engineering', 'active', '0618D3BB-012E-51E5-AEA1-32DBF236E885'),
    ('BAI', 'AI', 'Bachelor of Artificial Intelligence', 'active', None),
    ('BIA', 'IA', 'Bachelor of Information Assurance', 'active', None),
    ('BMKT', 'BA', 'Bachelor of Business Administration - Digital Marketing', 'active', None),
    ('BFIN', 'BA', 'Bachelor of Business Administration - Finance', 'active', None),
    ('BGD', 'GD', 'Bachelor of Graphic Design', 'active', None),
    ('BIB', 'BA', 'Bachelor of International Business (closed intake)', 'archived', None),
    ('BJP', 'JP', 'Bachelor of Japanese Language', 'archived', None),
]:
    add('PROGRAMMES', programmeId=pid or gid('prog', code), facultyId=FAC[fac][0], programmeCode=code, programmeName=name, status=st)

TERM = {
    'Fall 2025': (gid('term', 'Fall 2025'), '2025-09-08', '2025-12-28', 'past'),
    'Spring 2026': ('2929FCA6-0DE2-5976-AAFB-B698596B5C2E', '2026-01-05', '2026-04-26', 'past'),
    'Summer 2026': ('76DFFA8A-4335-5242-A551-C83A1F10A78D', '2026-05-04', '2026-08-23', 'past'),
    'Fall 2026': ('269FD2BD-0E00-525C-8896-D22F6B5B2D31', '2026-09-07', '2026-12-27', 'current'),
    'Spring 2027': ('EF3254A4-6B8D-5C7D-9921-D5602093842D', '2027-01-04', '2027-04-25', 'upcoming'),
}
TERM_ID = {k: v[0] for k, v in TERM.items()}
TERM_START = {k: T(v[1]) for k, v in TERM.items()}
for name, (tid, s, e, st) in TERM.items():
    add('ACADEMICTERMS', termId=tid, endDate=T(e), startDate=T(s), termName=name, status=st)

# Data attributes (same ids/names as the main script)
ATTRS = [
    ('8A95DB0E-4F7F-5086-B9AD-8DDFEBC95618', 'GPA', 'numeric', 'active', 'Your cumulative grade point average for the term, on a 10-point scale'),
    ('61ABB48C-FCBB-5292-8689-1263F7F5A21D', 'Attendance rate', 'numeric', 'active', 'How often you attended your classes this term (percentage of attended slots, 0-100)'),
    ('607DBFCA-8BCF-5F14-B128-C91B483E5D46', 'Credits earned', 'numeric', 'active', 'Credits you have passed so far in the programme'),
    ('2B9B608A-C7DA-56BD-BD3B-BE65D5F742E9', 'Credits attempted', 'numeric', 'active', 'Credits you registered for this term'),
    ('90174836-5490-5A0B-8938-74C4036DE15C', 'Failed subjects', 'numeric', 'active', 'Number of subjects you failed this term'),
    ('09FC457D-EDDF-5CE3-A6AC-8A7D49E50EB9', 'Absent slots', 'numeric', 'active', 'Number of class slots you were absent from this term'),
    ('B24AF0AD-E23B-58D0-93EC-AA6276AEC5D7', 'Late submissions', 'numeric', 'active', 'Number of assignments you submitted after the deadline this term'),
    ('6209A1F0-147F-5214-89AE-BA87651F785C', 'LMS logins per week', 'numeric', 'active', 'Average number of times you signed in to the learning system per week'),
    ('852DDEFF-0B5F-5D87-BADC-445C1A48FC0F', 'Library visits', 'numeric', 'active', 'Number of library visits recorded this term'),
    ('4FFD5A76-4D95-5F1B-95CB-7B991A7321A6', 'Tuition outstanding', 'numeric', 'active', 'Tuition still to be paid this term, in million VND'),
    ('0507C9AE-1465-5719-AFBA-E95EF0945680', 'Part-time work hours', 'numeric', 'active', 'Hours per week you work in a part-time job'),
    ('142CB05A-56FB-5746-8F2D-89C80B66A8FE', 'Commute time', 'numeric', 'active', 'Usual one-way travel time to campus, in minutes'),
    ('580B177A-2FB3-508C-B05A-6D52E73B2E82', 'Accommodation status', 'categorical', 'active', 'Where you are living this term (dormitory / rented / family / other)'),
    ('B122CC0A-9AF7-5A24-BF85-53AA657B4109', 'Scholarship status', 'categorical', 'active', 'Your scholarship this term (none / partial / full)'),
    ('397BDBD1-679B-52F4-B732-883F9FE0FD64', 'Academic warning level', 'categorical', 'active', 'Your academic warning this term (none / level1 / level2 / probation)'),
    ('A129D94E-85CA-5CE2-AEE4-1561995004D3', 'English level', 'categorical', 'active', 'Your latest English placement result (A2 / B1 / B2 / C1)'),
    ('C3D53A5C-68C4-50E5-B460-B628A368394A', 'Family income bracket', 'categorical', 'active', 'Your family income bracket on record (low / middle / high)'),
    ('D025CFC3-9A36-5206-9AC5-743A92297BBD', 'Transport mode', 'categorical', 'active', 'How you usually travel to campus (motorbike / bus / walk / car / bicycle)'),
    ('C0CCEDC3-61AB-56EE-92C9-1219F899A3CA', 'Financial difficulty', 'boolean', 'active', 'Whether you have reported financial difficulty'),
    ('64D9FABF-E417-5FEA-9FFD-F07AC749D566', 'Tuition overdue', 'boolean', 'active', 'Whether a tuition payment of this term is past its due date'),
    ('5CF4F201-BDDD-537B-8669-A6D7406177CF', 'Has part-time job', 'boolean', 'active', 'Whether you work a part-time job this term'),
    ('C84CC473-EB1C-5AF4-9235-4534A7243B42', 'First-generation student', 'boolean', 'active', 'Whether you are the first in your family to attend university'),
    ('1E1FC43F-D279-59E6-9230-9D09A81D3A71', 'Receives meal support', 'boolean', 'active', 'Whether you receive the campus meal support programme'),
    ('191D4DDB-8B96-5FF7-90D9-D1150EBF9165', 'Club membership count', 'numeric', 'archived', 'Number of student clubs you joined (no longer collected)'),
    ('8979B92E-3303-5EDF-A0C0-A925271D1BDD', 'Dormitory block', 'categorical', 'archived', 'Your dormitory block (no longer collected, replaced by Accommodation status)'),
]
A = {name: aid for aid, name, *_ in ATTRS}
ATTR_NAME = {aid: name for aid, name, *_ in ATTRS}
ATTR_TYPE = {aid: typ for aid, _, typ, *_ in ATTRS}
ATTR_STATUS = {aid: st for aid, _, _, st, _ in ATTRS}
for aid, name, typ, st, desc in ATTRS:
    add('DATAATTRIBUTES', dataAttributeId=aid, attributeDescription=desc, attributeName=name, dataType=typ, status=st)

SUBJ = {}   # code -> (id, faculty, status)
for code, fac, name, st, sid in [
    ('PRN212', 'SE', 'Basic Cross-Platform Application Programming With .NET', 'active', 'DB6EAD68-A440-5243-A770-B23709430D78'),
    ('SWP391', 'SE', 'Software Development Project', 'active', 'AC4AD437-135F-5FD6-8580-94E5E5668110'),
    ('PRF192', 'SE', 'Programming Fundamentals', 'active', '8E247EDF-E1AF-5399-B433-DCCB2D67A03A'),
    ('CSD201', 'SE', 'Data Structures and Algorithms', 'active', 'FB2D105D-705E-519E-8339-377303AE5B08'),
    ('DBI202', 'SE', 'Database Systems', 'active', 'FF9ADEAC-59D3-5A20-8C44-EB1F06B56133'),
    ('SWE201', 'SE', 'Introduction to Software Engineering', 'active', 'A3179490-31CA-5741-BC2E-7FAFE842140A'),
    ('MAE101', 'SE', 'Mathematics for Engineering', 'active', '116478D6-2CD7-5C43-997C-70EE05D0539B'),
    ('OSG202', 'SE', 'Operating Systems', 'archived', 'E1BC7A6B-8159-586E-A142-397E174A6F72'),
    ('PRJ301', 'SE', 'Java Web Application Development', 'active', None),
    ('SWT301', 'SE', 'Software Testing', 'active', None),
    ('MKT101', 'BA', 'Marketing Principles', 'active', 'EF5E237C-2785-5C25-92CC-12AD48A9A11B'),
    ('ECO111', 'BA', 'Microeconomics', 'active', 'D08967B2-5557-5B01-B1BF-FA49548AD476'),
    ('ACC101', 'BA', 'Principles of Accounting', 'active', 'A4A7996F-40D8-5F37-88C3-C76D41EF45F9'),
    ('FIN202', 'BA', 'Corporate Finance', 'active', 'D4DCC787-AA54-5C38-BCB6-1AA46C969B3A'),
    ('MGT103', 'BA', 'Introduction to Management', 'active', None),
    ('DMK301', 'BA', 'Digital Marketing', 'active', None),
    ('AIL303', 'AI', 'Machine Learning', 'active', None),
    ('DPL302', 'AI', 'Deep Learning', 'active', None),
    ('MAS291', 'AI', 'Statistics and Probability', 'active', None),
    ('NLP301', 'AI', 'Natural Language Processing', 'active', None),
    ('IAO202', 'IA', 'Information Security Fundamentals', 'active', None),
    ('NWC203', 'IA', 'Computer Networking', 'active', None),
    ('CRY302', 'IA', 'Applied Cryptography', 'active', None),
    ('IAA202', 'IA', 'Risk Management in Information Systems', 'active', None),
    ('DRP101', 'GD', 'Drawing Principles', 'active', None),
    ('VCM202', 'GD', 'Visual Communication', 'active', None),
    ('TPG203', 'GD', 'Typography', 'active', None),
    ('ANS301', 'GD', '3D Animation (old curriculum)', 'archived', None),
]:
    sid = sid or gid('subj', code)
    SUBJ[code] = (sid, fac, st)
    add('SUBJECTS', subjectId=sid, facultyId=FAC[fac][0], subjectCode=code, subjectName=name, status=st)
SUBJ_CODE = {v[0]: k for k, v in SUBJ.items()}
ACTIVE_SUBJ_BY_FAC = defaultdict(list)
for code, (sid, fac, st) in SUBJ.items():
    if st == 'active':
        ACTIVE_SUBJ_BY_FAC[fac].append(code)

# =====================================================================================================================
# 2. Accounts
# =====================================================================================================================
ACC = {}   # id -> dict(name,email,user,roles,fac,status,created,...)


def make_account(aid, user, email, name, fac, roles, created, status='active', link='linked', lang='vi',
                 password_hash=MOCK_PASSWORD_HASH, google=True):
    google_id = f"1000{uuid.uuid5(NS, 'google|' + aid).int % 10**17:017d}" if link == 'linked' and google else None
    acc = dict(id=aid, user=user, email=email, name=name, fac=fac, roles=set(roles), created=created, status=status, link=link)
    ACC[aid] = acc
    add('ACCOUNTS', accountId=aid, facultyId=FAC[fac][0] if fac else None, avatarUrl=None, email=email, fullName=name,
        googleId=google_id, authProvider='google' if google_id else None, passwordHash=password_hash, resetToken=None,
        resetTokenExpiresAt=None, userName=user, createdAt=created, linkStatus=link, preferredLanguage=lang, status=status)
    for r in roles:
        add('ACCOUNT_ROLE', accountId=aid, roleId=ROLE[r][0], assignedAt=created)
    return acc


# 2.1 The 7 seed test accounts: same ids and hashes as AdviseCare_Database_Script.sql, kept clean on purpose
SEEDS = [
    ('D2AA2929-437D-50E2-A61D-40EA2B09010E', None, 'admin@gmail.com', 'System Administrator', '$2a$11$1liYqIr7kcDxTzoTN59b6us3o6m6EMaya6FZ9tjsDN3g0231ERA7m', 'admin', 'admin'),
    ('4FA71983-548F-5C26-BEFE-0E089DF4956F', None, 'steward@gmail.com', 'Data Protection Steward', '$2a$11$lTeQom.w4NNhqDBbm.GcteEf7ZxdV6ZLH5Y2Zx0lxAdc/abs8DAjG', 'steward', 'steward'),
    ('16C05C9C-133D-5FAC-B287-37C15764B71E', 'SE', 'academicadvisor@gmail.com', 'Academic Advisor', '$2a$11$.M/cKreFct0akjeuyMIGj.eyuHnZ9D8C5anorpNHe0m2pU9LLdwnO', 'academicadvisor', 'aa'),
    ('B6944BEC-089A-5878-9C34-EED3583DAABF', 'SE', 'wellbeingadvisor@gmail.com', 'Wellbeing Advisor', '$2a$11$1wgRiVlejGNcTh6I9RWE4O2L6wIVFs0Hy3ClgJULcTRTmHpxvCeKW', 'wellbeingadvisor', 'wa'),
    ('0CAA0436-8CFC-5AB5-B05E-025D8C0280C5', None, 'welfareofficer@gmail.com', 'Student Welfare Officer', '$2a$11$QPB3eTzt1Wz/1cc9QVRxBeEASLzFmLvGFEt5hmf8GMs78PZUVabBW', 'welfareofficer', 'wo'),
    ('6341514A-BC92-5729-9218-7DA322CDCD4E', 'SE', 'student@gmail.com', 'Student', '$2a$11$1qow.v9rCZj8gaUToiAd8.VTPgfzBFEzWw6SK6vHa.kWx9LPPmzw.', 'student', 'student'),
    ('3978D886-B5A9-5108-89A0-5DD87AA154B4', 'SE', 'programmehead@gmail.com', 'Programme Head', '$2a$11$L.qbXWklG.R4RMzNMa25c.v.LHdbMdE244vgFogVXfFQCBxcvMSRu', 'programmehead', 'ph'),
]
for aid, fac, email, name, ph, user, role in SEEDS:
    SEED_IDS.add(aid)
    make_account(aid, user, email, name, fac, [role], T('2026-08-20 02:00'), link='notLinked', password_hash=ph, google=False)
add('STUDENTPROFILES', studentProfileId='24F1DD50-5D23-5928-ABDC-F5976888C001', studentId='6341514A-BC92-5729-9218-7DA322CDCD4E',
    studentCode='SE180001', academicStatus='enrolled')

# 2.2 Mock staff (password 123456)
STAFF_DEF = [
    # key, username, full name, faculty, roles, created, status, link, lang, subjects
    ('admin02', 'admin02', 'Trần Quốc Bảo', None, ['admin'], '2026-04-01 02:00', 'active', 'linked', 'vi', []),
    ('admin03', 'admin03', 'Võ Thị Kim Ngân', None, ['admin'], '2026-04-01 02:30', 'active', 'emailSent', 'en', []),
    ('steward02', 'steward02', 'Lê Thị Thu Hà', None, ['steward'], '2026-04-02 03:00', 'active', 'linked', 'en', []),
    ('steward03', 'steward03', 'Đặng Minh Châu', None, ['steward'], '2026-04-02 03:10', 'active', 'linked', 'vi', []),
    ('ph.quanpm', 'ph.quanpm', 'Phan Minh Quân', 'SE', ['ph'], '2026-04-03 02:00', 'active', 'linked', 'vi', []),
    ('aa.hungpt', 'aa.hungpt', 'Phạm Thanh Hùng', 'SE', ['aa'], '2026-04-03 03:00', 'active', 'linked', 'vi', ['PRN212', 'SWP391', 'PRF192', 'PRJ301']),
    ('aa.lannt', 'aa.lannt', 'Nguyễn Thị Lan', 'SE', ['aa'], '2026-04-03 03:05', 'active', 'linked', 'vi', ['CSD201', 'DBI202', 'MAE101']),
    ('aa.khoavd', 'aa.khoavd', 'Vũ Đăng Khoa', 'SE', ['aa', 'ph'], '2026-04-03 03:10', 'active', 'linked', 'en', ['SWE201', 'SWT301']),
    ('aa.thaonp', 'aa.thaonp', 'Ngô Phương Thảo', 'SE', ['aa'], '2026-04-03 03:15', 'active', 'emailSent', 'vi', []),
    ('aa.sonhm', 'aa.sonhm', 'Hồ Minh Sơn', 'SE', ['aa'], '2026-04-03 03:20', 'deactivated', 'notLinked', 'vi', ['OSG202']),
    ('aa.phongld', 'aa.phongld', 'Lưu Đình Phong', 'AI', ['aa'], '2026-04-03 03:25', 'active', 'linked', 'vi', ['AIL303', 'DPL302', 'NLP301']),
    ('aa.vylt', 'aa.vylt', 'Lâm Tường Vy', 'AI', ['aa'], '2026-04-03 03:30', 'active', 'linked', 'en', ['MAS291']),
    ('aa.tuandq', 'aa.tuandq', 'Đinh Quang Tuấn', 'IA', ['aa'], '2026-04-03 03:35', 'active', 'linked', 'vi', ['IAO202', 'NWC203', 'CRY302', 'IAA202']),
    ('aa.maidt', 'aa.maidt', 'Đỗ Thanh Mai', 'BA', ['aa'], '2026-04-03 03:40', 'active', 'linked', 'vi', ['MKT101', 'ECO111', 'DMK301']),
    ('aa.huyenbt', 'aa.huyenbt', 'Bùi Thu Huyền', 'BA', ['aa'], '2026-04-03 03:45', 'active', 'linked', 'vi', ['ACC101', 'FIN202', 'MGT103']),
    ('aa.trangnh', 'aa.trangnh', 'Nguyễn Hà Trang', 'GD', ['aa'], '2026-04-03 03:50', 'active', 'linked', 'en', ['DRP101', 'VCM202', 'TPG203']),
    ('wa.ngocbt', 'wa.ngocbt', 'Bùi Thị Ngọc', 'SE', ['wa'], '2026-04-04 02:00', 'active', 'linked', 'vi', []),
    ('wa.tamlh', 'wa.tamlh', 'Lý Hoàng Tâm', 'BA', ['wa'], '2026-04-04 02:05', 'active', 'linked', 'en', []),
    ('wa.linhvk', 'wa.linhvk', 'Võ Khánh Linh', 'AI', ['wa'], '2026-04-04 02:10', 'active', 'linked', 'vi', []),
    ('wa.dungpt', 'wa.dungpt', 'Phan Tiến Dũng', 'GD', ['wa'], '2026-04-04 02:15', 'active', 'linked', 'vi', []),
    ('wa.hoaitt', 'wa.hoaitt', 'Trương Thị Hoài', 'IA', ['wa'], '2026-04-04 02:20', 'deactivated', 'linked', 'vi', []),
    ('wo.yennh', 'wo.yennh', 'Ngô Hải Yến', None, ['wo'], '2026-04-05 02:00', 'active', 'linked', 'vi', []),
    ('wo.namtv', 'wo.namtv', 'Trần Văn Nam', None, ['wo'], '2026-04-05 02:05', 'active', 'linked', 'vi', []),
    ('wo.thuylm', 'wo.thuylm', 'Lê Minh Thủy', None, ['wo'], '2026-04-05 02:10', 'active', 'emailSent', 'en', []),
]
S = {}   # key -> id
ACC_SUBJECTS = defaultdict(list)
for key, user, name, fac, roles, created, st, link, lang, subjects in STAFF_DEF:
    aid = gid('acc', key)
    S[key] = aid
    make_account(aid, user, f"{user}@example.com", name, fac, roles, T(created), status=st, link=link, lang=lang)
    for i, code in enumerate(subjects):
        ACC_SUBJECTS[aid].append(code)
        add('ACCOUNT_SUBJECT', accountId=aid, subjectId=SUBJ[code][0], assignedAt=T(created) + D(2) + H(i))
ACC[S['aa.sonhm']]['deactivated_at'] = T('2026-08-31 09:00')
ACC[S['wa.hoaitt']]['deactivated_at'] = T('2026-09-30 08:00')

ADMINS = [S['admin02'], S['admin03']]
STEWARDS = [S['steward02'], S['steward03']]
PHS = [S['ph.quanpm'], S['aa.khoavd']]
ALL_PH = PHS + ['3978D886-B5A9-5108-89A0-5DD87AA154B4']


def has_role(aid, r):
    return r in ACC[aid]['roles']


def active_at(aid, t):
    a = ACC[aid]
    return a['created'] <= t and ('deactivated_at' not in a or t < a['deactivated_at'])


AAS = [a for a in S.values() if has_role(a, 'aa')]
WAS = [a for a in S.values() if has_role(a, 'wa')]
WOS = [a for a in S.values() if has_role(a, 'wo')]

# 2.3 Mock students
FAMILY = ['Nguyễn', 'Trần', 'Lê', 'Phạm', 'Hoàng', 'Huỳnh', 'Phan', 'Vũ', 'Võ', 'Đặng', 'Bùi', 'Đỗ', 'Hồ', 'Ngô', 'Dương', 'Lý',
          'Trương', 'Đinh', 'Lâm', 'Mai', 'Tạ', 'Cao', 'Châu', 'Quách', 'Hà', 'Kiều', 'Tôn', 'La']
MID_M = ['Văn', 'Minh', 'Quốc', 'Gia', 'Đức', 'Hoàng', 'Thanh', 'Tuấn', 'Hữu', 'Công', 'Nhật', 'Bảo', 'Trọng', 'Anh', 'Đình']
MID_F = ['Thị', 'Ngọc', 'Thu', 'Thanh', 'Minh', 'Khánh', 'Phương', 'Bảo', 'Mai', 'Gia', 'Hoài', 'Diệu', 'Kim', 'Thảo']
GIV_M = ['An', 'Bình', 'Cường', 'Dũng', 'Duy', 'Hải', 'Hiếu', 'Hùng', 'Huy', 'Khang', 'Khánh', 'Khoa', 'Kiên', 'Lâm', 'Long',
         'Nam', 'Nghĩa', 'Phát', 'Phong', 'Phúc', 'Quân', 'Quang', 'Sơn', 'Tài', 'Thắng', 'Thành', 'Thịnh', 'Tiến', 'Toàn',
         'Trí', 'Trung', 'Tuấn', 'Vinh', 'Vũ', 'Đạt', 'Đăng', 'Hào', 'Lộc', 'Nhân', 'Tín']
GIV_F = ['Anh', 'Chi', 'Dung', 'Giang', 'Hà', 'Hân', 'Hạnh', 'Hiền', 'Hoa', 'Hương', 'Lan', 'Linh', 'Ly', 'Mai', 'My', 'Nga',
         'Ngân', 'Ngọc', 'Nhi', 'Nhung', 'Oanh', 'Phương', 'Quỳnh', 'Thảo', 'Thư', 'Trang', 'Trâm', 'Uyên', 'Vân', 'Vy', 'Yến',
         'Như', 'Tâm', 'Thùy']

STUDENTS = []          # list of dict
STU = {}               # id -> dict
used_codes, used_users = {'SE180001'}, {'student'}
FAC_MIX = [('SE', 60), ('AI', 25), ('IA', 15), ('BA', 35), ('GD', 15)]
COHORTS = [('K17', 17, 0.17), ('K18', 18, 0.30), ('K19', 19, 0.28), ('K20', 20, 0.25)]
N_STUDENTS = 150
fac_list = [f for f, n in FAC_MIX for _ in range(n)]
rng.shuffle(fac_list)
for i in range(N_STUDENTS):
    fac = fac_list[i]
    male = rng.random() < 0.55
    fam, mid, giv = pick(FAMILY), pick(MID_M if male else MID_F), pick(GIV_M if male else GIV_F)
    name = f"{fam} {mid} {giv}"
    if i < 5:
        cohort, ck = 'K16', 16
    else:
        cohort, ck = wpick([((c, k), w) for c, k, w in COHORTS])
    while True:
        code = f"{fac}{ck}{rng.randint(0, 9999):04d}"
        if code not in used_codes:
            break
    used_codes.add(code)
    user = (unaccent(giv) + unaccent(fam)[0] + unaccent(mid)[0]).lower() + code.lower()
    used_users.add(user)
    sid = gid('stu', code)
    st = dict(id=sid, code=code, name=name, fac=fac, cohort=cohort, male=male, user=user)
    STUDENTS.append(st)
    STU[sid] = st

# academic status / account status
for st in STUDENTS[:5]:
    st['academic'] = 'graduated'
pool = [s for s in STUDENTS[5:] if s['cohort'] != 'K20']
rng.shuffle(pool)
for s in pool[:6]:
    s['academic'] = 'withdrawn'
for s in pool[6:14]:
    s['academic'] = 'onLeave'
for s in STUDENTS:
    s.setdefault('academic', 'enrolled')
    s['status'] = 'active'
for i, s in enumerate([s for s in STUDENTS if s['academic'] in ('withdrawn', 'graduated')]):
    if i % 3 != 2:
        s['status'] = 'deactivated'
enrolled_pool = [s for s in STUDENTS if s['academic'] == 'enrolled']
rng.shuffle(enrolled_pool)
enrolled_pool[0]['status'] = 'banned'
enrolled_pool[1]['status'] = 'banned'
enrolled_pool[2]['status'] = 'deactivated'
for s in STUDENTS:
    s['created'] = T('2026-08-28 03:00') + H(rng.random()) if s['cohort'] == 'K20' else T('2026-04-22 02:00') + H(rng.random() * 2)
    s['link'] = wpick([('linked', 62), ('emailSent', 22), ('notLinked', 16)])
    if s['academic'] in ('withdrawn', 'graduated') and s['link'] != 'linked':
        s['link'] = 'linked'
    google_only = s['link'] == 'linked' and rng.random() < 0.04
    s['google_only'] = google_only
    make_account(s['id'], s['user'], f"{s['user']}@example.com", s['name'], s['fac'], ['student'], minute(s['created']),
                 status=s['status'], link=s['link'], lang='vi' if rng.random() < 0.8 else 'en',
                 password_hash=None if google_only else MOCK_PASSWORD_HASH)
    add('STUDENTPROFILES', studentProfileId=gid('profile', s['id']), studentId=s['id'], studentCode=s['code'], academicStatus=s['academic'])
    # risk profile (drives the data values and the kind of cases)
    band = wpick([('low', 62), ('mid', 25), ('high', 13)])
    base = {'low': (0.0, 0.35), 'mid': (0.35, 0.65), 'high': (0.65, 1.0)}[band]
    s['ra'] = rng.uniform(*base)
    s['rf'] = clamp(rng.uniform(*base) * 0.7 + rng.random() * 0.3, 0, 1)
    s['rw'] = clamp(rng.uniform(*base) * 0.6 + rng.random() * 0.4, 0, 1)
# a few extra multi-role accounts to test role switching: one advisor is also a student of the AI programme? (kept simple: none)

ACTIVE_STUDENTS = [s for s in STUDENTS if s['academic'] == 'enrolled' and s['status'] == 'active']
# Status timeline: when withdrawn / on leave / graduated happened (used to decide which imports include them)
for s in STUDENTS:
    s['left_at'] = {'withdrawn': T('2026-09-02'), 'onLeave': T('2026-08-25'), 'graduated': T('2026-08-24')}.get(s['academic'])
    if s['status'] in ('deactivated', 'banned'):
        s['inactive_at'] = (s['left_at'] or T('2026-10-01')) + D(rng.randint(2, 15))


def enrolled_at(s, t):
    """Student was enrolled (and not yet left) at time t"""
    if s['cohort'] == 'K20' and t < T('2026-09-01'):
        return False
    if s['cohort'] == 'K16' and t >= T('2026-08-24'):
        return False
    return s['left_at'] is None or t < s['left_at']


def student_active_at(s, t):
    return s['created'] <= t and ('inactive_at' not in s or t < s['inactive_at'])

# =====================================================================================================================
# 3. Consent purposes, policies, support categories, external services, data sources
# =====================================================================================================================
P = {
    'academic': '62C800D3-1C1D-57A4-A911-4F345E58D26E',
    'wellbeing': '4972CD1E-F8EF-5C20-B644-74B82BE479FA',
    'financial': '67AE520A-3DED-5412-BF27-857BDA854CDC',
    'career': '6B45F610-99BE-50EB-BB03-ED7BC348EBFA',
    'retention': '601E7F3A-3C05-5434-BA56-B724DE50F1C7',
}
PURPOSE_NAME = {P['academic']: 'Academic advising', P['wellbeing']: 'Wellbeing support', P['financial']: 'Financial support',
                P['career']: 'Career guidance', P['retention']: 'Retention pilot 2025'}
PURPOSE_STATUS = {P['academic']: 'active', P['wellbeing']: 'active', P['financial']: 'active', P['career']: 'inactive', P['retention']: 'retired'}
for key, desc, creator in [
    ('academic', 'Lets your academic advisor see your study results so they can help with your subjects, study plan and academic warnings', 'steward02'),
    ('wellbeing', 'Lets your wellbeing advisor understand your situation outside class: housing, work, travel and campus life', 'steward02'),
    ('financial', 'Lets the welfare officer and your wellbeing advisor check your financial situation to arrange tuition support, loans, grants or meal support', 'steward03'),
    ('career', 'Internship and career advice (not launched yet)', 'steward03'),
    ('retention', 'Pilot study on student retention, finished in 2025', 'steward02'),
]:
    add('CONSENTPURPOSES', consentPurposeId=P[key], createdBy=S[creator], description=desc, purposeName=PURPOSE_NAME[P[key]], status=PURPOSE_STATUS[P[key]])

PURPOSE_ATTRS = {
    P['academic']: ['GPA', 'Attendance rate', 'Credits earned', 'Credits attempted', 'Failed subjects', 'Absent slots', 'Late submissions',
                    'Academic warning level', 'English level', 'LMS logins per week'],
    P['wellbeing']: ['Attendance rate', 'Accommodation status', 'Part-time work hours', 'Commute time', 'Has part-time job',
                     'First-generation student', 'Library visits', 'Transport mode'],
    P['financial']: ['Financial difficulty', 'Accommodation status', 'Tuition outstanding', 'Tuition overdue', 'Scholarship status',
                     'Family income bracket', 'Receives meal support', 'Has part-time job'],
    P['career']: ['English level', 'Credits earned', 'Has part-time job'],
    P['retention']: ['GPA', 'Attendance rate', 'Club membership count', 'Dormitory block'],
}
PURPOSE_ATTRS = {p: [A[n] for n in names] for p, names in PURPOSE_ATTRS.items()}
for p, attrs in PURPOSE_ATTRS.items():
    for a in attrs:
        add('PURPOSE_ATTRIBUTE', consentPurposeId=p, dataAttributeId=a)

POLICIES = [  # id, purpose, role, status
    ('B3689F75-4BEB-5778-9257-875C133360AF', 'academic', 'aa', 'active'),
    ('14088196-1D97-5B1B-86CD-5743A59864DE', 'wellbeing', 'wa', 'active'),
    ('C9C16ED5-E85B-5F2C-AACC-6A886EA283CD', 'financial', 'wa', 'active'),
    ('0D2FD37F-BA94-58DD-B9E8-5C37A929D223', 'financial', 'wo', 'active'),
    ('62F5C965-D262-5928-9FF1-9BA42D8FA7E2', 'wellbeing', 'wo', 'active'),
    ('237CBF56-AA3F-5E0B-8399-605B5B7B82E2', 'academic', 'ph', 'draft'),
    ('21C37377-598C-540F-8DA9-36C2E7594979', 'retention', 'aa', 'revoked'),
    ('EF52AFA8-86EF-507D-B647-A3E4FE5C237E', 'career', 'aa', 'draft'),
    (gid('policy', 'wellbeing-aa-revoked'), 'wellbeing', 'aa', 'revoked'),
]
for pid, pk, rk, st in POLICIES:
    add('ACCESSPOLICIES', accessPolicyId=pid, consentPurposeId=P[pk], createdBy=S['steward02'] if pk != 'financial' else S['steward03'], roleId=ROLE[rk][0], status=st)
# purposes each staff role may read (active policy + active purpose)
ROLE_PURPOSES = defaultdict(set)
for pid, pk, rk, st in POLICIES:
    if st == 'active' and PURPOSE_STATUS[P[pk]] == 'active':
        ROLE_PURPOSES[rk].add(P[pk])


def account_purposes(aid):
    out = set()
    for r in ACC[aid]['roles']:
        out |= ROLE_PURPOSES[r]
    return out


CAT = {}   # key -> dict
for key, cid, purpose, name, desc, typ, st in [
    ('course', 'AF347586-939E-5F8D-A18B-81D163777536', 'academic', 'Course content question', 'Questions about lectures, labs or assignments of a subject', 'academic', 'active'),
    ('plan', 'F6423B2F-DFDE-52BF-93EE-4A16B7544CE6', 'academic', 'Study plan', 'Planning which subjects to take and how to catch up', 'academic', 'active'),
    ('exam', 'FDD156EB-5B67-5C5B-8E4E-D8F7FE0CE031', 'academic', 'Exam preparation', 'Preparing for final exams or a resit', 'academic', 'active'),
    ('warning', '475E4312-560B-5126-8257-CDA07E894E90', 'academic', 'Academic warning', 'Help after receiving an academic warning', 'academic', 'active'),
    ('ojt', None, 'academic', 'Internship (OJT) preparation', 'CV, interview practice and choosing an on-the-job-training company', 'academic', 'active'),
    ('career', None, 'career', 'Career orientation', 'Choosing a career path after graduation (data sharing not available yet)', 'academic', 'active'),
    ('thesis', '88348E00-49B7-54B8-A140-3A6A62A69C2D', 'academic', 'Thesis topic (old)', 'Replaced by Study plan', 'academic', 'archived'),
    ('housing', '49F96775-F067-58E3-A311-5E2FB1EA20F7', 'wellbeing', 'Accommodation', 'Problems with housing or the dormitory', 'wellbeing', 'active'),
    ('stress', '7A063A85-F134-59F9-A48F-32806169DA7C', 'wellbeing', 'Stress and balance', 'Stress, workload or balancing study and work', 'wellbeing', 'active'),
    ('family', None, 'wellbeing', 'Family circumstances', 'A family situation that affects your studies', 'wellbeing', 'active'),
    ('adjust', None, 'wellbeing', 'Adjusting to university life', 'Settling in, making friends, living away from home for the first time', 'wellbeing', 'active'),
    ('money', '9285B8AC-C465-5254-8A87-CDFF33BC5A07', 'financial', 'Money', 'Tuition, loans, grants or daily living costs', 'wellbeing', 'active'),
    ('scholar', None, 'financial', 'Scholarship and grants', 'Applying for or keeping a scholarship or a grant', 'wellbeing', 'active'),
]:
    cid = cid or gid('cat', key)
    CAT[key] = dict(id=cid, purpose=P[purpose], name=name, type=typ, status=st, key=key)
    add('SUPPORTCATEGORIES', supportCategoryId=cid, consentPurposeId=P[purpose], createdBy=S['admin02'], categoryName=name,
        description=desc, supportType=typ, status=st)

EXT = {}
for key, sid, contact, name, typ, active in [
    ('bank', 'C7512875-FBB5-5C82-B4EF-B51DC408B12B', 'Hotline 1900 1234, studentloan@examplebank.example', 'Example Bank Student Loan Desk', 'Bank', True),
    ('housing', '4DEAAB49-4589-5252-8360-290D33B7CE8F', '028 3822 0000, housing@cityyouth.example', 'City Youth Housing Office', 'Housing', True),
    ('clinic', 'B4D459EC-436A-5BF4-81D2-DB347C06EBA6', 'Room A102, booking@clinic.example', 'Campus Counselling Partner Clinic', 'Counselling', True),
    ('district', 'EE0159C5-9F58-5ADA-B161-AB237731318D', '028 3910 1111', 'District Social Welfare Office', 'Government', True),
    ('meals', 'DB22589F-B286-5697-A2A8-2561529D61FD', 'meals@charity.example', 'Student Meal Charity Fund', 'Charity', True),
    ('oldfund', 'BC8F261F-46B3-519E-A4CF-1B3E6D2B8C64', 'No longer operating', 'Old Scholarship Foundation', 'Foundation', False),
    ('policybank', None, '1900 555 768, hssv@vbsp.example', 'Social Policy Bank - Student Credit Programme', 'Bank', True),
    ('legal', None, 'legalaid@youthunion.example, Thu 14:00-17:00', 'Youth Union Legal Aid Desk', 'Legal', True),
    ('jobs', None, 'parttime@jobcenter.example', 'Campus Part-time Job Centre', 'Employment', True),
]:
    sid = sid or gid('ext', key)
    EXT[key] = sid
    add('EXTERNALSERVICES', externalServiceId=sid, contactInfo=contact, serviceName=name, serviceType=typ, isActive=active)

FIELD_MAP = '{"studentIdentifier": "StudentCode", "value": "Value", "term": "Term", "validFrom": "RecordedOn"}'
DS = {}
for key, dsid, attr, cfg, name, typ, st in [
    ('attendance', '29930816-9A2C-53C1-B0B2-F46CB57852DA', 'Attendance rate', '{"folder": "imports/attendance"}', 'Attendance system export', 'file', 'active'),
    ('records', 'B61211EB-D3F4-5058-AAE3-4E3A5B98F09A', 'GPA', '{"server": "records.example", "table": "TermResults"}', 'Academic records database', 'database', 'active'),
    ('finance', '933EA929-E56E-5EF3-B7C7-6A10A060D060', 'Tuition outstanding', '{"url": "https://finance.example/api/tuition"}', 'Finance office API', 'api', 'paused'),
    ('lms', None, 'LMS logins per week', '{"url": "https://lms.example/api/activity", "auth": "service-account"}', 'LMS activity API', 'api', 'active'),
    ('library', None, 'Library visits', '{"folder": "imports/library-gates"}', 'Library gate log', 'file', 'disconnected'),
]:
    dsid = dsid or gid('ds', key)
    DS[key] = dsid
    add('DATASOURCES', dataSourceId=dsid, createdBy=S['admin02'], dataAttributeId=A[attr], connectionConfig=cfg, fieldMapping=FIELD_MAP,
        sourceName=name, sourceType=typ, status=st)

for key, freq, onfail, retry_int, retries, st, succ, fails in [
    ('attendance', 1440, 'warn', None, None, 'active', 0, 0), ('records', 1440, 'retry', 30, 3, 'active', 0, 0),
    ('finance', 10080, 'skip', None, None, 'paused', 0, 0), ('lms', 720, 'retry', 15, 5, 'active', 0, 0),
    ('library', 10080, 'warn', None, None, 'archived', 0, 0)]:
    sch_id = gid('sched', key)
    jobs = []
    if key in ('attendance', 'records', 'lms'):
        step = D(freq / 1440)
        t = T('2026-10-20 02:00') if key != 'lms' else T('2026-10-27 02:00')
        while t < NOW:
            fail = rng.random() < 0.12
            jobs.append((t, None if fail else t + H(0.05 + rng.random() * 0.1),
                         pick(['Connection timed out after 30 s', 'Source file not found: imports/attendance/2026-10-31.csv', 'HTTP 503 from the source API', 'Login to the source database failed']) if fail else None,
                         'failed' if fail else 'success'))
            t += step
        jobs.append((t, None, None, 'pending'))
        if key == 'records':
            jobs[-2] = (jobs[-2][0], None, None, 'running')
    elif key == 'finance':
        for t in [T('2026-09-14 02:00'), T('2026-09-21 02:00'), T('2026-09-28 02:00'), T('2026-10-05 02:00')]:
            jobs.append((t, t + H(0.2), None, 'success'))
        jobs.append((T('2026-10-12 02:00'), T('2026-10-12 02:01'), 'Finance API returned 401 Unauthorized; schedule paused by the admin', 'failed'))
    succ = sum(1 for j in jobs if j[3] == 'success') + (30 if key == 'attendance' else 10 if key == 'records' else 0)
    fails = sum(1 for j in jobs if j[3] == 'failed') + (2 if key == 'attendance' else 0)
    last_run = max([j[0] for j in jobs if j[3] in ('success', 'failed', 'running')], default=None)
    add('INGESTIONSCHEDULES', ingestionScheduleId=sch_id, dataSourceId=DS[key], scheduledStartTime=dt.time(2, 0) if key != 'lms' else dt.time(2, 0),
        ingestionFrequency=freq, lastRun=last_run, numberOfSuccesses=succ, numberOfFails=fails, onFailure=onfail,
        failureRetryInterval=retry_int, numberOfRetries=retries, status=st)
    for j, (t, done, err, jst) in enumerate(jobs):
        add('INGESTIONJOBS', ingestionJobId=gid('job', key, j), ingestionScheduleId=sch_id, expectedStartTime=t, completedAt=done, errorDetails=err, status=jst)

# =====================================================================================================================
# 4. Student data values (plaintext first; keys and encryption are applied at the end)
# =====================================================================================================================
for s in STUDENTS:
    s['noise'] = rng.gauss(0, 0.4)
    s['accom'] = wpick([('dormitory', 30), ('rented', 40), ('family', 25), ('other', 5)])
    if s['rf'] > 0.7 and s['accom'] == 'family':
        s['accom'] = 'rented'
    s['accom_fall'] = s['accom'] if rng.random() < 0.8 else wpick([('rented', 60), ('dormitory', 25), ('family', 15)])
    s['transport'] = {'dormitory': wpick([('walk', 60), ('bicycle', 25), ('motorbike', 15)]),
                      'rented': wpick([('motorbike', 65), ('bus', 25), ('bicycle', 10)]),
                      'family': wpick([('motorbike', 55), ('bus', 30), ('car', 15)]),
                      'other': wpick([('bus', 50), ('motorbike', 50)])}[s['accom_fall']]
    s['commute'] = {'walk': rng.randint(5, 15), 'bicycle': rng.randint(10, 25), 'motorbike': rng.randint(15, 60), 'bus': rng.randint(35, 95), 'car': rng.randint(25, 70)}[s['transport']]
    s['income'] = 'low' if s['rf'] > 0.6 else wpick([('middle', 65), ('low', 15), ('high', 20)])
    s['firstgen'] = rng.random() < (0.55 if s['income'] == 'low' else 0.2)
    s['pt_hours'] = rng.choice([12, 16, 20, 24, 28, 32]) if (s['rf'] > 0.5 and rng.random() < 0.8) or rng.random() < 0.15 else 0
    s['scholar'] = 'full' if s['ra'] < 0.1 and rng.random() < 0.5 else 'partial' if s['ra'] < 0.3 and rng.random() < 0.35 else 'none'
    s['english'] = wpick([('A2', 10 + 25 * s['ra']), ('B1', 40), ('B2', 30 - 15 * s['ra']), ('C1', 8)])
    s['meal'] = s['rf'] > 0.7 and rng.random() < 0.6
    s['tuition'] = round(rng.uniform(6, 28), 1) if s['rf'] > 0.62 else 0.0
    s['clubs'] = rng.randint(0, 4)


def fmt_num(v, decimals=0):
    return str(int(round(v))) if decimals == 0 else f"{v:.{decimals}f}"


GPA_CACHE = {}


def term_gpa(s, k):
    if (s['id'], k) not in GPA_CACHE:
        base = 8.7 - 4.3 * s['ra'] + s['noise']
        v = {'spring': base, 'summer': base + rng.gauss(0, 0.5)}.get(k)
        if k == 'fall1':
            v = base - 0.5 * s['rw'] + rng.gauss(0, 0.3)
        if k == 'fall2':
            v = term_gpa(s, 'fall1') + rng.gauss(0, 0.25) - (0.4 if s['ra'] > 0.6 else 0)
        GPA_CACHE[(s['id'], k)] = round(clamp(v, 2.8, 9.8), 1)
    return GPA_CACHE[(s['id'], k)]


ATT_CACHE = {}


def term_att(s, k):
    if (s['id'], k) not in ATT_CACHE:
        base = 97 - 33 * s['ra'] - 8 * s['rw'] + rng.gauss(0, 3)
        v = base if k in ('spring', 'summer', 'fall1') else term_att(s, 'fall1') - (6 if s['ra'] > 0.55 else 0) + rng.gauss(0, 2)
        ATT_CACHE[(s['id'], k)] = int(round(clamp(v, 42, 100)))
    return ATT_CACHE[(s['id'], k)]


def warning_level(gpa, att):
    lvl = 0 if gpa >= 5.5 else 1 if gpa >= 5.0 else 2 if gpa >= 4.5 else 3
    if att < 70:
        lvl = max(lvl, 1)
    return ['none', 'level1', 'level2', 'probation'][lvl]


CREDIT_BASE = {'K16': 138, 'K17': 108, 'K18': 78, 'K19': 44, 'K20': 0}


def plain_value(s, name, k):
    fail_spring = int(clamp(round((s['ra'] - 0.35) * 5 + rng.gauss(0, 0.6)), 0, 5))
    if name == 'GPA':
        return fmt_num(term_gpa(s, k), 1)
    if name == 'Attendance rate':
        return fmt_num(term_att(s, k))
    if name == 'Credits earned':
        c = CREDIT_BASE[s['cohort']] - int(s['ra'] * 12)
        return fmt_num(c + (6 if k in ('summer', 'fall1', 'fall2') and s['cohort'] != 'K20' else 0))
    if name == 'Credits attempted':
        return fmt_num(rng.choice([3, 6, 8]) if k == 'summer' else rng.choice([15, 18, 18, 20, 21, 22]) - (3 if s['pt_hours'] >= 24 else 0))
    if name == 'Failed subjects':
        return fmt_num(fail_spring if k == 'spring' else (1 if k == 'summer' and s['ra'] > 0.7 else 0))
    if name == 'Absent slots':
        slots = {'spring': 120, 'summer': 40, 'fall1': 30, 'fall2': 75}[k]
        return fmt_num((100 - term_att(s, k)) / 100 * slots)
    if name == 'Late submissions':
        return fmt_num(max(0, s['ra'] * 7 + rng.gauss(0, 1) + (1 if k == 'fall2' else 0)))
    if name == 'LMS logins per week':
        return fmt_num(clamp(11 - 8 * s['ra'] + rng.gauss(0, 1.5), 0.5, 20), 1)
    if name == 'Library visits':
        return fmt_num(max(0, rng.randint(0, 30) * (1 - s['ra'])))
    if name == 'Tuition outstanding':
        t = s['tuition']
        if k == 'fall2' and t > 0 and rng.random() < 0.4:
            t = round(t / 2, 1)
        return fmt_num(t, 1)
    if name == 'Part-time work hours':
        return fmt_num(s['pt_hours'] if k != 'spring' or rng.random() < 0.8 else 0)
    if name == 'Commute time':
        return fmt_num(s['commute'])
    if name == 'Accommodation status':
        return s['accom'] if k == 'spring' else s['accom_fall']
    if name == 'Scholarship status':
        return s['scholar']
    if name == 'Academic warning level':
        return warning_level(term_gpa(s, 'spring' if k in ('spring', 'summer') else k), term_att(s, 'spring' if k in ('spring', 'summer') else k))
    if name == 'English level':
        return s['english']
    if name == 'Family income bracket':
        return s['income']
    if name == 'Transport mode':
        return s['transport']
    if name == 'Financial difficulty':
        return 'true' if s['rf'] > 0.6 else 'false'
    if name == 'Tuition overdue':
        return 'true' if s['tuition'] > 0 and s['rf'] > 0.7 else 'false'
    if name == 'Has part-time job':
        return 'true' if s['pt_hours'] > 0 else 'false'
    if name == 'First-generation student':
        return 'true' if s['firstgen'] else 'false'
    if name == 'Receives meal support':
        return 'true' if s['meal'] else 'false'
    if name == 'Club membership count':
        return fmt_num(s['clubs'])
    if name == 'Dormitory block':
        return pick(['A1', 'A2', 'B1', 'B2', 'C3', 'D1']) if s['accom'] == 'dormitory' else 'none'
    raise KeyError(name)


PERF = {'GPA', 'Attendance rate', 'Credits earned', 'Credits attempted', 'Failed subjects', 'Absent slots', 'Late submissions',
        'LMS logins per week', 'Library visits', 'Tuition outstanding', 'Tuition overdue', 'Academic warning level', 'Club membership count'}
ACTIVE_ATTR_NAMES = [n for _, n, _, st, _ in ATTRS if st == 'active']
IMPORTS = [
    dict(key='spring', file='spring2026_results.xlsx', at=T('2026-04-30 03:00'), term='Spring 2026', attrs=[n for _, n, *_ in ATTRS],
         valid_perf=T('2026-04-20'), miss=0.02, takers=1.0),
    dict(key='summer', file='summer2026_results.xlsx', at=T('2026-08-28 03:00'), term='Summer 2026',
         attrs=['GPA', 'Credits earned', 'Credits attempted', 'Failed subjects'], valid_perf=T('2026-08-20'), miss=0.0, takers=0.35),
    dict(key='fall1', file='studentdata_fall2026_week3.xlsx', at=T('2026-09-28 02:30'), term='Fall 2026', attrs=ACTIVE_ATTR_NAMES,
         valid_perf=T('2026-09-26'), miss=0.03, takers=1.0),
    dict(key='fall2', file='studentdata_fall2026_midterm.xlsx', at=T('2026-10-26 02:30'), term='Fall 2026',
         attrs=['GPA', 'Attendance rate', 'Absent slots', 'Late submissions', 'LMS logins per week', 'Tuition outstanding', 'Tuition overdue'],
         valid_perf=T('2026-10-24'), miss=0.04, takers=1.0),
]
DATA_ROWS = []          # every STUDENTDATA row (dicts added to DB at the end)


def new_data_row(s, attr_id, term, plain, txn, valid, provenance, status='original', missing=False, source=None, row_id=None):
    row = dict(studentDataId=row_id or gid('sd', len(DATA_ROWS)), attributeKeyId=None, dataAttributeId=attr_id, dataSourceId=source,
               studentId=s['id'], termId=TERM_ID[term], dataValue=None, provenanceNote=provenance, erasedAt=None,
               transactionFrom=minute(txn), validFrom=valid, isMissing=missing, status=status,
               _plain=plain, _term=term)
    DATA_ROWS.append(row)
    return row


IMPORT_SUMMARY = []
for imp in IMPORTS:
    inserted = missing = 0
    takers = {s['id'] for s in STUDENTS if enrolled_at(s, imp['at']) and rng.random() < imp['takers']}
    for s in STUDENTS:
        if not enrolled_at(s, imp['at']):
            continue
        active_now = student_active_at(s, imp['at'])
        for name in imp['attrs']:
            omitted = s['id'] not in takers or rng.random() < imp['miss']
            if omitted:
                if active_now:   # the import records a missing row for every enrolled, active student without a value
                    new_data_row(s, A[name], imp['term'], None, imp['at'], TERM_START[imp['term']],
                                 f"Expected in {imp['file']}; no value was supplied. Recorded {imp['at']:%Y-%m-%d}.", missing=True)
                    missing += 1
                continue
            valid = imp['valid_perf'] if name in PERF else TERM_START[imp['term']]
            source = DS['attendance'] if imp['key'] == 'fall2' and name == 'Attendance rate' else None
            prov = (f"Synced from Attendance system export on {imp['at']:%Y-%m-%d}." if source
                    else f"Imported from {imp['file']} on {imp['at']:%Y-%m-%d}.")
            new_data_row(s, A[name], imp['term'], plain_value(s, name, imp['key']), imp['at'], valid, prov, source=source)
            inserted += 1
    IMPORT_SUMMARY.append((imp, inserted, missing))

# =====================================================================================================================
# 5. Texts written by people (Vietnamese mostly, some English like real users)
# =====================================================================================================================
REQ = {
    'course': ["Em không hiểu phần {subj_name} ({subj}) trên lớp, làm lab mãi không chạy được và sắp đến progress test.",
               "Em bị mất gốc phần đầu môn {subj}, xem lại slide vẫn không theo kịp. Em cần người hướng dẫn cách học lại.",
               "I don't understand the assignments in {subj} and my lab marks are dropping. I need help before the next progress test.",
               "Em nghỉ ốm 2 tuần nên không theo kịp môn {subj}, em muốn được hướng dẫn lại các nội dung đã học.",
               "Nhóm em làm assignment {subj} bị trễ tiến độ, em cần tư vấn cách chia việc để kịp deadline."],
    'plan': ["Em đang nợ môn {subj} và sợ bị trễ tiến độ, muốn được tư vấn nên đăng ký những môn nào kỳ sau.",
             "Em đi làm thêm nhiều nên muốn giảm số tín chỉ, nhờ thầy cô tư vấn lộ trình học phù hợp.",
             "I want to plan my remaining subjects so I can still graduate on time after failing {subj}.",
             "Em muốn chuyển sang chuyên ngành hẹp khác, cần tư vấn các môn phải học bù."],
    'exam': ["Còn 3 tuần nữa thi cuối kỳ môn {subj} mà điểm luyện tập của em rất thấp, em cần kế hoạch ôn thi.",
             "Em phải thi lại môn {subj} và không biết nên bắt đầu ôn từ đâu.",
             "My practice test scores in {subj} are below 4 and the final exam is soon. I need a revision plan."],
    'warning': ["Em vừa nhận email cảnh báo học vụ mức 1 vì điểm trung bình thấp, em muốn được tư vấn để tránh bị đình chỉ.",
                "Tỉ lệ đi học của em dưới 80% và em đã nhận cảnh báo, em cần hỗ trợ để không bị cấm thi.",
                "I received a level 1 academic warning and want to avoid probation next term."],
    'ojt': ["Kỳ sau em đi OJT nhưng chưa biết viết CV và chọn công ty, mong được tư vấn.",
            "Em đã phỏng vấn trượt 3 công ty OJT, em cần luyện phỏng vấn và sửa CV."],
    'career': ["Em sắp tốt nghiệp và phân vân giữa đi làm và học thạc sĩ, mong được định hướng.",
               "I am not sure which career path fits my major and would like to talk to an advisor."],
    'housing': ["Chủ nhà báo lấy lại phòng vào cuối tháng, em chưa tìm được chỗ ở mới.",
                "Phòng ký túc xá quá ồn, em không thể học bài buổi tối và muốn đổi phòng.",
                "My rented room is too far from campus; I spend almost 2 hours a day on the bus.",
                "Chủ trọ tăng giá phòng đột ngột, em không đủ tiền để tiếp tục thuê."],
    'stress': ["Em đi làm 25 tiếng mỗi tuần và cảm thấy kiệt sức, điểm đang tụt dần.",
               "Áp lực deadline nhiều môn cùng lúc khiến em mất ngủ, em cần người trao đổi và sắp xếp lại thời gian.",
               "I feel overwhelmed with the workload this term and cannot keep up with my part-time job."],
    'family': ["Gia đình em có người ốm nặng, em phải về quê thường xuyên nên bỏ lỡ nhiều buổi học.",
               "Bố mẹ em vừa mất việc, em lo không thể tiếp tục việc học."],
    'adjust': ["Em là sinh viên năm nhất ở xa nhà, chưa quen ai và thấy rất khó hòa nhập.",
               "I moved to the city for university and I am struggling to settle in and make friends."],
    'money': ["Gia đình em không đóng được đợt học phí thứ hai của kỳ này.",
              "Em bị mất việc làm thêm, không đủ tiền trả tiền nhà và ăn uống.",
              "Em muốn vay vốn sinh viên nhưng không hiểu hồ sơ cần những gì.",
              "My family can no longer pay my tuition this term and I may have to drop out."],
    'scholar': ["Em muốn nộp hồ sơ học bổng khuyến khích học tập nhưng không rõ điều kiện.",
                "Em có nguy cơ mất học bổng 50% vì GPA kỳ trước giảm, em cần tư vấn."],
}
OUTREACH_REQ = {
    'warning': ["Tỉ lệ đi học kỳ này giảm mạnh và GPA đang dưới 5.5. Tôi muốn gặp em để lập kế hoạch tránh cảnh báo học vụ.",
                "Attendance dropped below 75% and the GPA is under 5.5 this term. I would like to make a recovery plan together."],
    'plan': ["Em đang có 2 môn trượt từ kỳ trước, tôi muốn cùng em xem lại lộ trình học để không bị trễ tốt nghiệp."],
    'course': ["Điểm lab {subj} của em thấp hơn nhiều so với lớp, tôi muốn hỗ trợ em trước progress test."],
    'stress': ["Em làm thêm nhiều giờ và tỉ lệ đi học giảm, tôi muốn gặp em để hỏi thăm và hỗ trợ sắp xếp thời gian."],
    'housing': ["Thời gian di chuyển của em rất dài và em hay đi trễ, tôi muốn trao đổi về chỗ ở gần trường hơn."],
}
ASSIGN_REASON = {'spec': ["Chuyên môn {subj}", "Specialised in {subj}", "Dạy trực tiếp môn {subj}"],
                 'fac': ["Cùng khoa, đang ít case", "Same faculty, lowest caseload", "Phân công theo khoa"],
                 'wb': ["Cố vấn đời sống của khoa", "Wellbeing advisor of the faculty", "Vấn đề tài chính, chuyển cố vấn đời sống trước"]}
CONTACT_REASON = ["Đã gặp sinh viên qua Google Meet ngày {d}", "Đã gặp trực tiếp tại phòng tư vấn ngày {d}",
                  "Met the student at the advising office on {d}", "Đã gọi điện và hẹn gặp sinh viên ngày {d}"]
FOLLOW_REASON = ["Theo dõi kết quả sau buổi gặp đầu tiên", "Chờ sinh viên nộp lại bài lab", "Checking progress after two weeks",
                 "Hẹn gặp lại sau khi có điểm progress test", "Theo dõi sau khi bộ phận phúc lợi hỗ trợ xong"]
CLOSE_REASON = {'academic': ["Sinh viên đã nắm được nội dung và tự tin cho kỳ thi", "Đã lập xong kế hoạch học tập cho kỳ sau",
                             "Student passed the resit exam", "Sinh viên không phản hồi sau 3 lần liên hệ, đóng case",
                             "Điểm progress test đã cải thiện, sinh viên tự theo được"],
                'wellbeing': ["Sinh viên đã ổn định chỗ ở mới", "Vấn đề tài chính đã được giải quyết", "Student feels settled and has a support group",
                              "Sinh viên đã giảm giờ làm thêm và đi học đều lại", "Đã kết nối sinh viên với dịch vụ bên ngoài, không cần hỗ trợ thêm"]}
REOPEN_REASON = ["Sinh viên trượt bài kiểm tra tiếp theo và xin hỗ trợ thêm", "Student asked for help again after failing the final",
                 "Vấn đề cũ tái diễn, sinh viên liên hệ lại", "Sinh viên lại bị chủ trọ yêu cầu chuyển đi"]
NOTE = {'academic': ["Đã cùng sinh viên xem lại phần {subj} còn yếu. Sinh viên sẽ làm lại lab và gửi trước thứ Sáu.",
                     "Lập kế hoạch ôn tập 3 tuần, mỗi tuần 2 buổi tự học ở thư viện. Hẹn kiểm tra lại sau 1 tuần.",
                     "Went through the progress test mistakes together. The student will redo the exercises and email me.",
                     "Sinh viên thừa nhận hay thức khuya chơi game nên nghỉ học buổi sáng. Đã thống nhất mục tiêu đi học đủ 2 tuần tới.",
                     "Giới thiệu sinh viên tham gia nhóm học tập do lớp trưởng tổ chức vào tối thứ Ba.",
                     "Đã xem lại bảng điểm, đề xuất kỳ sau đăng ký học lại môn trượt và giảm 1 môn tự chọn."],
        'wellbeing': ["Sinh viên chia sẻ áp lực tài chính từ gia đình. Đã hướng dẫn các nguồn hỗ trợ của trường.",
                      "Hỏi thăm tình hình chỗ ở; sinh viên đang tìm phòng gần trường với 2 bạn cùng lớp.",
                      "The student is working nights; agreed to talk to the employer about fewer shifts during exams.",
                      "Sinh viên cảm thấy tốt hơn sau 2 tuần, đã tham gia CLB và có bạn cùng phòng mới.",
                      "Đã cung cấp thông tin phòng tư vấn tâm lý của trường, sinh viên đồng ý đặt lịch.",
                      "Kiểm tra lại giấy tờ thu nhập gia đình để chuẩn bị chuyển sang bộ phận phúc lợi."]}
REF_REASON = {'to_wa': ["Tỉ lệ đi học thấp do sinh viên phải đi làm thêm nhiều, cần cố vấn đời sống hỗ trợ.",
                        "The low attendance comes from a long commute after moving out of the dorm; this is more of a wellbeing issue."],
              'to_aa': ["Sinh viên cần người có chuyên môn {subj} hướng dẫn trực tiếp.", "Vấn đề chính là học tập, chuyển cố vấn học tập theo dõi."],
              'to_wo': ["Sinh viên cần hỗ trợ vay vốn hoặc miễn giảm học phí; thu nhập gia đình bị gián đoạn từ tháng 9.",
                        "Student needs a tuition loan or financial aid; family income stopped in September.",
                        "Cần hỗ trợ suất ăn và chi phí sinh hoạt trong tháng này.",
                        "Sinh viên không đủ tiền đặt cọc phòng mới, cần trợ cấp khó khăn đột xuất."]}
RETURN_REASON = {'wo': ["Không phải vấn đề tài chính, sinh viên cần hỗ trợ chỗ ở.",
                        "Thiếu giấy xác nhận thu nhập gia đình, đề nghị bổ sung trước khi chuyển lại."],
                 'adv': ["The student only needs help with the subject.", "Sinh viên đã được cố vấn khác hỗ trợ vấn đề này."]}
CR_REASON = {'advisor': ["Tôi nghỉ phép từ giữa tháng 11; {new} cũng phụ trách mảng này.", "I am on leave from mid-November; {new} also teaches the subject.",
                         "Tôi đang quá tải case, đề nghị chuyển cho {new}."],
             'student': ["Em muốn được cố vấn bởi thầy/cô dạy trực tiếp môn {subj}.", "Lịch của cố vấn hiện tại không trùng với lịch học của em.",
                         "I would prefer an advisor who teaches my subject."]}
CR_APPROVE_NOTE = ["Đồng ý trong thời gian cố vấn nghỉ phép", "Approved while the advisor is on leave", None, None]
CR_REJECT_NOTE = ["Cố vấn được đề xuất đã có quá nhiều case trong kỳ này.", "Đề nghị sinh viên trao đổi thêm với cố vấn hiện tại trước.",
                  "The proposed advisor already has too many open cases this term."]
REASSIGN_REASON = ["Cố vấn hiện tại quá tải", "Advisor is on sick leave this week", "Chuyển về cố vấn cùng khoa của sinh viên"]
INTERVENTION = {
    'financialAid': ("Đã gặp sinh viên, kiểm tra giấy tờ thu nhập gia đình và chuẩn bị hồ sơ miễn giảm học phí.", "Được giảm 50% học phí kỳ Fall 2026."),
    'loanArrangement': ("Liên hệ Ngân hàng Chính sách xã hội để làm hồ sơ vay vốn sinh viên.", "Hồ sơ vay vốn đã được ngân hàng chấp thuận, giải ngân trước 15/11."),
    'bankingPaperwork': ("Hướng dẫn sinh viên mở tài khoản ngân hàng và chuẩn bị giấy xác nhận sinh viên để nhận trợ cấp.", "Sinh viên đã có tài khoản và giấy xác nhận."),
    'other': ("Sắp xếp suất ăn hỗ trợ tại căng tin trong 2 tháng.", "Sinh viên đã nhận thẻ suất ăn hỗ trợ."),
}
GRANTS = [("Tuition reduction", "Giảm 50% học phí kỳ Fall 2026"), ("Emergency grant", "Trợ cấp khó khăn đột xuất 3.000.000 VND"),
          ("Meal support", "Thẻ suất ăn căng tin 2 tháng"), ("Book allowance", "Hỗ trợ giáo trình 800.000 VND"),
          ("Housing deposit", "Hỗ trợ tiền đặt cọc phòng 2.000.000 VND")]
EXT_OUTCOME = {'bank': "Ngân hàng đã duyệt khoản vay 20.000.000 VND cho phần học phí còn lại.",
               'policybank': "Hồ sơ tín dụng sinh viên được duyệt, giải ngân theo từng kỳ.",
               'housing': "Sinh viên đã chuyển vào nhà ở thanh niên từ 20/10.",
               'meals': "Quỹ đã nhận sinh viên vào chương trình suất ăn tháng 11.",
               'clinic': "Sinh viên đã tham gia 2 buổi tư vấn tại phòng khám đối tác.",
               'district': "Phòng LĐTB&XH quận xác nhận hộ cận nghèo, sinh viên đủ điều kiện miễn giảm.",
               'jobs': "Sinh viên nhận được việc làm thêm ca tối tại thư viện trường.",
               'legal': "Đã được tư vấn về hợp đồng thuê nhà và quyền lợi khi chủ trọ đòi phòng sớm."}
ESC_REASON = ["Sinh viên cần gia hạn đóng học phí, chỉ Trưởng chương trình có thể duyệt.",
              "The student needs a reduced study load approved this term.",
              "Cần quyết định cho sinh viên tạm hoãn đóng học phí đến khi khoản vay được giải ngân."]
ESC_RESPONSE = ["Đồng ý gia hạn học phí đến 15/12/2026.", "Approved a reduced study load (12 credits) for Fall 2026.",
                "Đồng ý tạm hoãn, phòng tài chính đã được thông báo."]
CONSENT_REQ_TEXT = {P['academic']: ["Để xem kết quả học tập kỳ trước trước khi lập kế hoạch học tập cùng em.",
                                    "To check your previous results before we plan your next term."],
                    P['wellbeing']: ["Để biết tình hình chỗ ở có ảnh hưởng đến việc đi học của em không.",
                                     "To check whether your attendance problem is linked to your part-time job."],
                    P['financial']: ["Để xác định em có đủ điều kiện nhận hỗ trợ học phí hay không.",
                                     "To see your tuition situation before referring you to the welfare officer."]}


SUBJ_NAMES = {row['subjectCode']: row['subjectName'] for row in DB['SUBJECTS']}

# =====================================================================================================================
# 6. Cases - every case is simulated step by step; each step writes the rows the API would write
# =====================================================================================================================
CASES = []
READS = []            # (t, accessorId, studentId, purposeFilter or None, emergency)
SHARES = []           # (t, studentId, purposeId, [attrIds], origin)
CONSENT_REQ_INTENTS = []
OPEN_LOAD = defaultdict(int)
stage_hist_n = [0]


def nm(aid):
    return ACC[aid]['name']


def em(aid):
    return ACC[aid]['email']


def hist(c, by, to, reason, t):
    add('CASESTAGEHISTORIES', caseStageHistoryId=gid('hist', c['id'], stage_hist_n[0]), caseId=c['id'], changedBy=by, reason=reason,
        changedAt=minute(t), fromStage=c['stage'], toStage=to)
    stage_hist_n[0] += 1
    c['stage'] = to


def timeline(c, t):
    c['timeline'].append((t, c['advisor'], c['status']))


def choose_advisor(c, t, exclude=(), typ=None):
    typ = typ or c['cat']['type']
    s = c['student']
    if typ == 'academic':
        pool = [a for a in AAS if active_at(a, t) and (c.get('summer') or active_at(a, NOW)) and a not in exclude]
        spec = [a for a in pool if c['subject'] and c['subject'] in ACC_SUBJECTS[a]]
        fac = [a for a in pool if ACC[a]['fac'] == s['fac']]
        cands, why = (spec, 'spec') if spec and rng.random() < 0.85 else ((fac, 'fac') if fac else (pool, 'fac'))
    else:
        pool = [a for a in WAS if active_at(a, t) and (c.get('summer') or active_at(a, NOW)) and a not in exclude]
        fac = [a for a in pool if ACC[a]['fac'] == s['fac']]
        cands, why = (fac, 'wb') if fac and rng.random() < 0.7 else (pool, 'wb')
    cands = sorted(cands, key=lambda a: (OPEN_LOAD[a] + rng.random() * 3))
    return cands[0], why


def pick_ph(t):
    return S['ph.quanpm'] if rng.random() < 0.78 else S['aa.khoavd']


def cat_purpose_attrs(c):
    p = c['cat']['purpose']
    return [a for a in PURPOSE_ATTRS[p] if ATTR_STATUS[a] == 'active'] if PURPOSE_STATUS[p] == 'active' else []


def subj_text(c):
    return f" for {c['subject']}" if c['subject'] else ''


def add_note(c, t, last_open=False):
    typ = c['cat']['type']
    text = pick(NOTE[typ]).format(subj=c['subject'] or 'môn học')
    draft = last_open and (NOW - t).days < 4 and rng.random() < 0.45
    edited = minute(t + H(rng.uniform(0.2, 6))) if rng.random() < 0.25 and t + H(6) < NOW else None
    nid = gid('note', c['id'], len(c['notes']))
    c['notes'].append(nid)
    add('CASENOTES', caseNoteId=nid, authorId=c['advisor'], caseId=c['id'], noteText=text, createdAt=minute(t), editedAt=edited, isFinalized=not draft)
    if not draft:
        log(c['advisor'], 'caseNoteFinalized', f"Finalized a note on the {c['cat']['name']} case of {c['student']['name']}", (edited or t) + H(0.1))


def step(c, name, t):
    s, cat, cname = c['student'], c['cat'], c['cat']['name']
    adv = c['advisor']
    url = f"/cases/{c['id']}"
    if name == 'open':
        hist(c, s['id'], 'pending', 'Support requested by the student', t)
        attrs = cat_purpose_attrs(c)
        shared = rng.sample(attrs, k=min(len(attrs), wpick([(0, 15), (1, 15), (2, 30), (3, 25), (4, 15)]))) if attrs else []
        if shared:
            SHARES.append((t, s['id'], cat['purpose'], shared, 'case'))
        log(s['id'], 'supportRequested', f"Requested {cat['type']} support ({cname}){subj_text(c)}, sharing {len(shared)} data attribute(s)", t)
        for ph in ALL_PH:
            notify(ph, 'New support request',
                   f"{s['name']} requested {cat['type']} support ({cname}{subj_text(c)}). The case is waiting in the matching queue.", url, t)
        timeline(c, t)
    elif name == 'outreach':
        READS.append((t - H(rng.uniform(0.2, 2)), c['advisor'], s['id'], None, False))
        c['stage'] = 'pending'
        hist(c, c['advisor'], 'assigned', 'Case opened by the advisor', t)
        OPEN_LOAD[c['advisor']] += 1
        log(c['advisor'], 'caseOpened', f"Opened a {cname} case{subj_text(c)} for {s['name']}", t)
        notify(s['id'], 'An advisor would like to meet you',
               f"{nm(c['advisor'])} ({em(c['advisor'])}) opened a {cname} case for you and would like to meet. Please contact them to arrange a meeting.", url, t)
        timeline(c, t)
    elif name == 'assign':
        ph = pick_ph(t)
        new, why = choose_advisor(c, t)
        c['advisor'] = new
        OPEN_LOAD[new] += 1
        hist(c, ph, 'assigned', pick(ASSIGN_REASON[why]).format(subj=c['subject']), t)
        log(ph, 'caseAssigned', f"Assigned the {cname} case of {s['name']} to {nm(new)}", t)
        notify(s['id'], 'Your support request has been matched',
               f"{nm(new)} ({em(new)}) will handle your {cname} request. Please contact them to arrange a meeting.", url, t)
        notify(new, 'New case assigned', f"You have been assigned the {cname} case of {s['name']} ({em(s['id'])}).", url, t)
        timeline(c, t)
    elif name == 'read':
        READS.append((t, adv, s['id'], {cat['purpose']}, False))
    elif name == 'wo_read':
        READS.append((t, c['wo'], s['id'], None, False))
    elif name in ('contact', 'followup'):
        to = 'contacted' if name == 'contact' else 'followUp'
        if c['stage'] == to:
            return
        reason = pick(CONTACT_REASON if name == 'contact' else FOLLOW_REASON).format(d=f"{t + H(7):%d/%m/%Y}")
        frm = c['stage']
        hist(c, adv, to, reason, t)
        log(adv, 'caseStageChanged', f"Moved the {cname} case of {s['name']} from {frm} to {to}", t)
    elif name == 'note':
        add_note(c, t, last_open=c.get('is_last_note', False))
    elif name == 'close':
        reason = pick(CLOSE_REASON[cat['type']])
        hist(c, adv, 'closed', reason, t)
        c['status'], c['closedAt'] = 'closed', minute(t)
        OPEN_LOAD[adv] -= 1
        log(adv, 'caseClosed', f"Closed the {cname} case of {s['name']}: {reason}", t)
        notify(s['id'], 'Your case has been closed', f"Your {cname} case was closed by {nm(adv)}: {reason}", url, t)
        timeline(c, t)
    elif name == 'reopen':
        reason = pick(REOPEN_REASON)
        c['status'], c['closedAt'] = 'reopened', None
        OPEN_LOAD[adv] += 1
        hist(c, adv, 'assigned', reason, t)
        log(adv, 'caseReopened', f"Reopened the {cname} case of {s['name']}: {reason}", t)
        notify(s['id'], 'Your case has been reopened', f"{nm(adv)} reopened your {cname} case: {reason}", url, t)
        timeline(c, t)
    elif name == 'priority':
        old = c['priority']
        new = pick([p for p in ['low', 'high', 'urgent'] if p != old])
        c['priority'] = new
        log(adv, 'casePriorityChanged', f"Changed the priority of the {cname} case of {s['name']} from {old} to {new}", t)
    elif name in ('ref_adv', 'ref_wo'):
        if name == 'ref_wo':
            recv = sorted([w for w in WOS if active_at(w, t)], key=lambda w: OPEN_LOAD[w] + rng.random() * 2)[0]
            reason = pick(REF_REASON['to_wo'])
        else:
            other = 'wellbeing' if cat['type'] == 'academic' and rng.random() < 0.6 else cat['type'] if rng.random() < 0.5 else ('academic' if cat['type'] == 'wellbeing' else 'academic')
            recv, _ = choose_advisor(c, t, exclude=(adv,), typ=other)
            reason = pick(REF_REASON['to_wa' if other == 'wellbeing' else 'to_aa']).format(subj=c['subject'] or 'môn học')
        prio = wpick([('medium', 45), ('high', 40), ('urgent', 15)])
        rid = gid('ref', c['id'], len(c['refs']))
        ref = dict(referralId=rid, caseId=c['id'], fromAccountId=adv, toAccountId=recv, referralReason=reason, acceptedAt=None,
                   createdAt=minute(t), priority=prio, status='pending')
        c['refs'].append(ref)
        c['pre_ref_stage'] = c['stage'] if c['stage'] != 'referred' else 'assigned'
        hist(c, adv, 'referred', f"Referred to {nm(recv)}: {reason}", t)
        log(adv, 'referralSent', f"Referred the {cname} case of {s['name']} to {nm(recv)} ({prio})", t)
        notify(recv, 'New referral', f"{nm(adv)} referred the {cname} case of {s['name']} to you ({prio} priority).", f"/referrals/{rid}", t)
        OPEN_LOAD[recv] += 1
    elif name == 'ref_accept':
        ref = c['refs'][-1]
        recv = ref['toAccountId']
        ref['status'], ref['acceptedAt'] = 'accepted', minute(t)
        log(recv, 'referralAnswered', f"Accepted the referral of the {cname} case of {s['name']} from {nm(adv)}", t)
        if has_role(recv, 'wo'):
            c['wo'] = recv
            notify(adv, 'Referral accepted', f"{nm(recv)} accepted your referral of the {cname} case of {s['name']}. The case comes back to you for follow-up once the welfare work is done.", f"/referrals/{ref['referralId']}", t)
        else:
            OPEN_LOAD[adv] -= 1
            c['advisor'] = recv
            hist(c, recv, 'assigned', f"Referral accepted by {nm(recv)}: {ref['referralReason']}", t)
            notify(adv, 'Referral accepted', f"{nm(recv)} accepted your referral of the {cname} case of {s['name']} and is now handling it.", f"/referrals/{ref['referralId']}", t)
            notify(s['id'], 'Your case has a new advisor', f"{nm(recv)} ({em(recv)}) is now handling your {cname} case. Please contact them to arrange a meeting.", url, t)
            timeline(c, t)
    elif name == 'ref_return':
        ref = c['refs'][-1]
        recv = ref['toAccountId']
        reason = pick(RETURN_REASON['wo' if has_role(recv, 'wo') else 'adv'])
        ref['status'] = 'returned'
        OPEN_LOAD[recv] -= 1
        hist(c, recv, c['pre_ref_stage'], f"Referral returned by {nm(recv)}: {reason}", t)
        log(recv, 'referralAnswered', f"Returned the referral of the {cname} case of {s['name']}: {reason}", t)
        notify(adv, 'Referral returned', f"{nm(recv)} returned your referral of the {cname} case of {s['name']}: {reason}", url, t)
    elif name == 'intervention':
        wo = c['wo']
        kinds = (['financialAid', 'loanArrangement'] if cat['key'] in ('money', 'scholar') else ['other', 'financialAid', 'bankingPaperwork'])
        n_int = 1 if rng.random() < 0.55 else 2
        if rng.random() < 0.35 and c['priority'] != 'urgent':
            log(wo, 'casePriorityChanged', f"Changed the priority of the {cname} case of {s['name']} from {c['priority']} to urgent", t - H(0.5))
            c['priority'] = 'urgent'
        for k in range(n_int):
            itype = kinds[k % len(kinds)] if rng.random() < 0.85 else 'bankingPaperwork'
            iid = gid('int', c['id'], k)
            when = t + D(k * rng.uniform(1, 4))
            future = when > NOW - H(2)
            status = 'scheduled' if future else wpick([('inProgress', 70), ('completed', 15), ('cancelled', 15)]) if not c.get('wo_will_close') else 'inProgress'
            if k == 0 and status == 'cancelled':
                status = 'inProgress'
            note, outcome = INTERVENTION[itype]
            row = add('INTERVENTIONS', interventionId=iid, caseId=c['id'], officerId=wo, notes=note,
                      outcome=None if status in ('scheduled', 'inProgress') and rng.random() < 0.7 else (outcome if status != 'cancelled' else 'Sinh viên không đến buổi hẹn, hủy và hẹn lại.'),
                      interventionDate=minute(when if not future else NOW + D(rng.randint(1, 6)) + H(rng.randint(0, 5))), interventionType=itype,
                      isClosed=status in ('completed', 'cancelled'), status=status)
            c['interventions'].append(row)
            log(wo, 'interventionLogged', f"Logged a {itype} intervention for the {cname} case of {s['name']}", min(when, NOW - H(1)) if not future else t)
            if itype in ('financialAid', 'loanArrangement', 'other') and status in ('inProgress', 'completed') and rng.random() < 0.6:
                gtype, gdesc = pick(GRANTS)
                issued = minute(min(when + D(rng.uniform(0.5, 3)), NOW - H(3)))
                revoked = rng.random() < 0.12
                add('SUPPORTGRANTS', supportGrantId=gid('grant', iid), interventionId=iid, grantDescription=gdesc, grantType=gtype, issuedAt=issued,
                    status='revoked' if revoked else 'issued')
                log(wo, 'supportGrantRecorded', f"Recorded a {gtype} grant for {s['name']}", issued)
                if revoked:
                    log(wo, 'supportGrantRevoked', f"Revoked the {gtype} grant of {s['name']}: hồ sơ trùng với học bổng khác", issued + D(2) if issued + D(2) < NOW else NOW - H(1))
        if rng.random() < 0.65:
            key = pick(['bank', 'policybank', 'housing', 'meals', 'district', 'jobs', 'legal', 'clinic'])
            referred = minute(t + H(rng.uniform(2, 30)))
            if referred < NOW - H(1):
                due = minute(referred + D(rng.choice([4, 6, 8, 12, 18])))
                if due < NOW:
                    status = wpick([('sent', 30), ('followedUp', 35), ('closed', 35)])
                else:
                    status = 'sent'
                overdue = status == 'sent' and due < NOW
                add('EXTERNALREFERRALS', externalReferralId=gid('extref', c['id']), caseId=c['id'], externalServiceId=EXT[key], referredBy=wo,
                    followUpDueAt=due, followUpOutcome=EXT_OUTCOME[key] if status != 'sent' else None, referredAt=referred, isOverdue=overdue, status=status)
                log(wo, 'externalReferralCreated', f"Referred {s['name']} to an external service ({key})", referred)
                if status != 'sent':
                    log(wo, 'externalReferralUpdated', f"Recorded the follow-up outcome of the external referral of {s['name']}", min(due, NOW - H(1)))
    elif name == 'escalate':
        wo = c['wo']
        ph = S['ph.quanpm'] if rng.random() < 0.75 else S['aa.khoavd']
        eid = gid('esc', c['id'])
        responded = (NOW - t).days >= 4 or rng.random() < 0.3
        i = rng.randrange(len(ESC_REASON))
        add('ESCALATIONS', escalationId=eid, caseId=c['id'], escalatedBy=wo, escalatedTo=ph, reason=ESC_REASON[i],
            response=ESC_RESPONSE[i] if responded else None, escalatedAt=minute(t), status='responded' if responded else 'pending')
        log(wo, 'escalationRaised', f"Escalated case {c['id']} to {nm(ph)}", t)
        notify(ph, 'Welfare escalation', 'A Welfare Officer escalated a case to you.', f"/escalations/{eid}", t)
        if responded:
            rt = min(t + H(rng.uniform(4, 60)), NOW - H(1))
            log(ph, 'escalationResponded', f"Responded to the escalation of case {c['id']}", rt)
            notify(wo, 'Escalation answered', 'The Programme Head responded to your escalation.', f"/escalations/{eid}", rt)
    elif name == 'wo_close':
        wo = c['wo']
        for row in c['interventions']:
            if row['status'] in ('scheduled', 'inProgress'):
                row['status'], row['isClosed'] = 'completed', True
                row['outcome'] = INTERVENTION[row['interventionType']][1] + ' Bàn giao lại cho cố vấn.'
                if row['interventionDate'] > t:
                    row['interventionDate'] = minute(t - H(rng.uniform(2, 20)))
        log(wo, 'interventionUpdated', f"Closed the intervention of case {c['id']}", t)
        notify(adv, 'Welfare intervention completed', 'The Welfare Officer completed the intervention for your case; you can continue the follow-up.', url, t)
        OPEN_LOAD[wo] -= 1
    elif name == 'cr_req':
        by_student = rng.random() < 0.5
        req = s['id'] if by_student else adv
        typ = cat['type']
        pool = [a for a in (AAS if typ == 'academic' else WAS) if active_at(a, NOW) and a != adv]
        new = sorted(pool, key=lambda a: (0 if c['subject'] in ACC_SUBJECTS[a] else 1, ACC[a]['fac'] != s['fac'], OPEN_LOAD[a] + rng.random() * 2))[0]
        reason = pick(CR_REASON['student' if by_student else 'advisor']).format(new=nm(new), subj=c['subject'] or 'môn học')
        crid = gid('cr', c['id'])
        c['cr'] = add('CASECHANGEREQUESTS', caseChangeRequestId=crid, caseId=c['id'], programmeHeadId=None, proposedAdvisorId=new, requestedBy=req,
                      decisionNotes=None, reason=reason, decidedAt=None, requestedAt=minute(t), status='pending')
        log(req, 'caseChangeRequested', f"Asked to move the {cname} case of {s['name']} to {nm(new)}", t)
        for ph in ALL_PH:
            notify(ph, 'Case advisor change requested', f"{nm(req)} asked to move the {cname} case of {s['name']} from {nm(adv)} to {nm(new)}.", f"/case-change-requests/{crid}", t)
    elif name in ('cr_approve', 'cr_reject'):
        cr = c['cr']
        ph = pick_ph(t)
        new = cr['proposedAdvisorId']
        cr['programmeHeadId'], cr['decidedAt'] = ph, minute(t)
        crurl = f"/case-change-requests/{cr['caseChangeRequestId']}"
        if name == 'cr_approve':
            cr['status'], cr['decisionNotes'] = 'approved', pick(CR_APPROVE_NOTE)
            hist(c, ph, c['stage'], f"Advisor changed from {nm(adv)} to {nm(new)}: case change request approved ({cr['reason']})", t)
            OPEN_LOAD[adv] -= 1
            OPEN_LOAD[new] += 1
            c['advisor'] = new
            log(ph, 'caseChangeDecided', f"Approved moving the {cname} case of {s['name']} from {nm(adv)} to {nm(new)}", t)
            notify(s['id'], 'Your case has a new advisor', f"{nm(new)} ({em(new)}) now handles your {cname} case. Please contact them to arrange a meeting.", url, t)
            notify(new, 'New case assigned', f"You now handle the {cname} case of {s['name']} ({em(s['id'])}), moved from {nm(adv)}.", url, t)
            notify(adv, 'Case change approved', f"The {cname} case of {s['name']} moved to {nm(new)}.", crurl, t)
            timeline(c, t)
        else:
            cr['status'], cr['decisionNotes'] = 'rejected', pick(CR_REJECT_NOTE)
            log(ph, 'caseChangeDecided', f"Rejected moving the {cname} case of {s['name']} to {nm(new)}: {cr['decisionNotes']}", t)
            for who in sorted({cr['requestedBy'], adv, s['id']}):
                notify(who, 'Case change rejected', f"The request to move the {cname} case of {s['name']} to {nm(new)} was rejected: {cr['decisionNotes']}", crurl, t)
    elif name == 'reassign':
        ph = pick_ph(t)
        new, _ = choose_advisor(c, t, exclude=(adv,))
        reason = pick(REASSIGN_REASON)
        hist(c, ph, c['stage'], f"Advisor changed from {nm(adv)} to {nm(new)}: {reason}", t)
        OPEN_LOAD[adv] -= 1
        OPEN_LOAD[new] += 1
        c['advisor'] = new
        log(ph, 'caseReassigned', f"Moved the {cname} case of {s['name']} from {nm(adv)} to {nm(new)}: {reason}", t)
        notify(new, 'New case assigned', f"You now handle the {cname} case of {s['name']} ({em(s['id'])}), moved from {nm(adv)}.", url, t)
        notify(adv, 'Case moved to another advisor', f"The {cname} case of {s['name']} moved to {nm(new)}.", url, t)
        notify(s['id'], 'Your case has a new advisor', f"{nm(new)} ({em(new)}) now handles your {cname} case. Please contact them to arrange a meeting.", url, t)
        timeline(c, t)
    elif name == 'consent_req':
        if cat['purpose'] in account_purposes(adv):
            CONSENT_REQ_INTENTS.append((t, adv, s['id'], cat['purpose'], c['id']))
    else:
        raise KeyError(name)


GAP = {'open': (0, 0), 'outreach': (0, 0), 'assign': (3, 60), 'read': (0.3, 18), 'contact': (12, 110), 'note': (0.5, 30),
       'followup': (48, 220), 'close': (24, 160), 'reopen': (40, 220), 'priority': (1, 48), 'ref_adv': (12, 90), 'ref_wo': (12, 90),
       'ref_accept': (2, 40), 'ref_return': (3, 40), 'wo_read': (0.5, 10), 'intervention': (10, 72), 'escalate': (24, 120),
       'wo_close': (72, 260), 'cr_req': (20, 120), 'cr_approve': (3, 40), 'cr_reject': (3, 40), 'reassign': (20, 100), 'consent_req': (1, 48)}
SCEN = {
    'pending': ['open'],
    'assigned': ['open', 'assign', 'read'],
    'contacted': ['open', 'assign', 'read', 'contact', 'note'],
    'followup': ['open', 'assign', 'read', 'contact', 'note', 'followup', 'note'],
    'closed': ['open', 'assign', 'read', 'contact', 'note', 'followup', 'note', 'close'],
    'closed_fast': ['open', 'assign', 'read', 'contact', 'note', 'close'],
    'reopened': ['open', 'assign', 'read', 'contact', 'note', 'close', 'reopen'],
    'reopened_contacted': ['open', 'assign', 'read', 'contact', 'note', 'close', 'reopen', 'contact', 'note'],
    'ref_adv_pending': ['open', 'assign', 'read', 'contact', 'note', 'ref_adv'],
    'ref_adv_accepted': ['open', 'assign', 'read', 'contact', 'note', 'ref_adv', 'ref_accept', 'read', 'contact', 'note'],
    'ref_adv_returned': ['open', 'assign', 'read', 'contact', 'ref_adv', 'ref_return', 'note', 'followup'],
    'cr_pending': ['open', 'assign', 'read', 'contact', 'note', 'cr_req'],
    'cr_approved': ['open', 'assign', 'read', 'cr_req', 'cr_approve', 'read', 'contact', 'note'],
    'cr_rejected': ['open', 'assign', 'read', 'contact', 'cr_req', 'cr_reject', 'note', 'followup'],
    'reassigned': ['open', 'assign', 'read', 'reassign', 'read', 'contact', 'note'],
    'wo_pending': ['open', 'assign', 'read', 'contact', 'note', 'ref_wo'],
    'wo_accepted': ['open', 'assign', 'read', 'contact', 'note', 'ref_wo', 'ref_accept', 'wo_read', 'intervention'],
    'wo_escalated': ['open', 'assign', 'read', 'contact', 'note', 'ref_wo', 'ref_accept', 'wo_read', 'intervention', 'escalate'],
    'wo_done': ['open', 'assign', 'read', 'contact', 'note', 'ref_wo', 'ref_accept', 'wo_read', 'intervention', 'wo_close', 'followup', 'note'],
    'wo_done_closed': ['open', 'assign', 'read', 'contact', 'note', 'ref_wo', 'ref_accept', 'wo_read', 'intervention', 'wo_close', 'followup', 'note', 'close'],
    'wo_returned': ['open', 'assign', 'read', 'contact', 'ref_wo', 'ref_return', 'note', 'followup'],
}
SC_ACADEMIC = [('pending', 9), ('assigned', 10), ('contacted', 13), ('followup', 10), ('closed', 22), ('closed_fast', 6), ('reopened', 3),
               ('reopened_contacted', 2), ('ref_adv_pending', 4), ('ref_adv_accepted', 5), ('ref_adv_returned', 3), ('cr_pending', 3),
               ('cr_approved', 3), ('cr_rejected', 2), ('reassigned', 2)]
SC_WELL = [('pending', 9), ('assigned', 10), ('contacted', 14), ('followup', 12), ('closed', 22), ('closed_fast', 6), ('reopened', 3),
           ('ref_adv_pending', 3), ('ref_adv_accepted', 3), ('ref_adv_returned', 2), ('cr_pending', 2), ('cr_approved', 2), ('cr_rejected', 2)]
SC_WELL_WO = SC_WELL + [('wo_pending', 4), ('wo_accepted', 4), ('wo_done_closed', 3)]
SC_MONEY = [('pending', 8), ('assigned', 6), ('contacted', 8), ('wo_pending', 12), ('wo_accepted', 14), ('wo_escalated', 8), ('wo_done', 10),
            ('wo_done_closed', 12), ('wo_returned', 6), ('closed', 10), ('reopened', 3)]


def plan_times(steps, opened, end_limit):
    raw = [0.0] + [rng.uniform(*GAP[st]) for st in steps[1:]]
    total = sum(raw)
    avail = (end_limit - opened).total_seconds() / 3600
    scale = min(1.0, avail / total) if total > 0 else 1.0
    times, t = [], opened
    for i, g in enumerate(raw):
        t = t + H(g * scale)
        tt = office(t) if i > 0 else minute(t)
        if times and tt <= times[-1]:
            tt = times[-1] + dt.timedelta(minutes=rng.randint(3, 40))
        if tt > end_limit:
            tt = times[-1] + dt.timedelta(minutes=2) if times else tt
        times.append(minute(tt))
        t = max(t, tt)
    return times


def new_case(s, cat_key, opened, scenario, outreach_by=None, summer=False, end_limit=None):
    cat = CAT[cat_key]
    subj = None
    if cat['type'] == 'academic' and cat_key not in ('ojt', 'career'):
        subjects = ACTIVE_SUBJ_BY_FAC[s['fac']]
        if cat_key in ('course', 'exam') or rng.random() < 0.6:
            subj = pick(subjects)
    cid = gid('case', len(CASES))
    c = dict(id=cid, student=s, cat=cat, subject=subj, advisor=outreach_by, stage='pending', status='open',
             priority='medium', closedAt=None, openedAt=minute(opened), refs=[], notes=[], interventions=[], timeline=[],
             openedBy=outreach_by or s['id'], wo=None, scenario=scenario, summer=summer)
    if outreach_by:
        c['reason'] = pick(OUTREACH_REQ.get(cat_key, OUTREACH_REQ['warning'])).format(subj=subj or 'môn học')
        c['priority'] = wpick([('high', 50), ('medium', 35), ('urgent', 15)])
    else:
        c['reason'] = pick(REQ[cat_key]).format(subj=subj or 'môn học', subj_name=SUBJ_NAMES.get(subj, 'bài học'))
    steps = list(SCEN[scenario])
    if outreach_by:
        steps = ['outreach'] + [x for x in steps if x not in ('open', 'assign')]
    if 'read' in steps and rng.random() < 0.25:
        steps.insert(steps.index('read') + 1, 'priority')
    if 'read' in steps and rng.random() < 0.22 and not summer:
        steps.insert(steps.index('read') + 1, 'consent_req')
    if 'intervention' in steps and 'escalate' not in steps and rng.random() < 0.3:
        steps.insert(steps.index('intervention') + 1, 'escalate')
    if 'wo_close' in steps:
        c['wo_will_close'] = True
    times = plan_times(steps, c['openedAt'], end_limit or NOW - H(0.5))
    last_note_idx = max([i for i, x in enumerate(steps) if x == 'note'], default=-1)
    for i, (st, t) in enumerate(zip(steps, times)):
        if st == 'reopen' and not active_at(c['advisor'], t):
            break      # the advisor left in the meantime: the case stays closed
        c['is_last_note'] = (i == last_note_idx and c['status'] != 'closed' and 'close' not in steps[i:])
        step(c, st, t)
    CASES.append(c)
    return c


def gap_total(scenario):
    return sum((a + b) / 2 for a, b in (GAP[x] for x in SCEN[scenario][1:]))


def choose_category(s, summer=False):
    w = [('course', 6 + 14 * s['ra']), ('plan', 4 + 6 * s['ra']), ('exam', 4 + 8 * s['ra']), ('warning', 1 + 14 * max(0, s['ra'] - 0.4)),
         ('ojt', 5 if s['cohort'] in ('K17', 'K16') else 0.3), ('career', 1.5 if s['cohort'] in ('K17', 'K16') else 0.2),
         ('housing', 2 + 8 * s['rw']), ('stress', 2 + 9 * s['rw']), ('family', 0.5 + 3 * s['rw']),
         ('adjust', 6 if s['cohort'] == 'K20' else 0.5), ('money', 0.5 + 14 * max(0, s['rf'] - 0.45)), ('scholar', 1 + 3 * (1 - s['ra']))]
    return wpick(w)


def scenario_for(cat_key):
    if cat_key in ('money', 'scholar'):
        return wpick(SC_MONEY)
    if cat_key in ('housing', 'family'):
        return wpick(SC_WELL_WO)
    if CAT[cat_key]['type'] == 'wellbeing':
        return wpick(SC_WELL)
    return wpick(SC_ACADEMIC)


# 6.1 Summer 2026 cases (history for the reports; all closed, two reopened in September)
summer_students = [s for s in STUDENTS if s['cohort'] != 'K20']
for i in range(40):
    s = pick(summer_students)
    cat_key = choose_category(s, summer=True)
    if cat_key in ('adjust', 'career'):
        cat_key = 'plan'
    opened = rand_between(T('2026-05-06'), T('2026-08-05'))
    if cat_key in ('money', 'scholar'):
        scenario = wpick([('wo_done_closed', 60), ('closed', 40)])
    else:
        scenario = wpick([('closed', 60), ('closed_fast', 40)])
    if i < 2 and enrolled_at(s, NOW) and s['status'] == 'active':
        scenario = 'reopened'
        new_case(s, cat_key, opened, scenario, summer=True)
    else:
        new_case(s, cat_key, opened, scenario, summer=True, end_limit=T('2026-08-30'))

# 6.2 Fall 2026 student requests
fall_students = [s for s in STUDENTS if s['academic'] == 'enrolled' and s['status'] == 'active']
weights = [0.25 + s['ra'] + 0.6 * s['rf'] + 0.5 * s['rw'] for s in fall_students]
open_cats = defaultdict(set)
N_FALL = 190
overdue_pending = 0
for i in range(N_FALL):
    s = rng.choices(fall_students, weights=weights)[0]
    for _ in range(5):
        cat_key = choose_category(s)
        if rng.random() < 0.18:
            cat_key = 'money' if s['rf'] > 0.35 or rng.random() < 0.5 else 'scholar'
        if cat_key not in open_cats[s['id']]:
            break
    scenario = scenario_for(cat_key)
    if cat_key == 'career' and scenario.startswith(('wo_', 'ref_wo')):
        scenario = 'contacted'
    if scenario == 'pending':
        if overdue_pending < 5:
            opened = NOW - H(rng.uniform(60, 220))
            overdue_pending += 1
        else:
            opened = NOW - H(rng.uniform(1, 45))
    else:
        tot = gap_total(scenario)
        age = rng.uniform(tot * 0.8, tot * 2.4)
        opened = max(NOW - H(age), T('2026-09-03 02:00') + H(rng.uniform(0, 72)))
    c = new_case(s, cat_key, office(opened) if opened < NOW - H(2) else opened, scenario)
    if c['status'] != 'closed':
        open_cats[s['id']].add(cat_key)

# 6.3 Advisor outreach (EX3) - advisors open cases for at-risk students
risky = sorted(fall_students, key=lambda s: -(s['ra'] + 0.5 * s['rw']))[:45]
OUTREACH_STUDENTS = []
for i in range(28):
    s = risky[i % len(risky)] if i < 20 else pick(risky)
    cat_key = wpick([('warning', 45), ('plan', 20), ('course', 15), ('stress', 12), ('housing', 8)])
    typ = CAT[cat_key]['type']
    c_tmp = dict(student=s, subject=None, cat=CAT[cat_key])
    adv, _ = choose_advisor(c_tmp, NOW - D(20), typ=typ)
    scenario = wpick([('assigned', 25), ('contacted', 30), ('followup', 20), ('closed', 20), ('ref_adv_pending', 5)])
    tot = gap_total(scenario)
    opened = max(NOW - H(rng.uniform(tot * 0.8, tot * 2.2)), T('2026-09-29 02:00') + H(rng.uniform(0, 48)))
    OUTREACH_STUDENTS.append((s, adv, opened))
    new_case(s, cat_key, office(opened), scenario, outreach_by=adv)

for c in CASES:
    add('CASES', caseId=c['id'], advisorId=c['advisor'], openedBy=c['openedBy'], studentId=c['student']['id'],
        subjectId=SUBJ[c['subject']][0] if c['subject'] else None, supportCategoryId=c['cat']['id'], requestReason=c['reason'],
        closedAt=c['closedAt'], openedAt=c['openedAt'], currentStage=c['stage'], priority=c['priority'], status=c['status'])
    for ref in c['refs']:
        add('REFERRALS', **ref)

# =====================================================================================================================
# 7. Consents, consent requests, withdrawals, erasure
# =====================================================================================================================
CONSENTS = []
BY_KEY = defaultdict(list)          # (studentId, purposeId, attrId) -> consents
BY_SA = defaultdict(list)           # (studentId, attrId) -> consents (any purpose)
CONSENT_HIST = []                   # (changedBy, consent, changedAt, new, previous)


def granted_at(c, t):
    return c['grantedAt'] <= t and (c['withdrawnAt'] is None or c['withdrawnAt'] > t)


def usable_at(c, t):
    return granted_at(c, t) and (c['expiresAt'] is None or c['expiresAt'] > t)


def create_consent(sid, p, a, t, expires, origin):
    n = len(BY_KEY[(sid, p, a)])
    c = dict(consentId=gid('consent', sid, p, a, n), consentPurposeId=p, dataAttributeId=a, studentId=sid, expiresAt=expires,
             grantedAt=minute(t), withdrawnAt=None, status='granted', _origin=origin)
    CONSENTS.append(c)
    BY_KEY[(sid, p, a)].append(c)
    BY_SA[(sid, a)].append(c)
    CONSENT_HIST.append((sid, c, minute(t), 'granted', None))
    return c


# 7.1 Consent page grants (students sharing without a case), including expiring and already-expired consents
for s, adv, opened in OUTREACH_STUDENTS:   # outreach is based on data the student shared earlier
    p = P['academic'] if has_role(adv, 'aa') else P['wellbeing']
    names = ['GPA', 'Attendance rate', 'Absent slots', 'Academic warning level'] if p == P['academic'] else ['Attendance rate', 'Part-time work hours', 'Commute time', 'Accommodation status']
    SHARES.append((opened - D(rng.uniform(3, 18)), s['id'], p, [A[n] for n in names], 'page'))
page_students = rng.sample(fall_students, 75)
for s in page_students:
    p = wpick([(P['academic'], 60), (P['wellbeing'], 25), (P['financial'], 15)])
    attrs = [a for a in PURPOSE_ATTRS[p] if ATTR_STATUS[a] == 'active']
    shared = rng.sample(attrs, k=rng.randint(1, len(attrs)))
    t = rand_between(T('2026-09-08 01:00'), NOW - D(1))
    SHARES.append((office(t), s['id'], p, shared, 'page'))
EXPIRY_OF = {}
for t, sid, p, attrs, origin in sorted(SHARES, key=lambda x: x[0]):
    if origin == 'page':
        r = rng.random()
        if r < 0.58:
            expires = None
        elif r < 0.80:
            expires = T('2026-12-27')
        elif r < 0.93 and t < T('2026-10-20'):
            expires = minute(rand_between(max(t + D(7), T('2026-10-10')), T('2026-11-03')))      # already expired
        else:
            expires = T('2027-04-25')
    else:
        expires = None
    created = 0
    for a in attrs:
        if any(granted_at(c, t) for c in BY_KEY[(sid, p, a)]) or BY_KEY[(sid, p, a)]:
            continue     # an attribute already granted is kept
        create_consent(sid, p, a, t, expires, origin)
        created += 1
    if created:
        log(sid, 'consentGranted', f"Granted {created} attribute(s) for {PURPOSE_NAME[p]}" + (f" until {expires:%Y-%m-%d}" if expires else ''), t)
        if expires and rng.random() < 0.15 and t + D(5) < NOW:
            log(sid, 'consentExpiryChanged', f"Changed the expiry date of {PURPOSE_NAME[p]} consents", t + D(rng.uniform(1, 5)))

# 7.2 Consent requests from advisors (EX7)
cr_n = 0
for t, adv, sid, p, case_id in CONSENT_REQ_INTENTS:
    cands = [a for a in PURPOSE_ATTRS[p] if ATTR_STATUS[a] == 'active' and not BY_KEY[(sid, p, a)]]
    if not cands:
        continue
    a = pick(cands)
    age = (NOW - t).days
    status = wpick([('pending', 75), ('granted', 15), ('denied', 10)]) if age < 3 else wpick([('granted', 45), ('denied', 25), ('withdrawn', 12), ('pending', 18 if age < 12 else 0)])
    responded = None if status == 'pending' else minute(min(t + H(rng.uniform(2, 72)), NOW - H(0.5)))
    stated = pick(CONSENT_REQ_TEXT[p])
    rid = gid('consentreq', cr_n)
    cr_n += 1
    add('CONSENTREQUESTS', consentRequestId=rid, advisorId=adv, consentPurposeId=p, dataAttributeId=a, studentId=sid, purposeStated=stated,
        requestedAt=minute(t), respondedAt=responded if status in ('granted', 'denied') else (responded if status == 'withdrawn' else None), status=status)
    log(adv, 'consentRequested', f"Asked {nm(sid)} to share {ATTR_NAME[a]} for {PURPOSE_NAME[p]}", t)
    notify(sid, 'New consent request', f"{nm(adv)} asks you to share {ATTR_NAME[a]} for {PURPOSE_NAME[p]}: {stated}", '/consents/requests', t)
    if status == 'granted':
        create_consent(sid, p, a, responded, None, 'request')
        log(sid, 'consentRequestAnswered', f"Granted the request of {nm(adv)} for {ATTR_NAME[a]}", responded)
        notify(adv, 'Consent request granted', f"{nm(sid)} shared {ATTR_NAME[a]} for {PURPOSE_NAME[p]}.", '/consents/requests/sent', responded)
    elif status == 'denied':
        log(sid, 'consentRequestAnswered', f"Denied the request of {nm(adv)} for {ATTR_NAME[a]}", responded)
        notify(adv, 'Consent request denied', f"{nm(sid)} did not share {ATTR_NAME[a]} for {PURPOSE_NAME[p]}.", '/consents/requests/sent', responded)
    elif status == 'withdrawn':
        log(adv, 'consentRequestWithdrawn', f"Withdrew the request to {nm(sid)} for {ATTR_NAME[a]}", responded)
# make sure a few pending requests exist for the inbox demo
for row in DB['CONSENTREQUESTS']:
    if row['status'] == 'withdrawn':
        row['respondedAt'] = None   # a withdrawn request was never answered by the student

# 7.3 Withdrawals (single attributes and whole purposes)
VALUE_TXNS = defaultdict(list)
for r in DATA_ROWS:
    if not r['isMissing']:
        VALUE_TXNS[(r['studentId'], r['dataAttributeId'])].append(r['transactionFrom'])
withdraw_pool = [c for c in CONSENTS if c['grantedAt'] < NOW - D(3)]
rng.shuffle(withdraw_pool)
WITHDRAWALS = []    # (time, [consents], whole_purpose)
used = set()
for c in withdraw_pool[:26]:
    if c['consentId'] in used:
        continue
    w = rand_between(c['grantedAt'] + D(1), NOW - H(2))
    WITHDRAWALS.append((office(w), [c], False))
    used.add(c['consentId'])
by_sp = defaultdict(list)
for c in CONSENTS:
    by_sp[(c['studentId'], c['consentPurposeId'])].append(c)
whole = [k for k, v in by_sp.items() if len(v) >= 2 and not any(x['consentId'] in used for x in v)]
rng.shuffle(whole)
for k in whole[:7]:
    cs = by_sp[k]
    w = rand_between(max(x['grantedAt'] for x in cs) + D(1), NOW - H(2))
    if w >= NOW - H(2):
        continue
    WITHDRAWALS.append((office(w), cs, True))
    used |= {x['consentId'] for x in cs}
WITHDRAWALS.sort(key=lambda x: x[0])
for w, cs, whole_purpose in WITHDRAWALS:
    sid = cs[0]['studentId']
    for c in cs:
        c['withdrawnAt'], c['status'] = w, 'withdrawn'
        CONSENT_HIST.append((sid, c, w, 'withdrawn', 'granted'))
    if whole_purpose:
        log(sid, 'consentWithdrawn', f"Withdrew all {len(cs)} attribute(s) of {PURPOSE_NAME[cs[0]['consentPurposeId']]}", w)
    else:
        log(sid, 'consentWithdrawn', f"Withdrew {ATTR_NAME[cs[0]['dataAttributeId']]} from {PURPOSE_NAME[cs[0]['consentPurposeId']]}", w)

# 7.4 Erasure requests queued by withdrawals, executed by a Steward (cryptographic erasure)
DESTROYS = defaultdict(list)        # (studentId, attrId) -> [destroy times]
PENDING_ERASURE = {}
ERASURES = []


def key_gen_at(sid, a, t):
    return sum(1 for d in DESTROYS[(sid, a)] if d < t)


def has_active_key(sid, a, t):
    last = max([d for d in DESTROYS[(sid, a)] if d <= t], default=None)
    return any(x <= t and (last is None or x > last) for x in VALUE_TXNS[(sid, a)])


for w, cs, _ in WITHDRAWALS:
    sid = cs[0]['studentId']
    queued = 0
    for c in cs:
        a = c['dataAttributeId']
        if any(granted_at(o, w) for o in BY_SA[(sid, a)] if o is not c):
            continue
        if not has_active_key(sid, a, w) or (sid, a) in PENDING_ERASURE:
            continue
        gen = key_gen_at(sid, a, w)
        er = dict(erasureRequestId=gid('erasure', sid, a, gen), attributeKeyId=gid('key', sid, a, gen), consentId=c['consentId'],
                  executedBy=None, executedAt=None, requestedAt=w, status='pending', _sa=(sid, a))
        ERASURES.append(er)
        PENDING_ERASURE[(sid, a)] = er
        queued += 1
        if w < NOW - D(1.5) and rng.random() < 0.75:
            ex = office(minute(min(w + H(rng.uniform(4, 70)), NOW - H(1))))
            if not any(granted_at(o, ex) for o in BY_SA[(sid, a)]):
                er.update(executedBy=pick(STEWARDS), executedAt=ex, status='completed')
                DESTROYS[(sid, a)].append(ex)
                del PENDING_ERASURE[(sid, a)]
    if queued:
        for st in STEWARDS:
            notify(st, 'Erasure request queued', f"{queued} withdrawn attribute(s) are waiting for verification and erasure.", '/erasure-requests', w)

# 7.5 Re-share after an erasure (new consent; a later import created a new key, the old values stay erased)
done = [e for e in ERASURES if e['status'] == 'completed' and e['executedAt'] < T('2026-10-24')]
for e in done[:2]:
    sid, a = e['_sa']
    old = next(c for c in CONSENTS if c['consentId'] == e['consentId'])
    t = office(minute(rand_between(T('2026-10-27'), NOW - D(1))))
    create_consent(sid, old['consentPurposeId'], a, t, None, 'page')
    log(sid, 'consentGranted', f"Granted 1 attribute(s) for {PURPOSE_NAME[old['consentPurposeId']]}", t)

# =====================================================================================================================
# 8. Correction requests (EX15) - planned before the keys, because an approved correction adds a STUDENTDATA row
# =====================================================================================================================
ROWS_BY_SAT = defaultdict(list)     # (studentId, attrId, term) -> rows
for r in DATA_ROWS:
    ROWS_BY_SAT[(r['studentId'], r['dataAttributeId'], r['_term'])].append(r)


def current_row(sid, a, term, t):
    rows = [r for r in ROWS_BY_SAT[(sid, a, term)] if r['transactionFrom'] <= t]
    return max(rows, key=lambda r: (r['transactionFrom'], r['validFrom'])) if rows else None


def case_state_at(c, t):
    state = None
    for when, adv, st in c['timeline']:
        if when <= t:
            state = (adv, st)
    return state


def open_case_advisor(sid, t):
    for c in CASES:
        if c['student']['id'] != sid:
            continue
        stt = case_state_at(c, t)
        if stt and stt[0] and stt[1] in ('open', 'reopened') and has_role(stt[0], 'aa') | has_role(stt[0], 'wa'):
            return stt[0]
    return None


CORR = []      # planned correction requests


def never_erased(sid, a):
    return not DESTROYS[(sid, a)] and (sid, a) not in PENDING_ERASURE


EXPLAIN = {
    'Attendance rate': ("Em bị ghi vắng ngày 05/10 và 12/10 dù có đi học, giảng viên đã xác nhận qua email.", lambda v: str(min(100, int(v) + rng.randint(6, 12)))),
    'Credits earned': ("Môn MAE101 kỳ Summer đã được phúc khảo đạt nhưng chưa cộng tín chỉ.", lambda v: str(int(v) + 3)),
    'GPA': ("GPA bị tính sai môn PRF192, điểm phúc khảo là 7.0 thay vì 4.5.", lambda v: f"{min(10.0, float(v) + 0.4):.1f}"),
    'Accommodation status': ("Em đã chuyển ra ở trọ từ đầu tháng 9, không còn ở ký túc xá.", lambda v: 'rented' if v != 'rented' else 'family'),
    'Tuition outstanding': ("I paid the second instalment on 2026-10-20; the tuition outstanding should be 0.", lambda v: '0.0'),
    'Part-time work hours': ("Em đã nghỉ làm thêm từ tháng 10.", lambda v: '0'),
    'Commute time': ("Em đã chuyển nhà gần trường, chỉ mất khoảng 15 phút.", lambda v: '15'),
    'English level': ("Em đã thi lại xếp lớp tiếng Anh và đạt B2.", lambda v: 'B2'),
}
REJECT_NOTES = ["Đã kiểm tra với bảng điểm danh của giảng viên, các buổi vắng là đúng.", "The attendance sheet confirms the absences.",
                "Phòng đào tạo chưa xác nhận kết quả phúc khảo, sinh viên nộp lại khi có quyết định.", "Số liệu khớp với phòng tài chính."]
APPROVE_NOTES = ["Đã đối chiếu với bảng điểm danh của giảng viên; đã sửa.", "Checked with the lecturer's attendance sheet; corrected.",
                 "Phòng đào tạo đã xác nhận kết quả phúc khảo.", "Đã xác minh với phòng tài chính."]


def plan_corrections():
    stus = [s for s in fall_students]
    rng.shuffle(stus)
    plans = [('approved_valid', 'Attendance rate', 'Fall 2026', T('2026-10-02'), T('2026-10-18'))] * 3 + \
            [('approved', 'Credits earned', 'Fall 2026', T('2026-10-01'), T('2026-11-01')),
             ('approved', 'Accommodation status', 'Fall 2026', T('2026-10-01'), T('2026-11-01'))] + \
            [('rejected', n, 'Fall 2026', T('2026-10-27'), T('2026-11-02')) for n in ['Attendance rate', 'GPA', 'Tuition outstanding']] + \
            [('rejected', 'English level', 'Fall 2026', T('2026-10-01'), T('2026-10-20'))] + \
            [('withdrawn', n, 'Fall 2026', T('2026-10-27'), T('2026-11-03')) for n in ['GPA', 'Commute time', 'Part-time work hours']] + \
            [('pending', n, 'Fall 2026', T('2026-10-28'), NOW - H(3)) for n in ['Attendance rate', 'GPA', 'Tuition outstanding', 'Credits earned',
                                                                                 'Accommodation status', 'Attendance rate', 'Commute time']]
    used_rows = set()
    for kind, name, term, lo, hi in plans:
        a = A[name]
        for s in stus:
            if not never_erased(s['id'], a):
                continue
            submitted = office(rand_between(lo, hi))
            if submitted >= NOW - H(1):
                submitted = NOW - H(2)
            src = current_row(s['id'], a, term, submitted)
            if not src or src['isMissing'] or src['studentDataId'] in used_rows:
                continue
            decided = minute(submitted + H(rng.uniform(5, 80))) if kind not in ('pending', 'withdrawn') else None
            if decided and (decided >= NOW - H(0.5) or (kind == 'approved_valid' and decided >= T('2026-10-25'))):
                continue
            check_t = decided or submitted
            adv = open_case_advisor(s['id'], check_t)
            if kind != 'withdrawn' and not adv:
                continue
            if kind == 'pending' and not open_case_advisor(s['id'], NOW - H(0.1)):
                continue
            # the value must still be the current one when the request is decided
            if decided and current_row(s['id'], a, term, decided) is not src:
                continue
            used_rows.add(src['studentDataId'])
            CORR.append(dict(kind=kind, s=s, a=a, src=src, submitted=submitted, decided=decided, adv=adv, name=name))
            if kind.startswith('approved'):
                new = new_data_row(s, a, term, None, decided, src['validFrom'],
                                   f"Correction of the value imported on {src['transactionFrom']:%Y-%m-%d}, approved on {decided:%Y-%m-%d}.", status='correction')
                ROWS_BY_SAT[(s['id'], a, term)].append(new)
                VALUE_TXNS[(s['id'], a)].append(decided)
                CORR[-1]['new'] = new
            break


plan_corrections()
for c in CORR:
    if c['kind'] == 'approved_valid':      # the reviewer can also open the as-of view of this attribute (EX13 demo)
        for p in sorted(account_purposes(c['adv']), key=lambda x: PURPOSE_NAME[x]):
            if c['a'] in PURPOSE_ATTRS[p] and not BY_KEY[(c['s']['id'], p, c['a'])]:
                gt = minute(c['submitted'] - D(1))
                create_consent(c['s']['id'], p, c['a'], gt, None, 'page')
                log(c['s']['id'], 'consentGranted', f"Granted 1 attribute(s) for {PURPOSE_NAME[p]}", gt)
                break
for c in CORR:
    expl, fn = EXPLAIN[c['name']]
    c['proposed'] = fn(c['src']['_plain'])
    if 'new' in c:
        c['new']['_plain'] = c['proposed']

# =====================================================================================================================
# 9. Attribute keys, encryption of every value
# =====================================================================================================================
KEYS = {}           # keyId -> dict(row, data_key)
ROW_KEY = {}
by_sa_rows = defaultdict(list)
for r in DATA_ROWS:
    if not r['isMissing']:
        by_sa_rows[(r['studentId'], r['dataAttributeId'])].append(r)
for (sid, a), rows in by_sa_rows.items():
    rows.sort(key=lambda r: r['transactionFrom'])
    destroys = sorted(DESTROYS[(sid, a)])
    for r in rows:
        gen = sum(1 for d in destroys if d < r['transactionFrom'])
        kid = gid('key', sid, a, gen)
        if kid not in KEYS:
            destroyed = destroys[gen] if gen < len(destroys) else None
            KEYS[kid] = dict(attributeKeyId=kid, dataAttributeId=a, studentId=sid, wrappedKey=None, masterKeyVersion=None,
                             createdAt=r['transactionFrom'], destroyedAt=destroyed, status='destroyed' if destroyed else 'active', _gen=gen)
        r['attributeKeyId'] = kid
        if KEYS[kid]['destroyedAt']:
            r['erasedAt'] = KEYS[kid]['destroyedAt']
for e in ERASURES:
    assert e['attributeKeyId'] in KEYS, e
for e in ERASURES:
    if e['status'] == 'completed':
        n = sum(1 for r in DATA_ROWS if r['attributeKeyId'] == e['attributeKeyId'])
        log(e['executedBy'], 'erasureExecuted', f"Erased {nm(e['_sa'][0])}'s {ATTR_NAME[e['_sa'][1]]} (key destroyed, {n} value(s) cleared)", e['executedAt'])
        notify(e['_sa'][0], 'Your data has been erased',
               f"Your {ATTR_NAME[e['_sa'][1]]} values can no longer be read by anyone. The record that they existed and were withdrawn is kept for audit.", '/consents', e['executedAt'])

# =====================================================================================================================
# 10. Emergency access (EX10)
# =====================================================================================================================
EMERGENCY = []
JUSTIFY = ["Sinh viên vắng học 3 tuần, không liên lạc được; cần thông tin chỗ ở để kiểm tra an toàn.",
           "Student has stopped attending and a classmate reported they may be in danger; need the living situation now.",
           "Gia đình báo sinh viên mất liên lạc 5 ngày, cần kiểm tra chỗ ở và tình hình đi học gấp.",
           "Sinh viên nhắn tin có dấu hiệu khủng hoảng rồi tắt máy; cần dữ liệu để phối hợp hỗ trợ ngay."]
REJECT_EMERGENCY = ["No immediate risk to the student; ask the student for consent instead.",
                    "Đã liên lạc được với sinh viên qua điện thoại, sinh viên an toàn; áp dụng quy trình xin đồng ý thông thường."]


def advisor_students(adv):
    return [c['student'] for c in CASES if c['advisor'] == adv and c['status'] != 'closed']


EM_PLAN = [  # requester, status, requestedAt, decide delay h, scope
    ('wa.ngocbt', 'approved', T('2026-11-03 07:10'), 0.9, T('2026-11-12 10:00')),
    ('aa.hungpt', 'approved', T('2026-11-04 01:30'), 0.8, T('2026-12-20 10:00')),
    ('wa.tamlh', 'approved', T('2026-10-09 09:00'), 0.7, T('2026-10-12 09:00')),
    ('aa.lannt', 'approved', T('2026-10-21 02:15'), 1.5, T('2026-10-24 02:00')),
    ('wa.linhvk', 'pending', T('2026-11-04 08:40'), None, None),
    ('aa.tuandq', 'pending', T('2026-11-05 01:20'), None, None),
    ('wa.dungpt', 'pending', T('2026-11-03 13:05'), None, None),
    ('aa.maidt', 'rejected', T('2026-10-22 02:43'), 1.9, None),
    ('wa.ngocbt', 'rejected', T('2026-10-15 09:59'), 3.2, None),
]
for i, (key, status, req_at, delay, scope) in enumerate(EM_PLAN):
    adv = S[key]
    cands = [s for s in advisor_students(adv) if s['status'] == 'active'] or [s for s in fall_students if s['fac'] == ACC[adv]['fac']]
    s = cands[i % len(cands)]
    steward = STEWARDS[i % 2]
    decided = minute(req_at + H(delay)) if delay else None
    j = pick(JUSTIFY)
    row = add('EMERGENCYACCESSREQUESTS', emergencyAccessRequestId=gid('emerg', i), requestedBy=adv, stewardId=steward if decided else None,
              studentId=s['id'], justification=j, rejectionReason=pick(REJECT_EMERGENCY) if status == 'rejected' else None,
              requestedAt=req_at, decidedAt=decided, scopeExpiresAt=scope, status=status)
    EMERGENCY.append(row)
    log(adv, 'emergencyAccessRequested', f"Requested emergency access to {s['name']}'s record", req_at)
    for st in STEWARDS:
        notify(st, 'Emergency access requested', f"{nm(adv)} asks for emergency access to {s['name']}'s record.", '/emergency-access', req_at)
    if status == 'approved':
        log(steward, 'emergencyAccessDecided', f"Approved emergency access of {nm(adv)} to {s['name']}'s record until {scope:%Y-%m-%d %H:%M}", decided)
        notify(adv, 'Emergency access approved', f"You may access {s['name']}'s record until {scope:%Y-%m-%d %H:%M} (UTC). Every access is logged.", '/emergency-access', decided)
        for k in range(rng.randint(1, 3)):
            rt = decided + H(0.3 + k * rng.uniform(2, 20))
            if rt < min(scope, NOW):
                READS.append((minute(rt), adv, s['id'], None, True))
    elif status == 'rejected':
        log(steward, 'emergencyAccessDecided', f"Rejected emergency access of {nm(adv)} to {s['name']}'s record: {row['rejectionReason']}", decided)
        notify(adv, 'Emergency access rejected', f"Your emergency access request for {s['name']}'s record was rejected: {row['rejectionReason']}", '/emergency-access', decided)
    if i == 4:
        log(adv, 'emergencyAccessRequestUpdated', f"Edited the justification of the emergency access request for {s['name']}", req_at + H(0.4))


def emergency_ok(accessor, sid, t):
    return any(e['requestedBy'] == accessor and e['studentId'] == sid and e['status'] == 'approved' and e['decidedAt'] <= t < e['scopeExpiresAt']
               for e in EMERGENCY)


# =====================================================================================================================
# 11. Access logs (one row per value shown to staff, exactly like IAccessGuardService.LogAccessBatchAsync)
# =====================================================================================================================
ROWS_BY_STUDENT = defaultdict(list)
for r in DATA_ROWS:
    ROWS_BY_STUDENT[r['studentId']].append(r)
ACCESS = []
for t, accessor, sid, pfilter, emergency in sorted(READS, key=lambda x: x[0]):
    purposes = account_purposes(accessor)
    if pfilter is not None:
        purposes &= pfilter
    current = {}
    for r in ROWS_BY_STUDENT[sid]:
        if r['transactionFrom'] <= t:
            k = (r['dataAttributeId'], r['_term'])
            if k not in current or (r['transactionFrom'], r['validFrom']) > (current[k]['transactionFrom'], current[k]['validFrom']):
                current[k] = r
    emerg = emergency_ok(accessor, sid, t)
    for (a, term), r in sorted(current.items(), key=lambda kv: (TERM_START[kv[0][1]], ATTR_NAME[kv[0][0]])):
        cons = [c for c in BY_SA[(sid, a)] if usable_at(c, t) and c['consentPurposeId'] in purposes and a in PURPOSE_ATTRS[c['consentPurposeId']]]
        if cons:
            purpose = sorted(cons, key=lambda c: PURPOSE_NAME[c['consentPurposeId']])[0]['consentPurposeId']
        elif emerg:
            purpose = None
        else:
            continue
        if r['isMissing']:
            continue
        ACCESS.append(dict(accessLogId=gid('access', len(ACCESS)), accessorId=accessor, consentPurposeId=purpose, recordedUserId=sid,
                           studentDataId=r['studentDataId'], accessedResource=f"Student data: {ATTR_NAME[a]} ({term})",
                           accessedAt=minute(t), isFlagged=False, status=None))
# flagged entries (EX9)
flag_pool = [x for x in ACCESS if x['accessedAt'] < NOW - D(1) and x['consentPurposeId'] is not None]
rng.shuffle(flag_pool)
for i, x in enumerate(flag_pool[:14]):
    by_student = i < 12
    flagged_at = minute(min(x['accessedAt'] + H(rng.uniform(1, 120)), NOW - H(1)))
    x['isFlagged'] = True
    status = 'unreviewed' if i % 3 == 0 or (NOW - flagged_at).days < 1 else ('cleared' if i % 3 == 1 else 'confirmedIssue')
    x['status'] = status
    flagger = x['recordedUserId'] if by_student else pick(STEWARDS)
    log(flagger, 'accessLogEntryFlagged', f"Flagged the access by {nm(x['accessorId'])} to {x['accessedResource']} on {x['accessedAt']:%Y-%m-%d %H:%M}", flagged_at)
    if by_student:
        for st in STEWARDS:
            notify(st, 'Access log entry flagged', f"A student flagged an access by {nm(x['accessorId'])} on {x['accessedAt']:%Y-%m-%d %H:%M} for review.",
                   '/access-logs?isFlagged=true', flagged_at)
    if status != 'unreviewed':
        rv = minute(min(flagged_at + H(rng.uniform(2, 60)), NOW - H(0.5)))
        log(pick(STEWARDS), 'accessLogReviewed',
            f"Reviewed the access by {nm(x['accessorId'])} to {nm(x['recordedUserId'])}'s {x['accessedResource']} on {x['accessedAt']:%Y-%m-%d %H:%M}: {status}", rv)
for x in ACCESS:
    add('ACCESSLOGS', **x)

# =====================================================================================================================
# 12. Sessions, sign-ins and the remaining activity history
# =====================================================================================================================
DEVICES = ['Chrome 141 on Windows 11', 'Edge 141 on Windows 11', 'Firefox 143 on Ubuntu', 'Safari on iPhone (iOS 19)', 'Chrome 141 on Android 16',
           'Chrome 141 on macOS', 'Safari 19 on macOS', 'Samsung Internet on Android 15', 'Chrome 141 on iPad']
sess_n = 0
for aid, acc in ACC.items():
    if aid in SEED_IDS or acc['link'] != 'linked':
        continue
    link_at = minute(acc['created'] + D(rng.uniform(0.5, 6)))
    log(aid, 'accountLinked', f"Linked the Google account {acc['email']}", link_at)
    no_pw = any(r['accountId'] == aid and r['passwordHash'] is None for r in DB['ACCOUNTS'])
    if not no_pw:
        log(aid, 'passwordSet', 'Set a first password after linking the Google account', link_at + H(0.1))
    inactive = acc.get('deactivated_at') or (STU[aid].get('inactive_at') if aid in STU else None)
    end = min(NOW - H(0.2), inactive) if inactive else NOW - H(0.2)
    start = max(link_at, NOW - D(70))
    if start >= end:
        continue
    staff = aid not in STU
    n = rng.randint(4, 9) if staff else rng.randint(1, 6)
    times = sorted(rand_between(start, end) for _ in range(n))
    dev_pref = rng.sample(DEVICES, 2)
    for k, t in enumerate(times):
        t = office(t)
        if t >= end:
            continue
        device = dev_pref[0] if rng.random() < 0.75 else dev_pref[1]
        last = k == len(times) - 1
        is_active = last and not inactive and (NOW - t).days < 30 and rng.random() < 0.75
        logout = None if is_active else minute(min(t + H(rng.uniform(0.3, 9)), end if inactive else NOW - H(0.1)))
        add('SESSIONS', sessionId=gid('session', sess_n), accountId=aid, deviceInfo=device, loginAt=t, logoutAt=logout, isActive=is_active)
        sess_n += 1
        method = 'Google' if (no_pw or rng.random() < 0.55) else 'password'
        log(aid, 'signedIn', f"Signed in with {method} on {device}", t)
        if logout and not inactive and rng.random() < 0.5:
            log(aid, 'signedOut', f"Signed out on {device}", logout)

# Admin: accounts, master data, imports
adm = S['admin02']
log(adm, 'facultyCreated', 'Created faculty SE - Software Engineering', T('2026-04-06 02:00'))
for code, (fid, name, st) in FAC.items():
    if code != 'SE':
        log(adm, 'facultyCreated', f"Created faculty {code} - {name}", T('2026-04-06 02:05'))
log(adm, 'facultyUpdated', 'Archived faculty JP - Japanese Language', T('2026-08-15 03:00'))
for row in DB['PROGRAMMES']:
    log(adm, 'programmeCreated', f"Created programme {row['programmeCode']} - {row['programmeName']}", T('2026-04-06 02:20'))
log(adm, 'programmeUpdated', 'Archived programme BIB - Bachelor of International Business (closed intake)', T('2026-08-15 03:05'))
for name in TERM:
    log(adm, 'termCreated', f"Created term {name}", T('2026-04-06 02:30'))
log(adm, 'termUpdated', 'Set Fall 2026 as the current term (Summer 2026 moved to past)', T('2026-09-07 00:10'))
for row in DB['SUBJECTS']:
    log(adm, 'subjectCreated', f"Created subject {row['subjectCode']} - {row['subjectName']}", T('2026-04-06 03:00'))
log(adm, 'subjectUpdated', 'Archived subject OSG202 - Operating Systems', T('2026-08-20 03:00'))
log(adm, 'subjectUpdated', 'Archived subject ANS301 - 3D Animation (old curriculum)', T('2026-08-20 03:02'))
for row in DB['ACCOUNT_SUBJECT']:
    log(adm, 'advisorSubjectLinked', f"Linked {nm(row['accountId'])} to {SUBJ_CODE[row['subjectId']]}", row['assignedAt'])
for c in CAT.values():
    log(adm, 'supportCategoryCreated', f"Created support category {c['name']} ({c['type']})", T('2026-04-10 03:00'))
log(adm, 'supportCategoryUpdated', 'Archived support category Thesis topic (old)', T('2026-08-25 02:00'))
for key, user, *_ in STAFF_DEF:
    if key != 'admin02':
        log(adm, 'accountCreated', f"Created account {user}@example.com", ACC[S[key]]['created'])
log(adm, 'accountRolesChanged', 'Added the role Programme Head to aa.khoavd@example.com', T('2026-04-03 03:12'))
k19 = sum(1 for s in STUDENTS if s['cohort'] != 'K20')
log(adm, 'accountsImported', f"Imported {k19} accounts from students_spring2026.xlsx", T('2026-04-22 02:00'))
log(adm, 'accountsImported', f"Imported {len(STUDENTS) - k19} accounts from students_k20_fall2026.xlsx", T('2026-08-28 03:00'))
log(adm, 'linkingEmailsSent', f"Sent linking emails to {k19} account(s)", T('2026-04-22 02:30'))
log(adm, 'linkingEmailsSent', f"Sent linking emails to {len(STUDENTS) - k19} account(s)", T('2026-08-28 03:20'))
log(adm, 'linkingEmailsSent', 'Resent linking emails to 14 account(s)', T('2026-10-05 02:00'))
for s in STUDENTS:
    if s['academic'] != 'enrolled':
        log(adm, 'studentProfileUpdated', f"Changed {s['code']} academic status to {s['academic']}", s['left_at'] or T('2026-09-01'))
    if s['status'] == 'deactivated':
        log(adm, 'accountDeactivated', f"Deactivated {s['user']}@example.com", s['inactive_at'])
    if s['status'] == 'banned':
        log(adm, 'accountBanned', f"Banned {s['user']}@example.com", s['inactive_at'])
log(adm, 'accountDeactivated', 'Deactivated aa.sonhm@example.com', ACC[S['aa.sonhm']]['deactivated_at'])
log(adm, 'accountDeactivated', 'Deactivated wa.hoaitt@example.com', ACC[S['wa.hoaitt']]['deactivated_at'])
for imp, ins, miss in IMPORT_SUMMARY:
    log(adm, 'studentDataImported', f"Imported {ins} value(s) from {imp['file']}; recorded {miss} missing value(s); left out 0 row(s) of archived data attributes", imp['at'])
log(adm, 'studentDataImportFailed', 'Refused studentdata_fall2026_midterm_v1.xlsx: 6 invalid row(s)', T('2026-10-26 02:05'))
# Steward: consent set-up
stw = S['steward02']
for aid, name, *_ in ATTRS:
    log(stw, 'dataAttributeCreated', f"Created data attribute {name}", T('2026-04-08 02:00'))
for aid, name, _, st, _ in ATTRS:
    if st == 'archived':
        log(stw, 'dataAttributeArchived', f"Archived data attribute {name}", T('2026-09-01 02:00'))
for p, name in PURPOSE_NAME.items():
    log(stw, 'consentPurposeCreated', f"Created consent purpose {name}", T('2026-04-08 03:00'))
    if PURPOSE_STATUS[p] in ('active', 'retired'):
        log(stw, 'consentPurposeUpdated', f"Activated consent purpose {name}", T('2026-04-08 03:30'))
log(stw, 'consentPurposeRetired', 'Retired consent purpose Retention pilot 2025', T('2026-04-09 02:00'))
for pid, pk, rk, st in POLICIES:
    log(stw, 'accessPolicyCreated', f"Created access policy {ROLE[rk][1]} - {PURPOSE_NAME[P[pk]]}", T('2026-04-09 03:00'))
    if st in ('active', 'revoked'):
        log(stw, 'accessPolicyActivated', f"Activated access policy {ROLE[rk][1]} - {PURPOSE_NAME[P[pk]]}", T('2026-04-09 03:10'))
    if st == 'revoked':
        log(stw, 'accessPolicyRevoked', f"Revoked access policy {ROLE[rk][1]} - {PURPOSE_NAME[P[pk]]}", T('2026-09-15 02:00'))
for t in [T('2026-10-05 03:00'), T('2026-10-31 08:00'), T('2026-11-04 02:30')]:
    log(pick(STEWARDS), 'processingRecordExported', 'Exported the processing record for Fall 2026 (xlsx)', t)

# Reports (D6) and the REPORTS table
REPORT_ROWS = [
    ('ph.quanpm', 'Summer 2026', 'caseReport', T('2026-08-26 02:00'), 'ready'), ('ph.quanpm', 'Summer 2026', 'caseloadClosure', T('2026-08-26 02:05'), 'ready'),
    ('ph.quanpm', 'Fall 2026', 'caseReport', T('2026-10-05 03:00'), 'ready'), ('aa.khoavd', 'Fall 2026', 'caseloadClosure', T('2026-10-19 07:00'), 'ready'),
    ('ph.quanpm', 'Fall 2026', 'caseReport', T('2026-11-02 02:00'), 'ready'), ('ph.quanpm', 'Fall 2026', 'caseloadClosure', T('2026-11-04 08:00'), 'generating'),
    ('steward02', 'Fall 2026', 'processingRecord', T('2026-10-31 08:00'), 'ready'), ('wo.yennh', 'Fall 2026', 'welfareStatistics', T('2026-10-30 04:00'), 'failed'),
    ('steward03', 'Spring 2026', 'processingRecord', T('2026-05-04 02:00'), 'ready'), ('aa.khoavd', 'Fall 2026', 'caseReport', T('2026-10-28 09:30'), 'failed'),
]
for i, (who, term, typ, t, st) in enumerate(REPORT_ROWS):
    add('REPORTS', reportId=gid('report', i), generatedBy=S[who], termId=TERM_ID[term],
        filePath=f"reports/{t:%Y/%m}/{typ}_{term.replace(' ', '').lower()}_{t:%Y%m%d%H%M}.xlsx" if st == 'ready' else None,
        reportType=typ, generatedAt=t, status=st)
    if typ in ('caseReport', 'caseloadClosure') and st == 'ready':
        log(S[who], 'reportExported', f"Exported the {typ} report for {term} (xlsx)", t)
# Students: exports, profile/password/notification settings
for s in rng.sample(fall_students, 12):
    log(s['id'], 'dataExported', 'Downloaded the JSON export of own record', rand_between(T('2026-09-20'), NOW - H(2)))
for s in rng.sample([x for x in STUDENTS if x['link'] == 'linked'], 18):
    log(s['id'], 'profileUpdated', 'Changed the preferred language', rand_between(max(s['created'] + D(7), T('2026-09-01')), NOW - H(2)))
for s in rng.sample([x for x in STUDENTS if x['link'] == 'linked' and not x['google_only']], 9):
    log(s['id'], 'passwordChanged', 'Changed password in Settings', rand_between(T('2026-09-10'), NOW - H(2)))
pref_accounts = rng.sample([a for a in ACC if a not in SEED_IDS and ACC[a]['link'] == 'linked'], 45)
for i, aid in enumerate(pref_accounts):
    channels = ['email'] if i % 3 == 0 else ['inApp'] if i % 3 == 1 else ['email', 'inApp']
    for ch in channels:
        add('NOTIFICATIONPREFERENCES', notificationPreferenceId=gid('pref', aid, ch), accountId=aid, channel=ch, isEnabled=rng.random() < 0.45)
    log(aid, 'notificationPreferencesUpdated', f"Turned the {channels[0]} channel {'on' if rng.random() < 0.5 else 'off'}", rand_between(T('2026-09-10'), NOW - H(2)))

# Backups / restores
for i in range(21):
    t = T('2026-10-15 19:00') + D(i)
    if t > NOW:
        break
    failed = i == 12
    manual = i in (5, 16)
    add('BACKUPS', backupId=gid('backup', i), createdBy=S['admin02'] if manual else None, backupTimestamp=t, sizeBytes=None if failed else 61_000_000 + i * 1_350_000 + rng.randint(0, 400_000),
        status='failed' if failed else 'completed')
add('RESTOREOPERATIONS', restoreOperationId=gid('restore', 0), backupId=gid('backup', 4), executedBy=S['admin02'],
    integrityCheckResult='All 43 tables present; row counts match the backup.', executedAt=T('2026-10-22 03:00'), status='verified')
add('RESTOREOPERATIONS', restoreOperationId=gid('restore', 1), backupId=gid('backup', 11), executedBy=S['admin03'],
    integrityCheckResult='Checksum mismatch on CASENOTES; restore stopped.', executedAt=T('2026-10-29 03:00'), status='failed')
add('RESTOREOPERATIONS', restoreOperationId=gid('restore', 2), backupId=gid('backup', 19), executedBy=S['admin02'],
    integrityCheckResult=None, executedAt=None, status='running')

# =====================================================================================================================
# 13. Encryption + final rows for STUDENTDATA, ATTRIBUTEKEYS, CORRECTIONREQUESTS, CONSENTS, CONSENTHISTORIES, ERASUREREQUESTS
# =====================================================================================================================
def parse_args():
    here = os.path.dirname(os.path.abspath(__file__))
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--appsettings', default=os.path.join(here, '..', '..', 'AdviseCare_API', 'AdviseCare.API', 'appsettings.json'))
    ap.add_argument('--master-key', help='base64 32-byte master key (overrides --appsettings)')
    ap.add_argument('--master-key-version', type=int, help='master key version (default: Encryption:CurrentVersion)')
    ap.add_argument('--out', default=os.path.join(here, 'AdviseCare_Mock_Data.sql'))
    return ap.parse_args()


ARGS = parse_args()
if ARGS.master_key:
    MASTER_B64, MASTER_VER = ARGS.master_key, ARGS.master_key_version or 1
else:
    with open(ARGS.appsettings, encoding='utf-8-sig') as f:
        enc = json.loads(re.sub(r'^\s*//.*$', '', f.read(), flags=re.M))['Encryption']
    MASTER_VER = ARGS.master_key_version or int(enc['CurrentVersion'])
    MASTER_B64 = enc['MasterKeys'][str(MASTER_VER)]
CRYPTO = Crypto(MASTER_B64, MASTER_VER)
DATA_KEYS = {}
for kid, k in KEYS.items():
    k['masterKeyVersion'] = MASTER_VER
    if k['status'] == 'active':
        ctx = CRYPTO.key_context(kid, k['studentId'], k['dataAttributeId'])
        k['wrappedKey'], DATA_KEYS[kid] = CRYPTO.new_wrapped_key(ctx)
    add('ATTRIBUTEKEYS', **{c: k[c] for c in COLS['ATTRIBUTEKEYS']})

for r in DATA_ROWS:
    if not r['isMissing'] and r['attributeKeyId'] in DATA_KEYS:
        r['dataValue'] = CRYPTO.encrypt_value(DATA_KEYS[r['attributeKeyId']], r['attributeKeyId'], r['studentId'], r['dataAttributeId'],
                                              r['studentDataId'], 'dataValue', r['_plain'])
    add('STUDENTDATA', **{c: r[c] for c in COLS['STUDENTDATA']})

for i, c in enumerate(sorted(CORR, key=lambda x: x['submitted'])):
    crid = gid('correction', i)
    src, s, a = c['src'], c['s'], c['a']
    kid = src['attributeKeyId']
    cur = prop = None
    if kid in DATA_KEYS:
        cur = CRYPTO.encrypt_value(DATA_KEYS[kid], kid, s['id'], a, crid, 'currentValue', src['_plain'])
        prop = CRYPTO.encrypt_value(DATA_KEYS[kid], kid, s['id'], a, crid, 'proposedValue', c['proposed'])
    status = {'approved_valid': 'approved'}.get(c['kind'], c['kind'])
    notes = pick(APPROVE_NOTES) if status == 'approved' else pick(REJECT_NOTES) if status == 'rejected' else None
    add('CORRECTIONREQUESTS', correctionRequestId=crid, reviewedBy=c['adv'] if status in ('approved', 'rejected') else None,
        studentDataId=src['studentDataId'], studentId=s['id'], currentValue=cur, decisionNotes=notes, explanation=EXPLAIN[c['name']][0],
        proposedValue=prop, decidedAt=c['decided'], submittedAt=c['submitted'], status=status)
    term = src['_term']
    log(s['id'], 'correctionRequested', f"Asked to correct {c['name']} ({term})", c['submitted'])
    if status == 'withdrawn':
        log(s['id'], 'correctionRequestWithdrawn', f"Withdrew the correction request on {c['name']} ({term})",
            minute(min(c['submitted'] + H(rng.uniform(2, 30)), NOW - H(0.5))))
    elif status == 'approved':
        log(c['adv'], 'correctionReviewed', f"Approved the correction of {s['name']}'s {c['name']} ({term})", c['decided'])
        notify(s['id'], 'Correction request approved', f"Your correction of {c['name']} ({term}) was approved.", '/my-data', c['decided'])
    elif status == 'rejected':
        log(c['adv'], 'correctionReviewed', f"Rejected the correction of {s['name']}'s {c['name']} ({term}): {notes}", c['decided'])
        notify(s['id'], 'Correction request rejected', f"Your correction of {c['name']} was rejected: {notes}", '/my-data', c['decided'])

for c in CONSENTS:
    add('CONSENTS', **{k: c[k] for k in COLS['CONSENTS']})
for i, (by, c, t, new, prev) in enumerate(sorted(CONSENT_HIST, key=lambda x: x[2])):
    add('CONSENTHISTORIES', consentHistoryId=gid('chist', i), changedBy=by, consentId=c['consentId'], changedAt=t, newStatus=new, previousStatus=prev)
for e in ERASURES:
    add('ERASUREREQUESTS', **{k: e[k] for k in COLS['ERASUREREQUESTS']})

# Self-check: decrypt every stored value again with an independent unwrap
for k in DB['ATTRIBUTEKEYS']:
    if k['status'] == 'active':
        ctx = CRYPTO.key_context(k['attributeKeyId'], k['studentId'], k['dataAttributeId'])
        assert CRYPTO.unwrap(k['wrappedKey'], ctx) == DATA_KEYS[k['attributeKeyId']]
for r in DATA_ROWS:
    if r['dataValue'] is not None:
        dk = CRYPTO.unwrap(KEYS[r['attributeKeyId']]['wrappedKey'], CRYPTO.key_context(r['attributeKeyId'], r['studentId'], r['dataAttributeId']))
        assert CRYPTO.decrypt_value(dk, r['attributeKeyId'], r['studentId'], r['dataAttributeId'], r['studentDataId'], 'dataValue', r['dataValue']) == r['_plain']

# =====================================================================================================================
# 14. Write the SQL script
# =====================================================================================================================
SORT_KEY = {'CASES': 'openedAt', 'CASESTAGEHISTORIES': 'changedAt', 'CASENOTES': 'createdAt', 'REFERRALS': 'createdAt', 'NOTIFICATIONS': 'createdAt',
            'ACTIVITYLOGS': 'occurredAt', 'SESSIONS': 'loginAt', 'ACCESSLOGS': 'accessedAt', 'CONSENTS': 'grantedAt', 'CONSENTREQUESTS': 'requestedAt',
            'STUDENTDATA': 'transactionFrom', 'CASECHANGEREQUESTS': 'requestedAt', 'ESCALATIONS': 'escalatedAt', 'INTERVENTIONS': 'interventionDate',
            'EXTERNALREFERRALS': 'referredAt', 'ERASUREREQUESTS': 'requestedAt', 'EMERGENCYACCESSREQUESTS': 'requestedAt', 'CORRECTIONREQUESTS': 'submittedAt'}


def lit(v):
    if v is None:
        return 'NULL'
    if isinstance(v, bool):
        return '1' if v else '0'
    if isinstance(v, int):
        return str(v)
    if isinstance(v, dt.datetime):
        return f"'{v:%Y-%m-%dT%H:%M:%S}.000'"
    if isinstance(v, dt.time):
        return f"'{v:%H:%M:%S}'"
    if isinstance(v, (bytes, bytearray)):
        return '0x' + bytes(v).hex().upper()
    if isinstance(v, str):
        return "N'" + v.replace("'", "''") + "'"
    raise TypeError(type(v))


def counts():
    return {t: len(DB[t]) for t in TABLE_ORDER}


def write_sql(path):
    for t, k in SORT_KEY.items():
        DB[t].sort(key=lambda r: r[k])
    c = counts()
    total = sum(c.values())
    open_cases = sum(1 for x in CASES if x['status'] != 'closed')
    with open(path, 'w', encoding='utf-8-sig', newline='\r\n') as f:   # BOM: SSMS and sqlcmd read the Vietnamese text correctly
        w = f.write
        w("/* =====================================================================================================================\n")
        w("   AdviseCare - FULL MOCK DATA (generated by generate_mock_data.py - do not edit by hand, re-run the generator)\n")
        w(f"   Reference date: {NOW:%Y-%m-%d %H:%M} UTC (Fall 2026 is the current term). Total rows: {total}.\n")
        w("   Run AFTER AdviseCare_Database_Script.sql (needs its 43 tables). This script DELETES every row of the 43 tables and\n")
        w("   inserts the data below in one transaction (all or nothing). It can be re-run at any time.\n")
        w(f"   STUDENTDATA values are REALLY encrypted with Encryption:MasterKeys version {MASTER_VER} of the appsettings.json used\n")
        w("   when generating: the API must run with that same key, otherwise every value shows as 'could not be decrypted'.\n")
        w("   Passwords: 123456 for every account (except 3-4 student accounts that sign in with Google only).\n")
        w("   The 7 seed accounts (admin@gmail.com, steward@, academicadvisor@, wellbeingadvisor@, welfareofficer@, student@,\n")
        w("   programmehead@) are kept clean so AdviseCare_API_Workflow_List.txt still starts from zero for them.\n")
        w(f"   Cases: {len(CASES)} ({open_cases} not closed). See README_MockData_vi.md for the accounts to test each feature.\n")
        w("   Row counts:\n")
        for t in TABLE_ORDER:
            w(f"     {t:<26}{c[t]:>7}\n")
        w("   ===================================================================================================================== */\n\n")
        w("USE [AdviseCareDB];\nGO\n\nSET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        w("IF OBJECT_ID(N'dbo.RESTOREOPERATIONS', N'U') IS NULL OR COL_LENGTH(N'dbo.CASECHANGEREQUESTS', N'decisionNotes') IS NULL\n")
        w("    THROW 50000, N'Run AdviseCare_Database_Script.sql first (the 43 tables, including CASECHANGEREQUESTS.decisionNotes, are needed).', 1;\n\n")
        w("BEGIN TRY\n    BEGIN TRANSACTION;\n\n    /* ---------- 1. Clear every table (children first) ---------- */\n")
        for t in reversed(TABLE_ORDER):
            w(f"    DELETE FROM [dbo].[{t}];\n")
        w("\n    /* ---------- 2. Insert (parents first) ---------- */\n")
        for t in TABLE_ORDER:
            rows = DB[t]
            w(f"\n    -- {t}: {len(rows)} rows\n")
            cols = COLS[t]
            for i in range(0, len(rows), 400):
                chunk = rows[i:i + 400]
                w(f"    INSERT INTO [dbo].[{t}] ({', '.join('[' + x + ']' for x in cols)}) VALUES\n")
                w(',\n'.join('        (' + ', '.join(lit(r[x]) for x in cols) + ')' for r in chunk))
                w(';\n')
        w("\n    COMMIT TRANSACTION;\n")
        w(f"    PRINT N'AdviseCare mock data loaded: {total} rows in 43 tables.';\n")
        w("END TRY\nBEGIN CATCH\n    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH\nGO\n")
    return c



def build_readme(path, sql_name):
    """A Vietnamese guide that names concrete accounts/records for every feature (always in sync with the data)."""
    u = lambda aid: ACC[aid]['user']
    case_url = lambda c: f"`{c['id']}`"
    out = []
    w = out.append
    open_cases = [c for c in CASES if c['status'] != 'closed']
    w("# AdviseCare - Bộ mock data đầy đủ\n")
    w(f"File SQL: `{sql_name}` (sinh bởi `generate_mock_data.py`). Ngày tham chiếu của dữ liệu: **{NOW:%d/%m/%Y %H:%M} UTC** (Fall 2026 là kỳ hiện tại).\n")
    w("## Cách chạy\n")
    w("1. Chạy `AdviseCare_Database_Script.sql` (tạo DB + 43 bảng). Có thể bỏ qua nếu DB đã có đủ bảng.")
    w(f"2. Chạy `{sql_name}`: script **xoá toàn bộ dữ liệu của 43 bảng** rồi nạp lại trong một transaction (lỗi thì rollback hết). Chạy lại bao nhiêu lần cũng được.")
    w("   - File ~7 MB: nếu SSMS mở chậm, dùng `sqlcmd -S localhost -E -f 65001 -i " + sql_name + "` (hoặc `-U sa -P ...` thay cho `-E`).")
    w(f"3. API phải dùng **đúng master key đã dùng khi sinh file** (`Encryption:MasterKeys:{MASTER_VER}` trong `AdviseCare_API/AdviseCare.API/appsettings.json`). "
      "Nếu đổi key, chạy lại `python generate_mock_data.py` để sinh file mới, nếu không mọi giá trị dữ liệu sinh viên sẽ báo không giải mã được.\n")
    w("## Tài khoản\n")
    w("Mật khẩu chung: **123456**. Đăng nhập bằng email hoặc username.\n")
    w("| Vai trò | Username (email = username@example.com) | Ghi chú |")
    w("|---|---|---|")
    w("| System Administrator | admin02, admin03 | admin03 chưa link Google |")
    w("| Data Protection Steward | steward02, steward03 | |")
    w("| Programme Head | ph.quanpm, aa.khoavd | aa.khoavd vừa là Academic Advisor vừa là Programme Head |")
    w("| Academic Advisor | aa.hungpt, aa.lannt, aa.khoavd, aa.thaonp, aa.phongld, aa.vylt, aa.tuandq, aa.maidt, aa.huyenbt, aa.trangnh | aa.sonhm đã bị deactivate (đăng nhập phải ra 403) |")
    w("| Wellbeing Advisor | wa.ngocbt, wa.tamlh, wa.linhvk, wa.dungpt | wa.hoaitt đã bị deactivate |")
    w("| Student Welfare Officer | wo.yennh, wo.namtv, wo.thuylm | |")
    w(f"| Student | {len(STUDENTS)} tài khoản mock, xem các ví dụ bên dưới | username dạng `tenhoxxse18xxxx` |")
    w("\n7 tài khoản seed (`admin@gmail.com`, `steward@gmail.com`, ... `programmehead@gmail.com`) vẫn **sạch** như trước để chạy theo `AdviseCare_API_Workflow_List.txt` từ đầu.\n")
    google_only = [s for s in STUDENTS if s['google_only']]
    banned = [s for s in STUDENTS if s['status'] == 'banned']
    deact = [s for s in STUDENTS if s['status'] == 'deactivated']
    w("Tài khoản đặc biệt:")
    w(f"- Chỉ có Google, không có mật khẩu (hasPassword = false, không đăng nhập bằng mật khẩu được; dùng để kiểm tra dashboard admin): {', '.join(s['user'] for s in google_only) or '(không có)'}")
    w(f"- Bị ban: {', '.join(s['user'] for s in banned)}; bị deactivate: {', '.join(s['user'] for s in deact[:4])}")
    w("- `linkStatus` có đủ 3 giá trị notLinked / emailSent / linked để test gửi lại email liên kết.\n")

    w("## Dữ liệu theo từng chức năng\n")
    ph_pending = [c for c in CASES if c['stage'] == 'pending']
    overdue = [c for c in ph_pending if (NOW - c['openedAt']).days >= 2]
    w(f"**Ghép case (Programme Head)** - đăng nhập `ph.quanpm`: hàng đợi có {len(ph_pending)} case `pending`, trong đó {len(overdue)} case chờ quá 2 ngày. "
      f"Có {sum(1 for r in DB['CASECHANGEREQUESTS'] if r['status'] == 'pending')} yêu cầu đổi cố vấn đang chờ duyệt và "
      f"{sum(1 for e in DB['ESCALATIONS'] if e['status'] == 'pending')} escalation chưa trả lời. Báo cáo: chọn khoảng 01/05/2026 - 31/12/2026 để thấy cả case kỳ Summer.\n")
    load = defaultdict(int)
    for c in open_cases:
        if c['advisor']:
            load[c['advisor']] += 1
    w("**Cố vấn (danh sách case được giao)** - số case đang mở: " + ', '.join(f"{u(a)} ({n})" for a, n in sorted(load.items(), key=lambda x: -x[1])) + ".\n")
    shared = None
    for c in open_cases:
        if c['advisor'] and c['cat']['purpose'] == P['academic'] and has_role(c['advisor'], 'aa'):
            n = [x for x in BY_SA if x[0] == c['student']['id'] and any(usable_at(k, NOW) and k['consentPurposeId'] == P['academic'] for k in BY_SA[x])]
            if len(n) >= 3:
                shared = c
                break
    if shared:
        w(f"**Xem dữ liệu sinh viên có đồng ý** - đăng nhập `{u(shared['advisor'])}`, mở case {case_url(shared)} của `{shared['student']['user']}`: "
          "các thuộc tính đã chia sẻ hiện giá trị + nguồn import, phần còn lại hiện \"exists, not shared\", dòng thiếu hiện isMissing. Mỗi lần xem sinh ra dòng ACCESSLOGS mới.\n")
    vt = [c for c in CORR if c['kind'] == 'approved_valid']
    if vt:
        c = vt[0]
        w(f"**Way back machine (EX13)** - sinh viên `{c['s']['user']}` có Attendance rate Fall 2026 = {c['src']['_plain']} (import 28/09), được đính chính thành {c['proposed']} "
          f"ngày {c['decided']:%d/%m/%Y} với validFrom 26/09, sau đó import giữa kỳ 26/10 ghi giá trị mới. Thử as-of `2026-10-{c['decided'].day - 1 if c['decided'].day > 1 else 1:02d}` "
          f"với timeAxis=system (giá trị cũ) và timeAxis=valid (giá trị đã sửa), rồi so sánh với `2026-11-01`. Đăng nhập `{u(c['adv'])}` (người đã duyệt đính chính, sinh viên đã chia sẻ Attendance rate cho vai trò này).\n")
    pend = [c for c in CORR if c['kind'] == 'pending']
    w("**Yêu cầu đính chính (EX15)** - " + ', '.join(f"`{c['s']['user']}` ({c['name']}, người duyệt: `{u(open_case_advisor(c['s']['id'], NOW))}`)" for c in pend[:4])
      + f" đang chờ duyệt. Tổng: {len(CORR)} yêu cầu (pending/approved/rejected/withdrawn).\n")
    refs_p = [(c, r) for c in CASES for r in c['refs'] if r['status'] == 'pending']
    to_wo = [(c, r) for c, r in refs_p if has_role(r['toAccountId'], 'wo')]
    to_adv = [(c, r) for c, r in refs_p if not has_role(r['toAccountId'], 'wo')]
    w(f"**Chuyển tuyến (EX4, WF2b)** - {len(refs_p)} referral đang chờ: " +
      ', '.join(f"`{u(r['toAccountId'])}` nhận từ `{u(r['fromAccountId'])}`" for c, r in (to_wo[:3] + to_adv[:3])) + ".\n")
    wo_busy = defaultdict(int)
    for c in CASES:
        if c['wo']:
            wo_busy[c['wo']] += 1
    w("**Welfare Officer** - case đã nhận: " + ', '.join(f"`{u(a)}` ({n})" for a, n in wo_busy.items()) +
      f". Có {len(DB['INTERVENTIONS'])} intervention, {len(DB['SUPPORTGRANTS'])} support grant (có grant bị revoke), {len(DB['EXTERNALREFERRALS'])} external referral "
      f"({sum(1 for r in DB['EXTERNALREFERRALS'] if r['isOverdue'])} quá hạn), {len(DB['ESCALATIONS'])} escalation.\n")
    w("**Consent (EX7, EX8)** - " +
      f"{sum(1 for c in CONSENTS if c['status'] == 'granted')} consent đang granted ({sum(1 for c in CONSENTS if c['status'] == 'granted' and c['expiresAt'] and c['expiresAt'] < NOW)} đã hết hạn theo expiresAt), "
      f"{sum(1 for c in CONSENTS if c['status'] == 'withdrawn')} đã rút. Consent request: " +
      ', '.join(f"{k} {v}" for k, v in sorted(defaultdict(int, {st: sum(1 for r in DB['CONSENTREQUESTS'] if r['status'] == st) for st in ('pending', 'granted', 'denied', 'withdrawn')}).items())) + ".")
    pr = [r for r in DB['CONSENTREQUESTS'] if r['status'] == 'pending']
    if pr:
        w(f"Sinh viên có request đang chờ trả lời: " + ', '.join(f"`{u(r['studentId'])}`" for r in pr[:4]) + ".")
    exp = [c for c in CONSENTS if c['status'] == 'granted' and c['expiresAt'] and c['expiresAt'] < NOW]
    if exp:
        w(f"Sinh viên có consent đã hết hạn: `{u(exp[0]['studentId'])}`.")
    w("")
    pe = [e for e in ERASURES if e['status'] == 'pending']
    blocked = [e for e in pe if any(c['status'] == 'granted' for c in BY_SA[e['_sa']])]
    ce = [e for e in ERASURES if e['status'] == 'completed']
    w(f"**Xoá mật mã (crypto erasure)** - Steward thấy {len(pe)} erasure request `pending` và {len(ce)} đã thực hiện "
      f"({sum(1 for k in KEYS.values() if k['status'] == 'destroyed')} key destroyed, {sum(1 for r in DATA_ROWS if r['erasedAt'])} giá trị đã bị xoá). "
      + (f"Request của `{u(blocked[0]['_sa'][0])}` ({ATTR_NAME[blocked[0]['_sa'][1]]}) **không thực hiện được** vì sinh viên đã chia sẻ lại thuộc tính này ở purpose khác (API phải trả 409). " if blocked else '')
      + (f"Sinh viên `{u(ce[0]['_sa'][0])}` có {ATTR_NAME[ce[0]['_sa'][1]]} hiện \"đã xoá\" trong hồ sơ của chính mình.\n" if ce else '\n'))
    act = [e for e in EMERGENCY if e['status'] == 'approved' and e['scopeExpiresAt'] > NOW]
    w("**Truy cập khẩn cấp (EX10)** - đang hiệu lực: " + ', '.join(f"`{u(e['requestedBy'])}` → `{u(e['studentId'])}` đến {e['scopeExpiresAt']:%d/%m/%Y %H:%M} UTC" for e in act) +
      f"; {sum(1 for e in EMERGENCY if e['status'] == 'pending')} request chờ Steward duyệt, có cả request đã hết hạn và bị từ chối. "
      "Sau ngày hết hạn, quyền tự mất (đúng thiết kế).\n")
    fl = [x for x in ACCESS if x['isFlagged']]
    w(f"**Nhật ký truy cập (EX9)** - {len(ACCESS)} dòng ACCESSLOGS (có dòng purpose = null của truy cập khẩn cấp), {len(fl)} dòng bị flag "
      f"({sum(1 for x in fl if x['status'] == 'unreviewed')} chưa review). Ví dụ sinh viên có dòng bị flag: `{u(fl[0]['recordedUserId'])}`.\n")
    w(f"**Import dữ liệu (WF5)** - 4 lần import: Spring 2026 (30/04), Summer 2026 (28/08), Fall 2026 tuần 3 (28/09), Fall 2026 giữa kỳ (26/10) + 1 lần import lỗi trong activity log. "
      f"{sum(1 for r in DATA_ROWS if r['isMissing'])} dòng `isMissing`, {sum(1 for r in DATA_ROWS if r['status'] == 'correction')} dòng `correction`. Bảng DATASOURCES / INGESTIONSCHEDULES / INGESTIONJOBS có job success, failed, running, pending.\n")
    w(f"**Thông báo, phiên đăng nhập, lịch sử hoạt động** - {len(DB['NOTIFICATIONS'])} notification (cả đã đọc và chưa đọc), {len(DB['SESSIONS'])} session "
      f"({sum(1 for x in DB['SESSIONS'] if x['isActive'])} còn active), {len(DB['ACTIVITYLOGS'])} activity log, {len(DB['NOTIFICATIONPREFERENCES'])} cài đặt kênh thông báo.\n")
    w("**Master data** - 6 khoa (JP archived), 8 chương trình, 5 kỳ (Fall 2025 → Spring 2027), 28 môn (2 archived), 13 support category "
      "(Thesis topic archived; Career orientation gắn purpose Career guidance chưa active nên sinh viên không chia sẻ dữ liệu được), 9 external service (1 inactive), 9 access policy (active/draft/revoked).\n")
    w("## Số dòng theo bảng\n")
    w("| Bảng | Số dòng |\n|---|---:|")
    for t in TABLE_ORDER:
        w(f"| {t} | {len(DB[t])} |")
    w("\n## Sinh lại dữ liệu\n")
    w("```\npip install cryptography\npython generate_mock_data.py                      # đọc master key từ ../../AdviseCare_API/AdviseCare.API/appsettings.json\n"
      "python generate_mock_data.py --master-key <base64> --master-key-version 1\n```")
    w("Seed ngẫu nhiên cố định nên nội dung, id và thời gian giống nhau mỗi lần sinh; chỉ phần mã hoá (nonce, data key) thay đổi.")
    with open(path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(out) + '\n')


if __name__ == '__main__':
    result = write_sql(ARGS.out)
    build_readme(os.path.join(os.path.dirname(os.path.abspath(ARGS.out)), 'README_MockData_vi.md'), os.path.basename(ARGS.out))
    print(f"Wrote {ARGS.out}")
    for t in TABLE_ORDER:
        print(f"  {t:<26}{result[t]:>7}")
    print(f"  cases {len(CASES)}, keys active {sum(1 for k in KEYS.values() if k['status'] == 'active')}, destroyed {sum(1 for k in KEYS.values() if k['status'] == 'destroyed')}")
