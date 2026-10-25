# Member C Changes and Runtime Validation

**Updated:** 2026-10-25  
**Scope:** Backend only; Member C checkpoints through C-10. C-01, C-02, C-03, C-04, C-05, C-06a, C-06b1, C-06b2, C-07a, C-07b, C-07c, C-08, C-09, and C-10 have passed the runtime flows described below.
**Runtime target:** Local SQL Server database `AdviseCareDB` on `LAPTOP-B9R8VFQL`, API on `http://127.0.0.1:5167`, and local HashiCorp Vault Transit dev server on `http://127.0.0.1:8200`.

## Current State

- The v6 development SQL script has been applied. It is a destructive rebuild script; **do not rerun it** against a database whose data must be retained.
- Member C implementation checkpoints C-01 through C-10 are recorded in the workflow documents. C-01, C-02, C-03, C-04, C-05, C-06a, C-06b1, C-06b2, C-07a, C-07b, C-07c, C-08, C-09, and C-10 have passed the runtime flows described below.
- The latest startup/runtime fixes are in [AdviseCareContext.cs](../AdviseCare_API/AdviseCare.Domain/Models/AdviseCareContext.cs) and [VaultTransitKeyManagementService.cs](../AdviseCare_API/AdviseCare.Infrastructure/Security/VaultTransitKeyManagementService.cs).
- The temporary C-08 test fixture remains in the development database and the local Vault Transit mount. Do not carry this fixture, dev accounts, dev tokens, or a Vault dev server into production.
- C-01 was exercised using the current build on port 5168 without stopping the API process on port 5167. Three synthetic external-service rows covered active/inactive status, service types, ordering, paging, and search filters; all three rows were removed after testing.
- C-02 was exercised using the current build on port 5168 without stopping the API process on port 5167. Administrator create/update, normalization, persistence, and write-role boundaries passed; the created synthetic service was removed after testing.
- C-03 was exercised using the current build on port 5168 without stopping the API process on port 5167. Future- and past-due referrals, search filters, background persistence of the overdue flag, follow-up completion, and access/error boundaries passed. Both referrals and all supporting synthetic case/service/category/purpose rows were removed.
- C-04 was exercised using the current build on port 5168 without stopping the API process on port 5167. Welfare Officer/Programme Head create, search, response, party visibility, and notifications passed. Temporary email-disabled preferences prevented outbound test emails and were removed with the synthetic fixture.
- C-05 was exercised using the current build on port 5168 without stopping the API process on port 5167. Intervention creation/update, support-grant persistence, outcome/closure, closed-state guards, UTC normalization, and the assigned-advisor notification passed. The synthetic fixture and temporary email safeguard were removed.
- C-06a was exercised using the current build on port 5168. CSV and XLSX preview/validation, invalid-row reporting, format rejection, and admin-only access passed; database row counts remained unchanged, and the temporary files were removed.
- C-06b1 was exercised with the real encryption and Vault Transit services against an isolated ephemeral Vault on port 18200. Provisioning, DEK wrapping/unwrapping, AES-GCM/HMAC verification, payload binding, invalid-token rejection, and key deletion passed. No SQL writes occurred; the user's API and Vault on ports 5167 and 8200 were untouched.
- C-06b2 was exercised on a temporary API at port 5168 with an isolated Vault. An invalid mixed-row import wrote nothing; a valid import resolved studentCode and email identifiers, persisted two encrypted values and keys, provisioned a missing StudentProfile, created one missing-value row, and wrote an import log. All synthetic SQL rows, both Transit keys, and the import log were removed.
- C-07a was exercised on a temporary API at port 5168 with an isolated Vault. An unassigned administrator profile read returned 403 without an access log. The assigned Academic Advisor received the consented value decrypted while the non-consented value remained hidden; exactly one access log was written for the permitted value, and ciphertext/HMAC were absent from the response. All C-07a fixture data, its access log, and Transit keys were removed.
- C-07b was exercised on a temporary API at port 5168 with an isolated Vault. An as-of date before the first transaction returned no indicator; the historical date returned the original value, and comparison returned the original and corrected values in their respective snapshots. Missing authentication returned 401, an unassigned administrator was denied with 403, and three snapshot reads produced three access logs. Encrypted payload fields were absent from responses. All C-07b SQL fixtures, the Transit key, correction request, notifications, and test access logs were removed.
- C-07c was exercised on a temporary API at port 5168 with an isolated Vault. The student self-profile returned only the authenticated student's value and did not add access logs. The JSON export included the student's identity, profile, own case, and own access log, excluded a decoy student's value and identity, and recorded exactly one `dataExported` activity. Student-only authentication boundaries passed; all synthetic SQL rows, Transit keys, and export/import activity records were removed.
- C-09 was exercised using the current build on port 5168 without stopping the API process on port 5167. Its 9 synthetic student accounts/profiles, 11 cases, 2 categories, purpose, and term were removed after testing; one C-09 export activity record remains as audit evidence. C-10 was fully exercised using an isolated, ephemeral Vault on port 18200. Its synthetic SQL fixture, 36 consent grants, 36 attribute keys, and isolated Vault were removed; four C-10 export activity records remain as audit evidence. The C-08 fixture remains as described below.

