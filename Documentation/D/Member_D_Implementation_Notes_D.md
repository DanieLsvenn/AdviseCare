# Member D - Support requests, cases, matching and reports

Scope from `AdviseCare_Backend_Task_Division.txt`, Member D (D1-D6).
Tables owned: SUPPORTCATEGORIES, CASES, CASENOTES, CASESTAGEHISTORIES, REFERRALS, ADVISORASSIGNMENTS, ASSIGNMENTCHANGEREQUESTS.
No database change: everything runs on the current `AdviseCare_Database_Script.sql` (45 tables).

## 1. What was added

| Task | Endpoints | Service |
|---|---|---|
| D1 Support categories | `GET /api/SupportCategories`, `/search`, `/{id}`, `/{id}/shareable-attributes`, `POST`, `PUT /{id}` | `SupportCategoryService` |
| D2 Support requests | `POST /api/Cases` (student), `GET /api/Cases/me`, `GET /api/Cases/{id}`, `GET /api/CaseStageHistories/search?caseId=` | `CaseService` |
| D3 Matching | `GET /api/Cases/matching-queue`, `/{id}/suggested-advisors`, `PUT /{id}/assign`, `PUT /{id}/reassign` | `CaseService` |
| D4.1 Assigned list | `GET /api/Cases/assigned` (filters, sort, paging, unread count) | `CaseService` |
| D4.2 Outreach | `POST /api/Cases` with `studentId` (advisor) | `CaseService` |
| D4.3 Notes | `POST /api/CaseNotes`, `PUT /{id}`, `PUT /{id}/finalize`, `GET /{id}`, `GET /search?caseId=` | `CaseNoteService` |
| D4.4 Stages | `PUT /api/Cases/{id}/stage`, `PUT /{id}/reopen`, `PUT /{id}/priority` | `CaseService` |
| D4.5 Referrals | `POST /api/Referrals`, `GET /me/incoming`, `/me/outgoing`, `/search`, `/{id}`, `PUT /{id}/accept`, `PUT /{id}/return` | `ReferralService` |
| D5 Assignment changes | `GET /api/AdvisorAssignments/search`, `/me/students`, `/{id}`, `PUT /{id}/reassign`; `POST /api/AssignmentChangeRequests`, `GET /search`, `/{id}`, `PUT /{id}/approve`, `PUT /{id}/reject` | `AdvisorAssignmentService`, `AssignmentChangeRequestService` |
| D6 Reports | `GET /api/Reports/cases`, `/caseload-closure`, `/cases/export`, `/caseload-closure/export` (xlsx/csv/pdf) | `CaseReportService` |

Shared pieces:
- `CaseRules` - role names, stage/status/priority values, notification URLs (one place, exactly as in the DB CHECK constraints).
- `CaseWorkflowHelper` (scoped) - who can see a case, stage-history insert, "create or keep" assignment, advisor hand-over (EX6), case/assignment mapping.
- `IPdfWriter` + `SimplePdfWriter` (Infrastructure/Excel) - dependency-free PDF table writer for the report exports.
- `Program.cs` - one "Member D" registration block plus `IPdfWriter`.

Routes and bodies follow the proposals already written in `AdviseCare_API_Workflow_List.txt` (tags changed to `[EXISTS - Member D]`; extra endpoints listed at the end of that file).

## 2. Rules (to defend in review)

**Transactions.** Every multi-row change (case + stage history + assignment, referral + case, approve + revoke + new assignment + moved cases) is staged with `PrepareCreate/PrepareUpdate` and committed with one `SaveAsync`. A student's request and the consents for the shared attributes are saved in the same `SaveChanges` (the case is staged, then `IConsentService.CreateAsync` saves everything). `UpdateAsync` is avoided in these flows because it clears the change tracker.

