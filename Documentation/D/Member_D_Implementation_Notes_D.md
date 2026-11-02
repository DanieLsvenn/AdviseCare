# Member D - Support requests, cases, matching and reports

Scope from `AdviseCare_Backend_Task_Division.txt`, Member D (D1-D6).
Tables owned: SUPPORTCATEGORIES, CASES, CASENOTES, CASESTAGEHISTORIES, REFERRALS, CASECHANGEREQUESTS.
Runs on the current `AdviseCare_Database_Script.sql` (43 tables). Member D added one column: `CASECHANGEREQUESTS.decisionNotes` (see section 3).

## 1. What was added

| Task | Endpoints | Service |
|---|---|---|
| D1 Support categories | `GET /api/SupportCategories`, `/search`, `/{id}`, `/{id}/shareable-attributes`, `POST`, `PUT /{id}` | `SupportCategoryService` |
| D2 Support requests | `POST /api/Cases` (student), `GET /api/Cases/me`, `GET /api/Cases/{id}`, `GET /api/CaseStageHistories/search?caseId=` | `CaseService` |
| D3 Matching | `GET /api/Cases/matching-queue`, `/search`, `/{id}/suggested-advisors`, `PUT /{id}/assign`, `PUT /{id}/reassign` | `CaseService` |
| D4.1 Assigned list | `GET /api/Cases/assigned` (filters, sort, paging, unread count) | `CaseService` |
| D4.2 Outreach | `POST /api/Cases` with `studentId` (advisor) | `CaseService` |
| D4.3 Notes | `POST /api/CaseNotes`, `PUT /{id}`, `PUT /{id}/finalize`, `GET /{id}`, `GET /search?caseId=` | `CaseNoteService` |
| D4.4 Stages | `PUT /api/Cases/{id}/stage`, `PUT /{id}/reopen`, `PUT /{id}/priority` | `CaseService` |
| D4.5 Referrals | `POST /api/Referrals`, `GET /me/incoming`, `/me/outgoing`, `/search`, `/{id}`, `PUT /{id}/accept`, `PUT /{id}/return` | `ReferralService` |
| D5 Case change requests | `POST /api/CaseChangeRequests`, `GET /search`, `/{id}`, `PUT /{id}/approve`, `PUT /{id}/reject` | `CaseChangeRequestService` |
| D6 Reports | `GET /api/Reports/cases`, `/caseload-closure`, `/cases/export`, `/caseload-closure/export` (xlsx/csv/pdf) | `CaseReportService` |

Shared pieces:
- `CaseRules` - role names, stage/status/priority values, notification URLs (exactly the strings of the DB CHECK constraints).
- `CaseWorkflowHelper` (scoped) - who can see a case, stage-history insert, advisor hand-over, case mapping.
- `IPdfWriter` + `SimplePdfWriter` (Infrastructure/Excel) - dependency-free PDF table writer for the report exports.
- `Program.cs` - one "Member D" registration block plus `IPdfWriter`.

## 2. Rules (to defend in review)