## Member C Work Completed

### Feature checkpoints

- **C-01 to C-05:** External service catalogue reads/writes, external referrals and follow-up, welfare escalations, interventions, and support grants.
- **C-06a:** Spreadsheet/CSV preview, column mapping, and whole-file validation.
- **C-06b1/C-06b2:** Authenticated envelope encryption, per-student/attribute Vault Transit key provisioning, insert-only indicator persistence, missing-value rows, student profile provisioning, student-code resolution, and import activity logging. Both checkpoints passed isolated Vault/runtime validation.
- **C-07a/C-07b/C-07c:** Consent-authorized advisor profile reads, historical snapshots/comparison, student self-profile access, and JSON export with access/activity logging. All three passed runtime validation, including assigned-case authorization, consent-gated decryption, system-time snapshot selection, comparison, self-only data boundaries, and export auditing.
- **C-08:** Student correction submission/status/withdrawal, assigned-advisor review, approval/rejection, encrypted values, notifications, and insert-only correction history. Approval adds a correction version; it does not overwrite the prior indicator row.
- **C-09:** Welfare Officer-only anonymised case statistics grouped by support category. The query accepts one academic term or a complete UTC calendar-date range, filters by case opened date, suppresses categories with fewer than five distinct students, returns no student-level rows or overall total, and generates an on-demand CSV export with activity logging.
- **C-10:** Staff-only anonymised cohort indicator summaries. Requires a term, faculty, or both; uses the latest row per student/indicator/term; applies the active Cohort Analytics purpose, staff role policy, and each student's explicit consent before decryption; logs source-value reads; returns numeric means or categorical/boolean frequencies with a ten-student minimum per cell; and exports the same aggregate as CSV with activity logging. Runtime validation passed using 12 synthetic students and an isolated Vault: numeric mean 6.5 from 12 values, categorical `Supported` frequency 10 released, a two-student category suppressed, and boolean categories of nine and three both suppressed. Term-only, faculty-only, and combined filters returned the same approved aggregates; CSV output matched; 216 source-value access logs were written across six summary requests. The synthetic SQL data and Vault keys were removed after testing.

The main C-08 implementation files include [CorrectionRequestsController.cs](../AdviseCare_API/AdviseCare.API/Controllers/CorrectionRequestsController.cs), [CorrectionRequestService.cs](../AdviseCare_API/AdviseCare.Application/Services/CorrectionRequestService.cs), [ICorrectionRequestService.cs](../AdviseCare_API/AdviseCare.Application/Interfaces/Services/ICorrectionRequestService.cs), [Correctionrequest.cs](../AdviseCare_API/AdviseCare.Domain/Models/Correctionrequest.cs), and the request/response contracts under `AdviseCare_API/AdviseCare.Application/Contracts/`.

The C-09 API is exposed by [WelfareStatisticsController.cs](../AdviseCare_API/AdviseCare.API/Controllers/WelfareStatisticsController.cs), implemented in [WelfareStatisticsService.cs](../AdviseCare_API/AdviseCare.Application/Services/WelfareStatisticsService.cs), and documented with runnable request examples in [WelfareStatistics.http](../AdviseCare_API/AdviseCare.API/WelfareStatistics.http).

The C-10 API is exposed by [CohortIndicatorsController.cs](../AdviseCare_API/AdviseCare.API/Controllers/CohortIndicatorsController.cs), implemented in [CohortIndicatorService.cs](../AdviseCare_API/AdviseCare.Application/Services/CohortIndicatorService.cs), and documented with request examples in [CohortIndicators.http](../AdviseCare_API/AdviseCare.API/CohortIndicators.http). The shared [CsvExportWriter.cs](../AdviseCare_API/AdviseCare.Application/Services/CsvExportWriter.cs) encodes CSV values for both C-09 and C-10. The access guard changes in [AccessGuardService.cs](../AdviseCare_API/AdviseCare.Application/Services/AccessGuardService.cs) preserve the existing self-read behavior while allowing C-10's explicit-purpose bulk reads to log every authorized source value, including a staff accessor's own row.

No SQL schema rebuild is required for C-10. Before production use, configure an active consent purpose named exactly `Cohort Analytics`, include the intended attributes in that purpose, add active policies for the staff roles being enabled, and obtain explicit student consent for the selected purpose/attributes. The API does not create purpose data or grant consent. Faculty filtering uses the current `Studentprofile.ProgrammeId` to resolve `Programme.FacultyId`; historical programme/faculty membership is not stored.