**Case lifecycle.**
- Student request: stage `pending`, status `open`, priority `medium`, no advisor. One history row `pending -> pending` ("Support requested by the student"). Programme Head(s) notified.
- Advisor outreach: stage `assigned` with the advisor; only for students the advisor already has an active assignment with (EX3 step 1 "students assigned to them"). Student notified with the advisor's contact.
- Manual stages via `/stage`: `contacted`, `followUp`, `closed`. `assigned` only comes from matching, reopen or an accepted referral; `referred` only from a referral; never back to `pending`.
- Close sets `status = closed` + `closedAt`; reopen sets `status = reopened`, stage `assigned`, clears `closedAt`.
- Every stage change and every hand-over writes a CASESTAGEHISTORIES row (hand-overs use the same stage on both sides with the reason "Advisor changed from A to B: ...").
- A pending referral blocks stage changes and reassignment of that case.

**Visibility.**
- Case + stage history: the student, the current advisor, the Programme Head, and anyone who sent/received a referral of the case.
- Case notes: the same staff, but never the student. Drafts are private to their author; after finalize, no edits.
- Other people get 404 (not 403) so case ids cannot be probed.

**Matching suggestions** (D3.2): active advisors of the type's role, ordered by: familiar pair (already assigned to the student) -> subject specialist -> same faculty (general advisors first) -> fewest open cases. Each item lists `matchReasons`.

**Referrals.** Receiver may be an Academic Advisor, Wellbeing Advisor or Student Welfare Officer.
- Advisor accepts -> becomes the case's advisor, assignment created/kept, stage `referred -> assigned`, student gets the new contact.
- Welfare Officer accepts -> advisor keeps the case, stage stays `referred` (WF2b step 6: the case comes back to the advisor for follow-up).
- Return -> stage goes back to what it was before the referral; the reason goes to the stage history and the sender.

**Assignments (EX6).** Approve / direct reassign revokes the old row (`revoked`, `revokedAt`), creates or keeps the new pair, and moves the old pair's open cases whose support type the new advisor handles and that have no pending referral (`movedCaseIds` / `keptCaseIds` in the response). Student, old and new advisor are notified.

**Unread count** (D4.1). There is no "read" column on CASES, so it is computed from the advisor's unread NOTIFICATIONS whose `redirectUrl` is `/cases/{caseId}`. Opening `GET /api/Cases/{id}` marks them read.

**Reports.** Cases opened between `from` and `to` (inclusive UTC days). Caseload groups by the case's current advisor; average days to close uses closed cases only. Exports are generated on the fly, nothing is stored (REPORTS stays for the future "report history" item). CSV is UTF-8 with BOM so Excel shows Vietnamese names.

## 3. Points for the team

1. **Rejection reason of an assignment change request** has no column in ASSIGNMENTCHANGEREQUESTS. It is sent in the notifications and kept in ACTIVITYLOGS. If the reviewers want it on the row, add `decisionNotes NVARCHAR(MAX) NULL` (like CORRECTIONREQUESTS) and re-scaffold.
2. **PDF export** uses a small built-in writer (Courier, landscape A4) because the project has no PDF library. Standard PDF fonts cannot draw Vietnamese, so diacritics are removed in the PDF only (xlsx/csv keep them). Swapping in QuestPDF later only touches `SimplePdfWriter`.
3. **New activity type** `casePriorityChanged` (added to `AdviseCare_Activity_Types.txt`); all Member D types are now marked "(D, now)".
4. **Member C overlap**: Welfare Officer code (interventions, escalations) keeps working on CASES as before; the Welfare Officer can now also open the case, read finalized notes and change priority once a case is referred to them.
5. **Frontend routes**: notification `redirectUrl`s are `/cases/{id}`, `/referrals/{id}`, `/assignment-change-requests/{id}` (same style as Member C's `/escalations/{id}`); the web app needs to map them to its own pages.

## 4. How it was checked

- Full compile of Application + API controllers (no warnings in the new files).
- A scenario run of the real services (including Member B's real `ConsentService`) over an in-memory repository, following the API workflow file: EX11.4 -> WF2 -> WF2b -> EX3 -> EX4 -> EX6 -> WF4, with the negative cases listed there. 96/96 checks passed.
- PDF output opened with an independent parser (pypdf) and rendered to check layout.
- **Not yet done:** a run against a real SQL Server (`AdviseCareDB`) through Swagger/Postman. Please walk the `[EXISTS - Member D]` steps of `AdviseCare_API_Workflow_List.txt` once locally.