**Advisor-student link.** There is no assignment table: `CASES.advisorId` is the only link (same rule as Member C's correction requests). An advisor "advises" a student while holding one of their cases that is not closed.

**Transactions.** Every multi-row change (case + stage history, referral + case, approved change request + case + history) is staged with `PrepareCreate/PrepareUpdate` and committed with one `SaveAsync`. A student's request and the consents for the shared attributes are saved in the same `SaveChanges` (the case is staged, then `IConsentService.CreateAsync` saves everything). `UpdateAsync` is avoided in these flows because it clears the change tracker.

**Case lifecycle.**
- Student request: stage `pending`, status `open`, priority `medium`, no advisor. One history row `pending -> pending`. Programme Head(s) notified.
- Advisor outreach (EX3): any active student, category of the advisor's own type; stage `assigned` with the advisor; the student is notified with the advisor's contact.
- Manual stages via `/stage`: `contacted`, `followUp`, `closed`. `assigned` only comes from matching, reopen or an accepted referral; `referred` only from a referral; never back to `pending`.
- Close sets `status = closed` + `closedAt`; reopen sets `status = reopened`, stage `assigned`, clears `closedAt`.
- Every stage change and every hand-over writes a CASESTAGEHISTORIES row (hand-overs keep the stage: "Advisor changed from A to B: ...").
- A pending referral blocks stage changes, reassignment and approving a change request for that case.

**Visibility.**
- Case + stage history: the student, the current advisor, the Programme Head, and anyone who sent/received a referral of the case.
- Case notes: the same staff, never the student. Drafts are private to their author; after finalize, no edits.
- Other people get 404 (not 403) so ids cannot be probed.

**Matching suggestions** (D3.2): active advisors of the type's role, ordered by: already handling another open case of the student -> subject specialist -> same faculty (general advisors first) -> fewest open cases. Each item lists `matchReasons`.

**Referrals.** Receiver may be an Academic Advisor, Wellbeing Advisor or Student Welfare Officer.
- Advisor accepts -> becomes the case's advisor, stage `referred -> assigned`, the student gets the new contact.
- Welfare Officer accepts -> advisor keeps the case, stage stays `referred` (WF2b step 6).
- Return -> stage goes back to what it was before the referral; the reason goes to the stage history and the sender.

**Case change requests (EX6).** Made by the case's student or current advisor; proposed advisor must be active and of the case's type; one pending request per case; `programmeHeadId` stays null until a Programme Head decides. Approve -> `CASES.advisorId` = proposed advisor (stage kept, history row), optional `decisionNotes`. Reject -> reason stored in `decisionNotes`. Student and both advisors are notified either way.

**Unread count** (D4.1). Computed from the advisor's unread NOTIFICATIONS whose `redirectUrl` is `/cases/{caseId}`. Opening `GET /api/Cases/{id}` marks them read.

**Reports.** Cases opened between `from` and `to` (inclusive UTC days). Caseload groups by the case's current advisor; average days to close uses closed cases only. Exports are generated on the fly, nothing is stored. CSV is UTF-8 with BOM so Excel shows Vietnamese names.

## 3. Points for the team

1. **New column `CASECHANGEREQUESTS.decisionNotes`** (NVARCHAR(MAX) NULL) with `CK_CASECHANGEREQUESTS_decisionNotes`: a rejected row must have a reason. Added to the script (including the mock rejected row), the ERD and the `Casechangerequest` entity + `AdviseCareContext` mapping by hand, in the same form EF Core Power Tools generates. Re-run the script once.
2. **PDF export** uses a small built-in writer (Courier, landscape A4) because the project has no PDF library. Standard PDF fonts cannot draw Vietnamese, so diacritics are removed in the PDF only (xlsx/csv keep them).
3. **Activity types**: `caseChangeRequested`, `caseChangeDecided` and the other Member D types are now marked "(D, now)"; new `casePriorityChanged`.
4. **Frontend routes**: notification `redirectUrl`s are `/cases/{id}`, `/referrals/{id}`, `/case-change-requests/{id}`.
5. The open questions of `AdviseCare_API_Workflow_List.txt` that Member D owned are answered in its NOTES section.

## 4. How it was checked

- Full compile of Application + API controllers + the Infrastructure encryption/CSV code (no warnings in Member D files).
- Every entity mapping in `AdviseCareContext` (all 43 tables, all members) compared column by column, type and nullability with the script: all match.
- A scenario run of the real services (including Member B's real `ConsentService`) over an in-memory repository, following the API workflow file: EX11.4 -> WF2 -> WF2b -> EX3 -> EX4 -> EX6 -> WF4, with the negative cases. 99/99 checks passed.
- **Not yet done:** a run against a real SQL Server through Swagger/Postman. Please walk the `[EXISTS - Member D]` steps once locally.