C-10 negative consent/policy runtime validation also passed with ten synthetic students and an isolated API/Vault. The positive control released mean 5.5 (n=10). Removing one consent and withdrawing another each suppressed the cell; only nine permitted source values were logged in each case, with no log for the denied student's value. Revoking the Academic Advisor policy returned an empty summary with no source-read logs; deactivating the purpose returned 409 with no source-read logs. Temporary administrator/advisor identities and sessions used a test-only signing key; this focused check did not exercise the login flow. All temporary SQL rows, encrypted values, Transit keys, access logs, and token-bearing logs were removed. Existing C-10 export-audit rows were retained.

Other current Member C code and schema changes include:

- [Program.cs](../AdviseCare_API/AdviseCare.API/Program.cs), [StudentIndicatorImportService.cs](../AdviseCare_API/AdviseCare.Application/Services/StudentIndicatorImportService.cs), and [IndicatorValueValidator.cs](../AdviseCare_API/AdviseCare.Application/Services/IndicatorValueValidator.cs).
- [AdviseCareDB_Revise.sql](./AdviseCareDB_Revise.sql), [AdviseCare_ERD_Tables.txt](./AdviseCare_ERD_Tables.txt), [AdviseCare_Backend_Task_Division.txt](./AdviseCare_Backend_Task_Division.txt), [AdviseCare_Workflow_List.txt](./AdviseCare_Workflow_List.txt), and [AdviseCare_API_Workflow_List.txt](./AdviseCare_API_Workflow_List.txt).

### Runtime fixes

- Mapped `Studentprofile.StudentId` as the EF Core primary key so EF can build the model.
- Changed the fallback database setting to `ConnectionStrings:AdviseCareDB` and made `OnConfiguring` leave dependency-injected `DbContextOptions` unchanged.
- Vault Transit key creation now configures `deletion_allowed=true` with the separate key-configuration operation. If configuration fails, the service attempts to remove the just-created key and reports a cleanup failure instead of silently leaving it behind.

## Build and Local Run

For the complete Windows PowerShell runbook—including token handling, Transit setup, command-shell troubleshooting, and API startup syntax—see [Member C Team Handover and Vault Setup](./Member_C_Team_Handover_and_Vault_Setup.md).

**Current workspace safety:** the API on port `5167` and Vault on port `8200` are already running, and the development database/Vault retain the C-08 fixture. Do not restart or replace the Vault on port `8200`, delete its Transit keys, or rerun the destructive v6 SQL rebuild script. For new runtime checks, use a separate Vault port and preferably a separate development database; stop only the temporary processes you started.

## Runtime Validation Results

