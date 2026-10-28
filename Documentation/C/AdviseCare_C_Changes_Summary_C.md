# Member C – documentation changes, separated from the official A/B docs (28/10/2026)

## What was done

| Step | Detail |
|---|---|
| Compared | Every non-code file Member C (Nguyen Nhu Thanh) changed. Range: `eea05f1 External Services read API` → `2ab7f75 Updated Documentations for Vault`, measured against our base `642c6e6 Add steward part B and fix some documentations`. C's `0e0a161 Merge branch 'binhnkse182311_stewardAPI'` brought **our** A/B work into C's branch, so it is not counted as C's change. |
| Separated | C's **complete branch version** (`2ab7f75`) of every changed doc was copied to `Documentation\C\` with the suffix `_C`, the same way C kept its own database in `AdviseCareDB_Revise.sql`. C's three new files were moved there and renamed `_C` too. |
| Reverted | The 7 official docs in `Documentation\` were restored to **our** latest version, `6dd72bf Add attribute-key encryption (B4.1 …)`. Checked with `git diff 6dd72bf -- Documentation`: no difference left. |
| Not touched | All .NET code (as asked), `AdviseCare_Activity_Types.txt` (C never changed it), the .docx files, README, web app. |

**Note.** The `_C` files are C's view of the whole document. They contain our A/B content **as it was at `642c6e6`** plus C's changes. Our later B4.1 changes (`6dd72bf`: rejectionReason, nullable emergency purpose, master-key encryption) are **not** in them, because C never had that commit. To see only C's own edits for one file, run:

```
git diff 642c6e6 2ab7f75 -- Documentation/<file>
```

## Files in `Documentation\C\`

| File | Origin | Lines C changed vs. our base | C commits that touched it |
|---|---|---|---|
| `AdviseCareDB_Revise_C.sql` | New file by C (was `AdviseCareDB_Revise.sql`) | +1463 (new) | 150c1df, b5fe4a8, 274023e |
| `AdviseCare_Database_Script_C.sql` | C's version of `AdviseCare_Database_Script.sql` | +50 / −7 | 150c1df |
| `AdviseCare_ERD_Tables_C.txt` | C's version of `AdviseCare_ERD_Tables.txt` | +79 / −35 | 150c1df, b5fe4a8, 274023e |
| `AdviseCare_Functional_Requirements_C.txt` | C's version | +21 / −9 | 150c1df, 274023e, 2ab7f75 |
| `AdviseCare_Workflow_List_C.txt` | C's version | +37 / −16 | eea05f1, 134328d, 202c796, 0a33684, 1a996ad, 9f10c0b, 150c1df, b5fe4a8, 274023e, 2ab7f75 |
| `AdviseCare_API_Workflow_List_C.txt` | C's version | +143 / −66 | eea05f1, 134328d, 202c796, 0a33684, 1a996ad, 9f10c0b, 150c1df, b5fe4a8, 274023e, 2ab7f75 |
| `AdviseCare_Backend_Task_Division_C.txt` | C's version | +20 / −11 | 150c1df, b5fe4a8, 274023e, 2ab7f75 |
| `AdviseCare_Future_List_C.txt` | C's version | +0 / −3 | 274023e, 2ab7f75 |
| `Member_C_Changes_and_Runtime_Validation_C.md` | New file by C (was `Member_C_Changes_and_Runtime_Validation.md`) | +313 (new) | 274023e, 2ab7f75 |
| `Member_C_Team_Handover_and_Vault_Setup_C.md` | New file by C (was `Member_C_Team_Handover_and_Vault_Setup.md`) | +231 (new) | 2ab7f75 |

## What C changed, file by file (compared with our official docs)

### AdviseCareDB_Revise_C.sql (new, C's own full database, "v6", 47 tables)

- **Everything in C's Database_Script below**, plus:
  - new tables `CASETEAMMEMBERS`, `CASENOTEREFERRALSHARES`;
  - `PROGRAMMES.programmeHeadId`;
  - visibility columns on 11 tables.
- **Logic in SQL:**
  - triggers: Student role for profiles, Programme Head role, protected roles, CASENOTES write checks;
  - procedures `usp_ExecuteErasure` and `usp_ConfirmKmsKeyDestruction`;
  - view `VW_CASETEAM`;
  - row-level security on CASENOTES.
- **Seed:** faculty FIT, programme SE (headed by the test Programme Head), and a profile for the test student (SV000001).
- **Missing compared with ours:** `ACCESSLOGS.consentPurposeId` is NOT NULL again; `EMERGENCYACCESSREQUESTS.rejectionReason` is absent.

### AdviseCare_Database_Script_C.sql

- **New table:** `45. STUDENTPROFILES` (studentCode, programmeId, academicStatus), with a unique index on studentCode.
- **STUDENTINDICATORS:**
  - `attributeKeyId` + composite FK to ATTRIBUTEKEYS;
  - `indicatorValue` changed to `VARBINARY(MAX)` (nullable);
  - CHECK "missing row ⇔ no key and no value";
  - extra index;
  - drop order moved.
- **CASES:** `requestReasonVisibility` (assignedAdvisor/caseTeam).
- **CASENOTES:** `visibility` (assignedAdvisor/caseTeam).
- **ATTRIBUTEKEYS:**
  - `wrappedKey` nullable;
  - unique (attributeKeyId, studentId);
  - CHECK tying status to wrappedKey/destroyedAt.
- `SET QUOTED_IDENTIFIER ON` before the indexes; header changed to 45 tables.

### AdviseCare_ERD_Tables_C.txt

- **New table:** `45. STUDENTPROFILES`. Every `studentId` relationship moved from `1 ACCOUNTS` to `45 STUDENTPROFILES`: STUDENTINDICATORS, ADVISORASSIGNMENTS, CASES, CORRECTIONREQUESTS, CONSENTS, CONSENTREQUESTS, EMERGENCYACCESSREQUESTS, ATTRIBUTEKEYS.
- **STUDENTINDICATORS:** + dataAttributeId, attributeKeyId, valueHmac, erasedAt; `indicatorValue` changed to varbinary.
- **CORRECTIONREQUESTS:**
  - + studentIndicatorId, dataAttributeId, attributeKeyId;
  - encrypted current/proposed values + HMACs;
  - textVisibility;
  - status + `withdrawn`.
- **CASES / CASENOTES:** visibility columns.
- **ERASUREREQUESTS:** + studentId, dataAttributeId (composite FKs); status + `pendingKeyDestruction`.
- **ATTRIBUTEKEYS:** + keyManagementKeyId (Vault key name); status + `pendingKmsDestruction`.
- **NOTES:**
  - new bullets "Indicator erasure" (two-phase Vault) and "Narrative visibility";
  - Programme Head bullet now says the programme is stored in STUDENTPROFILES.

### AdviseCare_Functional_Requirements_C.txt

- **Advisor:**
  - "Mark the case reason visible to the assigned advisor only or the authorized case team";
  - the same for case notes.
- **Welfare statistics:** no longer "pending review". Detailed rules: term or UTC date range, under 5 students suppressed, no totals, CSV export.
- **Student:** new section "Maintain student identity and programme details" (studentCode, programme, academic status; import matching by code/email/username).
- **Correction request:** no longer "pending review".
- **Cohort indicators:** no longer "pending review". Detailed rules: Cohort Analytics purpose, under 10 suppressed, mean/frequency, CSV.

### AdviseCare_Workflow_List_C.txt

- **WF2:**
  - 2.3.1 reason visibility;
  - step 7 note visibility.
- **WF2b:**
  - 3.4 review intervention history;
  - step 4 split into catalogue read (4.1), Admin manages catalogue (4.2), referral (4.3), overdue (4.4), follow-up (4.5);
  - step 6 notifies the assigned advisor.
- **WF5:**
  - .xlsx/.csv only (Google Sheet pending);
  - preview/validate without saving;
  - missing rows for every active student per indicator/term;
  - activity log in the same save;
  - checkpoint status note.
- **EX8 4.3:** two-phase Vault erasure.
- **EX13:** as-of = latest row recorded through the end of the date, with consent checks and logging.
- **EX14:** own profile + JSON export details.
- **EX15:** no longer "pending review".
- **New EX17:** anonymised welfare statistics.
- **New EX18:** anonymised cohort indicators.

### AdviseCare_API_Workflow_List_C.txt

- **All of C's routes:**
  - changed from `[PLANNED - Member C]` to `[IMPLEMENTED - Member C C-xx]`;
  - real bodies/expectations;
  - "Runtime validation" notes.
- **Routes covered:**
  - ExternalServices (read, Admin create/update);
  - ExternalReferrals (create, search, follow-up);
  - Interventions (create, update, search, outcome, close);
  - SupportGrants (create, search);
  - Escalations (create, search, respond, get);
  - StudentIndicators (preview, validate, import, profile, as-of, compare, me/profile, me/export);
  - CorrectionRequests (create, me/{id}, search, approve, reject, withdraw);
  - WelfareStatistics and CohortIndicators (summary + export).
- **Other changes:**
  - run order + EX17, EX18;
  - cohort test prerequisite;
  - EX8 erasure expectations for Vault.

### AdviseCare_Backend_Task_Division_C.txt

- **A2.6 (added to Member A):** maintain STUDENTPROFILES.
- **B4.1/B4.2 rewritten for Vault:**
  - a Vault Transit key per student/attribute, `deletion_allowed`;
  - `usp_ExecuteErasure` → delete Vault key → `usp_ConfirmKmsKeyDestruction`.
- **C1–C2:** rewritten with checkpoint codes (C-06…C-08) and implementation details.
- **C3.5:** implemented (C-09).
- **New C4:** cohort indicator analytics (C-10).
- **D4.3a (added to Member D):** enforce visibility.

### AdviseCare_Future_List_C.txt

- Removed from "NOT FUTURE, BUT PENDING REVIEW": Correction requests, Anonymised welfare statistics, Anonymised cohort indicators.

### Member_C_Changes_and_Runtime_Validation_C.md (new)

- C's evidence log for checkpoints C-01 … C-10: what was tested, against which local DB/API/Vault, and cleanup notes.
- Contains machine-specific details (computer name, ports).

### Member_C_Team_Handover_and_Vault_Setup_C.md (new)

- Team briefing: what C delivered, concepts (authorization vs. permission, envelope encryption with Vault, missing/erased/corrected states, audit vs. anonymisation), runtime evidence summary.
- Local Vault setup in PowerShell and common setup errors.
- Contains machine-specific paths (`D:\Office\vault.exe`, `D:\SchoolWork\…`).

## Things to know after this separation

- **Official docs are A/B only again**, so our docs no longer describe several things C's code relies on: STUDENTPROFILES, the binary/Vault indicator columns, C's routes as built, C4 cohort analytics.
- **The code still expects C's database.** C's .NET code on this branch (left untouched, as asked) expects C's schema. Until we decide which parts of C to adopt (review Q1/Q2/Q14), build a test database from `C\AdviseCareDB_Revise_C.sql` to run C's features, and from `AdviseCare_Database_Script.sql` for A/B.
- **Nothing is committed yet.** `git status` shows the 7 docs as modified (reverted), C's 3 original files as deleted (moved), and `Documentation\C\` as new.
