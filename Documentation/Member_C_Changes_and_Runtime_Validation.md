# Member C Changes and Runtime Validation

**Updated:** 2026-10-25  
**Scope:** Backend only; Member C checkpoints through C-09.  
**Runtime target:** Local SQL Server database `AdviseCareDB` on `LAPTOP-B9R8VFQL`, API on `http://127.0.0.1:5167`, and local HashiCorp Vault Transit dev server on `http://127.0.0.1:8200`.

## Current State

- The v6 development SQL script has been applied. It is a destructive rebuild script; **do not rerun it** against a database whose data must be retained.
- Member C implementation checkpoints C-01 through C-09 are recorded as implemented in the workflow documents. C-08 has additionally passed the manual runtime flow described below.
- The latest startup/runtime fixes are in [AdviseCareContext.cs](../AdviseCare_API/AdviseCare.Domain/Models/AdviseCareContext.cs) and [VaultTransitKeyManagementService.cs](../AdviseCare_API/AdviseCare.Infrastructure/Security/VaultTransitKeyManagementService.cs).
- The temporary C-08 test fixture remains in the development database and the local Vault Transit mount. Do not carry this fixture, dev accounts, dev tokens, or a Vault dev server into production.
- C-09 was started after approval and is implemented below. The next Member C checkpoint has not started; request approval before beginning another task.

## Member C Work Completed

### Feature checkpoints

- **C-01 to C-05:** External service catalogue reads/writes, external referrals and follow-up, welfare escalations, interventions, and support grants.
- **C-06a:** Spreadsheet/CSV preview, column mapping, and whole-file validation.
- **C-06b1/C-06b2:** Authenticated envelope encryption, per-student/attribute Vault Transit key provisioning, insert-only indicator persistence, missing-value rows, student profile provisioning, student-code resolution, and import activity logging.
- **C-07a/C-07b/C-07c:** Consent-authorized advisor profile reads, historical snapshots/comparison, student self-profile access, and JSON export with access/activity logging.
- **C-08:** Student correction submission/status/withdrawal, assigned-advisor review, approval/rejection, encrypted values, notifications, and insert-only correction history. Approval adds a correction version; it does not overwrite the prior indicator row.
- **C-09:** Welfare Officer-only anonymised case statistics grouped by support category. The query accepts one academic term or a complete UTC calendar-date range, filters by case opened date, suppresses categories with fewer than five distinct students, returns no student-level rows or overall total, and generates an on-demand CSV export with activity logging.

The main C-08 implementation files include [CorrectionRequestsController.cs](../AdviseCare_API/AdviseCare.API/Controllers/CorrectionRequestsController.cs), [CorrectionRequestService.cs](../AdviseCare_API/AdviseCare.Application/Services/CorrectionRequestService.cs), [ICorrectionRequestService.cs](../AdviseCare_API/AdviseCare.Application/Interfaces/Services/ICorrectionRequestService.cs), [Correctionrequest.cs](../AdviseCare_API/AdviseCare.Domain/Models/Correctionrequest.cs), and the request/response contracts under `AdviseCare_API/AdviseCare.Application/Contracts/`.

The C-09 API is exposed by [WelfareStatisticsController.cs](../AdviseCare_API/AdviseCare.API/Controllers/WelfareStatisticsController.cs), implemented in [WelfareStatisticsService.cs](../AdviseCare_API/AdviseCare.Application/Services/WelfareStatisticsService.cs), and documented with runnable request examples in [WelfareStatistics.http](../AdviseCare_API/AdviseCare.API/WelfareStatistics.http).

Other current Member C code and schema changes include:

- [Program.cs](../AdviseCare_API/AdviseCare.API/Program.cs), [StudentIndicatorImportService.cs](../AdviseCare_API/AdviseCare.Application/Services/StudentIndicatorImportService.cs), and [IndicatorValueValidator.cs](../AdviseCare_API/AdviseCare.Application/Services/IndicatorValueValidator.cs).
- [AdviseCareDB_Revise.sql](./AdviseCareDB_Revise.sql), [AdviseCare_ERD_Tables.txt](./AdviseCare_ERD_Tables.txt), [AdviseCare_Backend_Task_Division.txt](./AdviseCare_Backend_Task_Division.txt), [AdviseCare_Workflow_List.txt](./AdviseCare_Workflow_List.txt), and [AdviseCare_API_Workflow_List.txt](./AdviseCare_API_Workflow_List.txt).

### Runtime fixes