| Check | Result | Evidence |
|---|---|---|
| Backend solution build | PASS | Current C-10 solution build to a temporary output directory: 0 warnings and 0 errors. |
| API startup | PASS | Started on `http://127.0.0.1:5167` against SQL Server. |
| C-01 current-assembly runtime validation | PASS | Current build started on port 5168. List returned three services ordered by name; pages 1/2 returned the expected 2/1 rows; get-by-id matched. Name, type, active/inactive, combined, and no-match filters passed. Unknown ID returned 404, invalid pagination 400, anonymous access 401, and Student-role catalogue access 200. |
| C-01 test-fixture cleanup | PASS | Removed all three synthetic external-service rows; the temporary API on port 5168 was stopped and its artifacts removed. |
| C-02 current-assembly runtime validation | PASS | Current build started on port 5168. System Administrator create and update returned 200; trimming/empty optional-value normalization, active-by-default behavior, inactive update, and persisted read-back passed. Welfare Officer create/update returned 403; anonymous create returned 401; invalid create/update returned 400; unknown update returned 404. Rejected writes did not alter the saved record. |
| C-02 test-fixture cleanup | PASS | Removed the one synthetic catalogue service; the temporary API on port 5168 was stopped and its artifacts removed. |
| C-03 current-assembly runtime validation | PASS | Current build started on port 5168. Future-due referral returned sent/not-overdue; past-due referral returned sent/overdue. Case/service/status/overdue filters passed. Background processing persisted the overdue flag during the validation window. Follow-up trimmed and persisted the outcome, changed status to followedUp, cleared overdue, and a second update returned 409. Inactive service returned 400; unknown case/service returned 404; anonymous and Student-role reads returned 401 and 403. |
| C-03 test-fixture cleanup | PASS | Removed both referrals, the synthetic case, two services, category, and purpose. The temporary API on port 5168 was stopped and its artifacts removed. |
| C-04 current-assembly runtime validation | PASS | Current build started on port 5168. Welfare Officer creation returned pending with a trimmed reason; the assigned Programme Head and raising Welfare Officer could search/read it, while Admin/Student/anonymous requests were denied. Programme Head response changed status to responded, the Welfare Officer saw the response, both in-app notifications were available to their intended recipients, and a repeated response returned 409. Invalid response returned 400; unknown case/escalation returned 404; non-Programme-Head recipient returned 400. |
| C-04 test-fixture cleanup | PASS | Removed escalation, two generated notifications, synthetic case/category/purpose, and both temporary email-disabled preferences. No test emails were sent. The temporary API on port 5168 was stopped and its artifacts removed. |
| C-05 current-assembly runtime validation | PASS | Current build started on port 5168. Create/update, case/student/status search, support-grant creation/search, outcome, and closure passed. Offset dates were normalized to UTC; closed status was completed and its final outcome persisted. Assigned advisor received the in-app completion notification. Update/outcome/re-close/grant-after-close returned 409; invalid input returned 400, unknown case 404, and Student/anonymous access 403/401. |
| C-05 test-fixture cleanup | PASS | Removed the grant, intervention, synthetic case/category/purpose, advisor notification, and temporary email-disabled preference. No test email was sent. The temporary API on port 5168 was stopped and its artifacts removed. |
| C-06a current-assembly runtime validation | PASS | Current build started on port 5168. CSV preview returned headers and the first five of six rows; all six valid CSV rows and both valid XLSX rows passed validation. Invalid CSV rows for an unknown student and a nonnumeric value were reported with `validRowCount=0`. Unsupported extension returned 400; Student and anonymous preview requests returned 403/401. |
| C-06a no-write/cleanup verification | PASS | `STUDENTINDICATORS`, `ATTRIBUTEKEYS`, and `ACTIVITYLOGS` counts remained unchanged at 4/3/59 across preview and validation requests. The API and temporary CSV/XLSX fixtures were removed. The import/write endpoint was not called. |
| C-06b1 envelope-encryption runtime validation | PASS | A temporary probe compiled with 0 warnings/errors and exercised the actual encryption and Vault Transit services using an isolated ephemeral Vault on port 18200. Key provisioning/wrapping, Unicode round-trip, decrypt/HMAC verification, authenticated payload binding, rejection of a modified HMAC and invalid Vault token, deletion-enabled preflight, and key destruction all passed. |
| C-06b1 no-write/cleanup verification | PASS | The temporary probe, isolated Vault process, and token-bearing logs were removed. `STUDENTINDICATORS`, `ATTRIBUTEKEYS`, and `ACTIVITYLOGS` remained 4/3/59. The user's API on port 5167 and Vault on port 8200 were not stopped or modified. |
| C-06b2 import runtime validation | PASS | Current solution build passed with 0 warnings/errors. An invalid CSV containing one valid and one unknown-student row was rejected without creating indicator rows, keys, a StudentProfile, or an import log. A valid two-row CSV resolved one student by `studentCode` and one by email; it returned two inserted values and one missing row, persisted ciphertext/HMAC with two active wrapped-key references, created the previously absent profile, and wrote the `indicatorsImported` activity record. |
| C-06b2 fixture cleanup | PASS | Removed both synthetic accounts, their profiles and roles, both encrypted indicator rows plus the missing row, both SQL key records and Vault keys, the temporary term, and the import activity record. No C-06b2 records remain. Three signed-in/signed-out authentication audit entries from the test runs were retained; overall database counts are 4 indicators, 3 keys, 62 activity logs, 1 term, 7 accounts, and 1 student profile. |
| C-07a current-assembly runtime validation | PASS | Current solution build had 0 warnings and errors. With a temporary API on port 5168 and an isolated Vault on port 18200, an unassigned administrator profile read returned 403 and wrote no access log. The assigned Academic Advisor read returned the consented indicator value decrypted and withheld the non-consented value; exactly one access log was written for the permitted value. Ciphertext and HMAC were absent from the response. |
| C-07a fixture cleanup | PASS | Removed the synthetic student, profile, term, purpose, policy, consent, support category, case, imported indicators, Transit keys, import activity, and test access log. Four C-07a sign-in/sign-out authentication audit entries were retained. No C-07a fixtures remain; final counts are 4 indicators, 3 keys, 66 activity logs, 1 term, 7 accounts, 1 student profile, and 5 access logs. The temporary API, isolated Vault, logs, probe, and build output were removed; the user's services on ports 5167/8200 were left running. |
| C-07b point-in-time runtime validation | PASS | Backend build succeeded with 0 warnings and errors. A temporary encrypted import created the original value (4.25); the correction workflow appended a new version (9.5). The as-of query before the first transaction returned no rows; the historical snapshot returned 4.25; the comparison paired 4.25 and 9.5. Missing authentication returned 401 and an unassigned administrator returned 403. The historical read and the comparison's two snapshots each wrote one access log; ciphertext, HMAC, and raw indicator payload fields were absent from responses. |
| C-07b fixture cleanup | PASS | Removed the synthetic account/profile, attribute/indicator, term, purpose/policy/consent, support category/case/advisor assignment, correction request, imported/correction indicator versions, Vault Transit key, notification, and four test-purpose access logs. Synthetic-account authentication and import activity records were removed. Twelve sign-in/sign-out audit entries for the existing administrator/advisor accounts remain from the runtime attempts. No C-07b fixtures remain; final counts are 4 indicators, 3 keys, 5 access logs, 78 activity logs, 1 term, 7 accounts, and 1 student profile. The temporary API/Vault on ports 5168/18200, probe, logs, and build output were removed; the user's services on ports 5167/8200 remain running. |
| C-07c self-profile and JSON export runtime validation | PASS | Backend build succeeded with 0 warnings and errors. The Student-only self-profile returned its own encrypted-import value (6.75), with no access-log increase; anonymous and non-Student requests returned 401/403. The JSON export returned `application/json` with only the authenticated student's identity, profile, one own case, and one own advisor access-log entry. A second student's value (88.25), ID, and email were absent; ciphertext/HMAC fields were absent. Exactly one `dataExported` activity record was created. |
| C-07c fixture cleanup | PASS | Removed the synthetic account/profile, new attribute/indicator/term, purpose/policy/consent, support category/case/advisor assignment, both imported encrypted rows and Transit keys, the advisor access log, and test import/export activity records. Four sign-in/sign-out audit entries for the existing administrator/advisor accounts remain. No C-07c fixtures remain; final counts are 4 indicators, 3 keys, 5 access logs, 82 activity logs, 1 term, 7 accounts, and 1 student profile. The temporary API/Vault on ports 5168/18200, probe, logs, and build output were removed; the user's services on ports 5167/8200 remain running. |
| C-09 current-assembly runtime validation | PASS | Current build started on port 5168; term returned 7 cases from 6 students; inclusive date range returned 6 cases from 5 students. The 4-student category was suppressed in both. CSV matched the date-range aggregate. Missing filters, partial dates, reversed dates, and term/date combinations returned 400; unknown term returned 404; anonymous and Student-role requests returned 401 and 403. |
| C-09 test-fixture cleanup | PASS | Removed all 9 synthetic accounts/profiles, 11 cases, 2 categories, purpose, and term. One `welfareStatisticsExported` activity record remains as audit evidence. The temporary API on port 5168 was stopped and its artifacts removed. |
| C-10 runtime validation | PASS | Current build started on port 5168 with an isolated Vault on port 18200. With 12 synthetic students and 36 explicit test consents, term, faculty, and combined filters returned mean 6.5 (n=12) and `Supported` (n=10); a two-student category and boolean cells with counts 9 and 3 were suppressed. CSV matched the JSON aggregate. Six successful summaries wrote 216 per-value access logs; the export audit record was confirmed. Missing filters returned 400, anonymous access 401, and Student role 403. |
| C-10 test-fixture cleanup | PASS | All 12 synthetic accounts, profiles, consents, indicators, encrypted values, 36 attribute keys, purpose/policy rows, and the temporary term were removed. All 36 isolated Vault keys were destroyed and the temporary Vault/API processes were stopped. Four export activity records remain as audit evidence. |
| C-10 negative consent/policy runtime validation | PASS | Release publish and startup passed with a temporary API on port 5168 and isolated Vault on port 18200. The ten-consent positive control returned mean 5.5 (n=10) and wrote ten source-value access logs. Absent consent and withdrawn consent each returned an empty below-threshold summary, logged exactly nine permitted reads, and wrote no log for the denied student. A revoked Academic Advisor policy returned an empty summary with no source-value logs; an inactive purpose returned 409 with no source-value logs. Test-only administrator/advisor JWT/session fixtures were used; login behavior was not in scope. |
| C-10 negative test-fixture cleanup | PASS | Removed all ten synthetic students, test staff accounts/sessions, profiles, consents, indicator rows, ten SQL key records, purpose/policy/attribute/term rows, import activity, and all 28 test access logs; all ten isolated Vault keys were confirmed destroyed. Global counts returned to the pre-test baseline: 4 indicators, 3 keys, 5 access logs, 82 activity logs, 1 term, 7 accounts, and 1 student profile. The four pre-existing C-10 export activity records were retained. Temporary build output and token-bearing logs were deleted; the user's API/Vault on ports 5167/8200 remained on their original processes. |
| Swagger and auth boundary | PASS | Swagger returned HTTP 200; an unauthenticated correction search returned HTTP 401. |
| Database prerequisites | PASS | v6 source column, enabled/trusted composite FK, and filtered unique pending-request index were verified. |
| Vault Transit provisioning | PASS | Three encrypted imports succeeded after the key-configuration fix. |
| C-08 student flows | PASS | Submitted three requests; verified own status; withdrew one pending request. |
| C-08 advisor flows | PASS | Assigned advisor search returned authorized values and access logging; approved one request and rejected another. |
| Persistence and encryption | PASS | Final statuses were approved, rejected, and withdrawn. Approval retained the original indicator and inserted a correction version. All three correction requests had 30-byte current/proposed ciphertexts and 32-byte current/proposed HMACs. |
| Notifications | PASS | Approval and rejection generated two student notifications. |
| Automated integration suite | UNVERIFIED | No automated test project was found; validation was performed manually against the live local API, SQL Server, and Vault dev server. |

This is development runtime evidence, not a production-readiness sign-off. The API reported the `Production` environment because it was launched with `--no-launch-profile`, but the database and Vault were local development services.

### C-06b1 build and runtime status

- The temporary .NET probe compiled successfully with **0 warnings and 0 errors** and directly exercised the production C-06b1 encryption and Vault Transit services.
- An isolated ephemeral Vault on port 18200 accepted the valid token and rejected an invalid token. The service created a deletion-enabled Transit key, wrapped a 256-bit data key, encrypted and decrypted normalized Unicode text, and verified the derived HMAC. Decryption rejected both a different authenticated payload identifier and a modified HMAC.
- The test key was deleted and its absence confirmed. The isolated Vault, token-bearing logs, and temporary probe were removed. No database rows were written; the user's API on port 5167 and local Vault on port 8200 remained untouched.

### C-06b2 build and runtime status

- The current backend solution built successfully with **0 warnings and 0 errors** to a temporary output directory. A separate API instance on port 5168 used an isolated ephemeral Vault on port 18200; the API on port 5167 and Vault on port 8200 were left running.
- An invalid two-row CSV (one valid studentCode row and one unknown student) was rejected with errors and no SQL side effects. A valid two-row CSV resolved one student by `studentCode` and one by email; it inserted two new encrypted values, provisioned two active wrapped data keys, created the missing profile for the second student, added one missing-value row for the other active Student account, and recorded one import activity event.
- The import activity and all synthetic rows were removed, and both new Transit keys were destroyed. No test fixture remains. Authentication audit entries for two test sign-ins and one sign-out remain by design; the final database counts are 4 `STUDENTINDICATORS`, 3 `ATTRIBUTEKEYS`, 62 `ACTIVITYLOGS`, 1 `ACADEMICTERMS`, 7 `ACCOUNTS`, and 1 `STUDENTPROFILES`.