- Mapped `Studentprofile.StudentId` as the EF Core primary key so EF can build the model.
- Changed the fallback database setting to `ConnectionStrings:AdviseCareDB` and made `OnConfiguring` leave dependency-injected `DbContextOptions` unchanged.
- Vault Transit key creation now configures `deletion_allowed=true` with the separate key-configuration operation. If configuration fails, the service attempts to remove the just-created key and reports a cleanup failure instead of silently leaving it behind.

## Build and Local Run

Run these steps in PowerShell. Keep development credentials in the current process only; do not paste tokens into this document, source files, or command history.

1. Start the local Vault development server using the installed Vault binary. Dev mode is ephemeral and is only suitable for local testing:

   ```powershell
   .\vault server -dev -dev-listen-address=127.0.0.1:8200
   ```

   Keep that terminal open. In another PowerShell terminal, point the CLI at the HTTP dev listener and enable Transit once for this Vault instance:

   ```powershell
   $env:VAULT_ADDR = 'http://127.0.0.1:8200'
   .\vault secrets enable -path=transit transit
   .\vault secrets list -detailed
   ```

   If Transit is already listed, do not enable it again. If `vault` is on `PATH`, the `.\` prefix is unnecessary; if the binary is in another directory, use its actual path.

2. In the PowerShell window that will run the API, set the same `VAULT_ADDR`, securely enter the current dev Root Token, and pass it to the API process:

   ```powershell
   $env:VAULT_ADDR = 'http://127.0.0.1:8200'
   $secureToken = Read-Host 'Paste the Vault dev Root Token' -AsSecureString
   $token = [System.Net.NetworkCredential]::new('', $secureToken).Password
   $env:VAULT_TOKEN = $token
   $env:VaultTransit__Token = $token
   $secureToken.Dispose()
   Remove-Variable token
   ```

   `VAULT_TOKEN` is for the Vault CLI; `VaultTransit__Token` is the API configuration key. Do not use an Unseal Key as the token. The Vault dev Root Token is temporary and changes when the dev server is restarted.

3. Build and start the API from the repository root:

   ```powershell
   dotnet build .\AdviseCare_API\AdviseCare_API.sln
   Set-Location .\AdviseCare_API\AdviseCare.API
   dotnet run --no-launch-profile --urls http://127.0.0.1:5167
   ```

   The SQL connection must resolve to the local development database. Keep the Vault-token environment variables in the same PowerShell process that runs `dotnet`.

4. Stop the API and Vault dev server with `Ctrl+C` in their respective terminals when finished.

## Runtime Validation Results

| Check | Result | Evidence |
|---|---|---|
| Backend solution build | PASS | 0 warnings and 0 errors after the EF and Vault fixes. |
| API startup | PASS | Started on `http://127.0.0.1:5167` against SQL Server. |
| Swagger and auth boundary | PASS | Swagger returned HTTP 200; an unauthenticated correction search returned HTTP 401. |
| Database prerequisites | PASS | v6 source column, enabled/trusted composite FK, and filtered unique pending-request index were verified. |
| Vault Transit provisioning | PASS | Three encrypted imports succeeded after the key-configuration fix. |
| C-08 student flows | PASS | Submitted three requests; verified own status; withdrew one pending request. |
| C-08 advisor flows | PASS | Assigned advisor search returned authorized values and access logging; approved one request and rejected another. |
| Persistence and encryption | PASS | Final statuses were approved, rejected, and withdrawn. Approval retained the original indicator and inserted a correction version. All three correction requests had 30-byte current/proposed ciphertexts and 32-byte current/proposed HMACs. |
| Notifications | PASS | Approval and rejection generated two student notifications. |
| Automated integration suite | UNVERIFIED | No automated test project was found; validation was performed manually against the live local API, SQL Server, and Vault dev server. |

This is development runtime evidence, not a production-readiness sign-off. The API reported the `Production` environment because it was launched with `--no-launch-profile`, but the database and Vault were local development services.

### C-09 build and runtime status

- Backend solution build passed with **0 warnings and 0 errors** after C-09 was added. The user's existing API process had its normal output assemblies open, so the successful build used a temporary output directory and did not disturb that running process.
- No automated test project was found. Manual API request examples are in `AdviseCare_API/AdviseCare.API/WelfareStatistics.http`.
- The C-09 endpoint has not yet been exercised against the new assembly at runtime. The API process that was already running during implementation still has the older loaded assembly; restart the API after stopping it with `Ctrl+C` to load C-09 and run the HTTP examples.

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