### C-06a build and runtime status

- Backend solution build passed with **0 warnings and 0 errors** using a temporary output directory.
- CSV preview returned the five-column header and first five of six sample rows. Whole-file validation accepted all six valid CSV rows and both valid XLSX rows. The invalid CSV reported both the unknown student and invalid numeric value and returned `validRowCount=0`; an unsupported file extension returned 400.
- A Student-role preview request returned 403 and an anonymous request returned 401. Database counts for `STUDENTINDICATORS`, `ATTRIBUTEKEYS`, and `ACTIVITYLOGS` remained unchanged at 4, 3, and 59 respectively. The import/write endpoint was intentionally not called.
- The current API on port 5168 was stopped; temporary CSV/XLSX files, logs, and build output were removed.

### C-05 build and runtime status

- Backend solution build passed with **0 warnings and 0 errors** using a temporary output directory; the existing API on port 5167 was left running and untouched.
- The Welfare Officer created and updated an intervention; trimmed notes and `+07:00` input dates were persisted with UTC normalization. Case, student, and status searches returned the record. A tuition grant was created with normalized description/type and UTC issue date, then returned by intervention-scoped search.
- Recording an outcome and closing the intervention succeeded. Closure set `isClosed=true` and `status=completed`, persisted the final outcome, and created an in-app notification visible to the assigned Academic Advisor. Temporary email-disabled preferences prevented external email and were removed afterward.
- Invalid type/grant/search inputs returned 400, unknown case returned 404, and Student/anonymous reads returned 403/401. After closure, update, outcome, repeat close, and grant creation all returned 409.
- The grant, intervention, synthetic case/category/purpose, advisor notification, and temporary preference were removed; the validation API and temporary files were cleaned up.

### C-04 build and runtime status

- Backend solution build passed with **0 warnings and 0 errors** using a temporary output directory; the existing API on port 5167 was left running and untouched.
- The Welfare Officer created a pending escalation with a trimmed reason. The assigned Programme Head and raising Welfare Officer could search and retrieve it. The Programme Head response was trimmed and persisted as `responded`; the Welfare Officer could then read the response, and a second response returned 409.
- The Programme Head received the creation notification and the Welfare Officer received the response notification through their own notification endpoints. Temporary email-disabled preferences were inserted for both recipients before testing so no external emails were sent; those safeguards were removed afterward.
- Admin/Student search and anonymous access returned 403/401; the Welfare Officer could not respond (403). A non-Programme-Head recipient returned 400, whitespace response returned 400, and unknown case/escalation returned 404.
- The escalation, generated notifications, synthetic case/category/purpose, and temporary notification preferences were removed; the validation API and temporary files were cleaned up.

### C-03 build and runtime status

- Backend solution build passed with **0 warnings and 0 errors** using a temporary output directory; the existing API on port 5167 was left running and untouched.
- The Welfare Officer created future- and past-due referrals. The first remained `sent` and not overdue; the past-due referral was immediately reported as overdue and the background process persisted its overdue flag within the validation window. Searches by case, service, status, and overdue state returned the expected records.
- Recording a follow-up trimmed and saved the outcome, changed the status to `followedUp`, and cleared the overdue flag; a second update returned 409. Referrals to an inactive service returned 400; unknown cases/services returned 404; anonymous and Student-role access returned 401 and 403.
- Both referrals and the synthetic case, two services, support category, and consent purpose were removed. The validation API and temporary files were cleaned up.

### C-02 build and runtime status

- Backend solution build passed with **0 warnings and 0 errors** using a temporary output directory; the existing API on port 5167 was left running and untouched.
- A System Administrator created a synthetic service and received HTTP 200 with a trimmed service name, service type, and contact value; the new service was active by default. Updating it returned HTTP 200, trimmed the new name, normalized blank optional fields to empty response values, changed the service to inactive, and persisted the update on read-back.
- Welfare Officer create and update attempts returned 403; anonymous create returned 401. A whitespace-only create name and an update missing `isActive` returned 400; updating an unknown ID returned 404. Rejected requests left the persisted service unchanged.
- The synthetic service was deleted, the validation API was stopped, and temporary logs/build output were removed.

### C-09 build and runtime status

- Backend solution build passed with **0 warnings and 0 errors** using a temporary output directory, leaving the existing API on port 5167 untouched.
- Runtime checks against the current assembly passed: term aggregation returned 7 cases from 6 distinct students; the inclusive 2026-09-10 through 2026-09-15 range returned 6 cases from 5 distinct students. The 4-student category was suppressed in both results. CSV output matched the range aggregate and wrote one `welfareStatisticsExported` activity record.
- Validation returned 400 for missing filters, a partial date range, reversed dates, and term combined with dates; an unknown term returned 404. Anonymous access returned 401 and a Student-role request returned 403. The API became ready on port 5168 and was stopped after validation.
- The 9-account/9-profile, 11-case, 2-category synthetic fixture, its purpose, and term were removed; one export activity record was retained as audit evidence. Temporary metadata, logs, and build output were removed.
- No automated test project was found. Manual API request examples remain in `AdviseCare_API/AdviseCare.API/WelfareStatistics.http`.

## Temporary Fixture and Cleanup

Fixture marker: `C08-RUNTIME-8dd625bac0`. Three original source indicator rows:

- Approve: `DDE76355-9B38-4A31-B4D9-C59093A42A5A`
- Reject: `32977B86-BF50-4227-AAA1-4E5CEE3B7CBE`
- Withdraw: `4056D2E9-63AE-40D9-8CDE-6B87ECB2C69A`

The fixture also created an approved correction version, three correction requests, encrypted attribute keys, consent/policy metadata, an advisor assignment, import/access logs, and two notifications. Cleanup has **not** been run. The following is a targeted, two-phase cleanup for this exact fixture only. Back up the development database first if you need to retain the evidence.

### Phase A — remove encrypted values and test activity from SQL

Run against `AdviseCareDB` only:

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DELETE FROM dbo.CORRECTIONREQUESTS
WHERE correctionRequestId IN
(
    'BC8A5478-3FD0-4DFB-917F-5BBB6B70301B',
    '84286369-FEA2-441C-9185-D032754CFAF7',
    '78F210B9-951C-46CE-B87E-F48E025C84AB'
);

DELETE FROM dbo.ACCESSLOGS
WHERE consentPurposeId = 'E043FD23-53C4-4B3C-AD2B-79A366C481A5';

DELETE FROM dbo.NOTIFICATIONS
WHERE notificationId IN
(
    '4C5AD605-4E7A-4EF8-8661-1AC54F466E7A',
    'AE2BD7C3-E907-4E60-A1CF-9419C06C60E1'
);

DELETE FROM dbo.ACTIVITYLOGS
WHERE activityLogId = '95B69D35-E632-4EE4-AEB5-9FF71BEDE192';

DELETE FROM dbo.STUDENTINDICATORS
WHERE studentId = 'A030F600-40D0-F111-B79A-C86E08512362'
  AND termId = '6E035E19-8792-46A2-B60A-23155A81C36A'
  AND indicatorId IN
  (
      '28962C0B-22F9-4178-88FC-B958AEB5343F',
      '584D144D-6EE7-4769-B3A9-07565FE6912E',
      '9E13179E-FBBA-475A-B263-7E4FA0FA3F8C'
  );

COMMIT TRANSACTION;
```

### Phase B — destroy only the fixture's three Vault Transit keys

Use the current local Vault dev server and list keys first:

```powershell
.\vault list transit/keys
.\vault delete transit/keys/advisecare-student-attribute-d62e087c839847d5bd985f385789e4a1
.\vault delete transit/keys/advisecare-student-attribute-44092fd28de84daabfc1d8414e395a1b
.\vault delete transit/keys/advisecare-student-attribute-5798fbfa12274b8b8d279925347051bb
```

If one of these keys is already absent, verify that exact key is absent and continue; do not delete any other key. These keys were configured as deletable by the API. Do not use a broad key-deletion command.

### Phase C — remove remaining fixture metadata from SQL

Run only after Phase B has succeeded or each exact key has been verified absent:

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DELETE FROM dbo.ATTRIBUTEKEYS
WHERE attributeKeyId IN
(
    'D62E087C-8398-47D5-BD98-5F385789E4A1',
    '44092FD2-8DE8-4DAA-BFC1-D8414E395A1B',
    '5798FBFA-1227-4B8B-8D27-9925347051BB'
);

DELETE FROM dbo.CONSENTS
WHERE consentPurposeId = 'E043FD23-53C4-4B3C-AD2B-79A366C481A5'
  AND studentId = 'A030F600-40D0-F111-B79A-C86E08512362'
  AND dataAttributeId IN
  (
      '33E7D7B4-044C-4BCE-A7A1-6E26F039678C',
      '70D13A05-4B5A-4A75-BBF3-C00BA397499E',
      'C4004E06-66D6-408D-B06A-C85BDB466FEB'
  );

DELETE FROM dbo.PURPOSE_ATTRIBUTE
WHERE consentPurposeId = 'E043FD23-53C4-4B3C-AD2B-79A366C481A5'
  AND dataAttributeId IN
  (
      '33E7D7B4-044C-4BCE-A7A1-6E26F039678C',
      '70D13A05-4B5A-4A75-BBF3-C00BA397499E',
      'C4004E06-66D6-408D-B06A-C85BDB466FEB'
  );

DELETE FROM dbo.ACCESSPOLICIES
WHERE accessPolicyId = '5FFFBD92-25FF-403E-B9BE-F302B84AF3D3';

DELETE FROM dbo.ADVISORASSIGNMENTS
WHERE advisorAssignmentId = 'E0E099C5-9BDF-49E1-8C76-E0A9FA8C46E5';

DELETE FROM dbo.INDICATORS
WHERE indicatorId IN
(
    '28962C0B-22F9-4178-88FC-B958AEB5343F',
    '584D144D-6EE7-4769-B3A9-07565FE6912E',
    '9E13179E-FBBA-475A-B263-7E4FA0FA3F8C'
);

DELETE FROM dbo.DATAATTRIBUTES
WHERE dataAttributeId IN
(
    '33E7D7B4-044C-4BCE-A7A1-6E26F039678C',
    '70D13A05-4B5A-4A75-BBF3-C00BA397499E',
    'C4004E06-66D6-408D-B06A-C85BDB466FEB'
);

DELETE FROM dbo.ACADEMICTERMS
WHERE termId = '6E035E19-8792-46A2-B60A-23155A81C36A';

DELETE FROM dbo.CONSENTPURPOSES
WHERE consentPurposeId = 'E043FD23-53C4-4B3C-AD2B-79A366C481A5';

COMMIT TRANSACTION;
```

The cleanup deliberately leaves the seeded accounts and their authentication/session records untouched. If this database will be used beyond local development, remove or rotate all development accounts and secrets through the project's approved administration process. Do not execute this fixture cleanup against a different database.

## Production Readiness Notes

- Replace Vault dev mode with a durable, sealed Vault deployment using TLS, restricted network access, and a least-privilege service identity. Never run the API with a Vault Root Token.
- Use durable key storage and operationally tested backup/recovery procedures. Keep Transit keys aligned with the database's per-attribute key references.
- Configure the SQL connection, JWT signing key, Vault credentials, and other secrets through a production secret manager; do not commit secrets or rely on development defaults.
- Remove development seed accounts and test fixture data. Rotate any credentials that were exposed in shell history or shared terminals.
- Review API environment settings, Swagger exposure, HTTPS, CORS, logging, database least privilege, backups, and deployment-specific health checks.
- Treat the v6 SQL file as a local bootstrap/rebuild script, not a production migration. Use a reviewed, backed-up deployment migration strategy for any existing production database.

## Related Workflow Records

- [AdviseCare_Workflow_List.txt](./AdviseCare_Workflow_List.txt)
- [AdviseCare_API_Workflow_List.txt](./AdviseCare_API_Workflow_List.txt)
- [AdviseCare_Backend_Task_Division.txt](./AdviseCare_Backend_Task_Division.txt)
