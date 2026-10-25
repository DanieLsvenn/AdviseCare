# Member C Backend Handover and Local Vault Guide

**Audience:** AdviseCare project team  
**Scope:** Member C backend work completed after Member B's API changes were merged into this working branch. Frontend work is intentionally excluded.

For detailed endpoint-by-endpoint evidence, see [Member C Changes and Runtime Validation](./Member_C_Changes_and_Runtime_Validation.md). The current endpoint/task checklist is in the [API workflow list](./AdviseCare_API_Workflow_List.txt) and [project workflow list](./AdviseCare_Workflow_List.txt).

## Executive summary

Member C's backend checkpoints C-01 through C-10 are implemented and have passed the documented local runtime flows. The work covers external-service and welfare workflows, encrypted indicator import and access, student/advisor profiles, correction requests, and anonymised statistics.

The most important integration with Member B is the shared access guard. Member C does not duplicate consent rules: data reads use Member B's purpose, active role policy, student consent, and access-log services before decrypting a protected value.

The latest C-10 negative validation confirmed that a missing or withdrawn consent, a revoked advisor policy, and an inactive Cohort Analytics purpose do not release unauthorized values. All fixtures created for that validation were removed.

This is local development validation, not a production-readiness sign-off. Runtime checks were performed manually against the API, SQL Server, and Vault; the existing report records that no automated integration-test project was found.

## What Member C delivered

| Checkpoint | Backend behavior |
|---|---|
| C-01 | External-service catalogue reads with filtering, ordering, paging, and detail lookup. |
| C-02 | Administrator-only external-service create/update, validation, normalization, and persisted read-back. |
| C-03 | External referrals, follow-up outcomes, filters, and overdue-state updates by a background service. |
| C-04 | Welfare Officer escalations and Programme Head responses, with party-scoped visibility and in-app notifications. |
| C-05 | Interventions and support grants, including update/closure rules, outcome persistence, UTC handling, and advisor notification. |
| C-06a | CSV/XLSX preview, column mapping, and whole-file validation without writes. |
| C-06b1 | Authenticated envelope encryption and per-student/per-attribute Vault Transit key provisioning. |
| C-06b2 | All-or-nothing encrypted imports, student-code/email resolution, insert-only indicator rows, expected-but-missing rows, StudentProfile provisioning, and import activity logging. |
| C-07a | Assigned-advisor indicator reads. The access guard runs before decryption; permitted values are access-logged and ciphertext/HMAC are not returned. |
| C-07b | As-of history and comparison. The service selects the latest recorded version for each requested snapshot and applies the same assignment/consent checks. |
| C-07c | Student-only self-profile and JSON export, limited to the signed-in student's own data. Self-reads do not create staff access logs; the export creates an activity log. |
| C-08 | Correction submission, status/withdrawal, assigned-advisor review, approval/rejection, encrypted request values, notifications, and insert-only correction history. Approval creates a new `correction` indicator version rather than overwriting the original. |
| C-09 | Welfare Officer-only case statistics grouped by support category. Categories with fewer than five distinct students are suppressed; no student-level rows or overall total are returned; CSV export is activity-logged. |
| C-10 | Staff-only cohort indicator summaries. The service uses the latest row per student/indicator/term, checks active purpose + role policy + explicit consent before decryption, logs every source-value read, and suppresses each cell below ten students. Numeric values return a mean/count; categorical and boolean values return category/count cells. CSV uses the same summary. |

### Supporting fixes and integration

- Corrected the EF Core primary-key mapping for `Studentprofile.StudentId`.
- Preserved dependency-injected database options and aligned the fallback connection setting with `ConnectionStrings:AdviseCareDB`.
- Vault Transit key creation now explicitly configures `deletion_allowed=true`; failures are surfaced and newly created keys are cleaned up when possible.
- C-09 uses existing `CASES` data and does not wait for Member D's case API implementation.
- C-10 uses Member B's shared access guard rather than implementing a second consent/policy system.
- `DATASOURCES`, `INGESTIONSCHEDULES`, and `INGESTIONJOBS` remain future work; Google Sheets ingestion also remains future work.

## Concepts to explain to the team

### 1. Authentication is not the same as permission to read a value

The controller's `[Authorize]` role check is the outer API boundary. For protected student data, that is not enough. The access guard also checks:

1. The requested purpose is active and still covers the data attribute.
2. The caller has an active access policy for that purpose through one of their roles.
3. The student has granted an unexpired consent for that purpose and attribute.
4. Only then may the service validate and decrypt the value.

The read is logged per source value. An aggregate result can be released without exposing student identities or individual values.

### 2. Envelope encryption and Vault Transit

The database does not store imported indicator values as plaintext. For each student/data-attribute pair:

1. The application creates a random 32-byte data-encryption key (DEK).
2. Vault Transit wraps the DEK; the wrapped form and Vault key name are stored with the SQL `ATTRIBUTEKEYS` record.
3. The application encrypts the indicator payload with AES-256-GCM. The ciphertext, authentication tag/nonce, and a keyed HMAC are persisted; the raw DEK is cleared from memory after use.
4. To read the value, the application asks Vault Transit to unwrap the DEK, verifies the payload binding/integrity, and decrypts only after authorization.

Vault Transit is the key-management boundary; it is not where the application's SQL rows are stored. A separate destroyable key per student/attribute permits targeted cryptographic erasure without destroying every student's data.

The API configures each Transit key with `deletion_allowed=true` because the erasure workflow must be able to delete it. The Vault client implementation is in [VaultTransitKeyManagementService.cs](../AdviseCare_API/AdviseCare.Infrastructure/Security/VaultTransitKeyManagementService.cs); the envelope-encryption implementation is in [AuthenticatedEnvelopeEncryptionService.cs](../AdviseCare_API/AdviseCare.Infrastructure/Security/AuthenticatedEnvelopeEncryptionService.cs).

### 3. Missing, erased, and corrected data are different states

- A missing indicator row means the import expected a value, but no value was supplied; it has no key or encrypted payload.
- An erased row retains the audit/version record but has its live ciphertext/HMAC removed and its key destroyed.
- A correction is a new version. It does not rewrite the earlier version, which is why historical snapshots can still describe what the system knew at an earlier time.

`validFrom` represents when a value is effective; `transactionFrom` records when that version entered the system. The point-in-time view uses the recorded history rather than silently replacing older data.

### 4. Audit and anonymisation are separate controls

- `ACCESSLOGS` records which protected source values were read by a staff accessor and for which purpose.
- `ACTIVITYLOGS` records higher-level operations such as a successful import or statistics export.
- C-09 suppresses support-category cells below five students.
- C-10 suppresses every cohort cell below ten students.

Those thresholds apply to each output cell, not just the overall group size.

## Runtime evidence at a glance

- C-01 through C-05: catalogue, referral, escalation, intervention, support-grant, role-boundary, persistence, and notification flows passed.
- C-06a: CSV/XLSX preview and validation passed; validation-only requests did not write indicator/key/activity rows.
- C-06b1/b2: isolated-Vault encryption/key lifecycle checks and valid/invalid import paths passed.
- C-07a/b/c: advisor authorization, consent-gated reads, historical selection/comparison, self-only export, ciphertext exclusion, and audit behavior passed.
- C-08: three correction requests were exercised: approved, rejected, and withdrawn. Approval retained the original row and added a correction version.
- C-09: term and inclusive date-range aggregates, five-student suppression, CSV parity, input errors, and authorization boundaries passed.
- C-10 positive coverage: a 12-student cohort returned mean 6.5 (n=12) and one category with n=10; smaller categorical/boolean cells were suppressed. Filters, CSV parity, and per-value access logging were checked.
- C-10 negative coverage: a ten-consent positive control returned mean 5.5 (n=10). Removing one consent and withdrawing another each returned no below-threshold cell and logged exactly nine permitted reads, with no new log for the denied student. A revoked Academic Advisor policy returned an empty summary with no source-read logs. An inactive purpose returned HTTP 409 with no source-read logs. The 28 test access logs were removed.

The C-10 negative check used temporary staff account/session fixtures and a test-only JWT signing key to exercise the API's authorization/session middleware and data path. It did **not** test password login. The key, accounts, sessions, indicator rows, SQL key records, Vault keys, purpose/policy, import activity, access logs, temporary API/Vault, and token-bearing logs were cleaned up. The four pre-existing C-10 export activity records were retained.

## Vault: what it is and what the team should know

### Root token versus unseal key

- The **Vault dev Root Token** authenticates administrative Vault API/CLI operations. The API needs a Vault token in its `VaultTransit:Token` configuration.
- An **Unseal Key** is used to unseal a non-dev Vault. It is not an API token.
- A Vault **dev server starts initialized and unsealed**; for local dev, use its dev Root Token and do not run an unseal command.
- Treat the Root Token as a secret. Never commit it, paste it into source code, or include it in a team report or screenshot.

### Critical warning for the current shared development environment

The existing API on port `5167` and Vault on port `8200` are already running. The development database also retains the C-08 fixture, whose encrypted values depend on the current Vault Transit keys. Vault dev storage is ephemeral: restarting or replacing that Vault can lose its keys while SQL ciphertext remains, making those values undecryptable.

**Do not restart/stop the current Vault on port 8200, delete its Transit keys, or rerun the destructive v6 SQL rebuild script.** The v6 script is [AdviseCareDB_Revise.sql](./AdviseCareDB_Revise.sql). For new experiments, use a separate Vault port (for example `18200`) and preferably a separate database. Previous runtime probes used a temporary API on `5168` and isolated Vault on `18200`; they removed their SQL fixtures and did not stop the user's API/Vault.

### Local Windows PowerShell setup

Use **Windows PowerShell**, not `cmd.exe`. PowerShell environment syntax such as `$env:VAULT_ADDR` is not a CMD command.

The commands below use this machine's current Vault binary path, `D:\Office\vault.exe`; replace it if another team member installed Vault elsewhere.

#### A. Check the existing Vault without restarting it

In a new PowerShell window:

```powershell
$vaultExe = 'D:\Office\vault.exe'
$env:VAULT_ADDR = 'http://127.0.0.1:8200'
& $vaultExe status
```

The explicit `http://` matters for the local dev listener. Without it, the CLI may default to HTTPS and report that the HTTP server answered an HTTPS request. If the binary is in the current directory, PowerShell requires `.\vault.exe`; simply typing `vault` may fail unless Vault is on `PATH`.

#### B. Start Vault only for a fresh/isolated development environment

Do not run this on the existing port `8200` while the retained C-08 fixture is needed. On a fresh environment, open a dedicated PowerShell window and run:

```powershell
$vaultExe = 'D:\Office\vault.exe'
& $vaultExe server -dev -dev-listen-address=127.0.0.1:18200
```

Keep this window open. The startup output contains the temporary dev Root Token; capture it privately and do not copy the output into chat or source files. A dev server is intentionally for local testing only. Its in-memory state and keys are lost when it is stopped.

#### C. Point the CLI at that instance and enable Transit once

In another PowerShell window:

```powershell
$vaultExe = 'D:\Office\vault.exe'
$env:VAULT_ADDR = 'http://127.0.0.1:18200'
$secureToken = Read-Host 'Vault dev Root Token' -AsSecureString
$token = [System.Net.NetworkCredential]::new('', $secureToken).Password
$env:VAULT_TOKEN = $token
$env:VaultTransit__Token = $token
$env:VaultTransit__Address = 'http://127.0.0.1:18200/'
$env:VaultTransit__MountPath = 'transit'
$secureToken.Dispose()

& $vaultExe status
& $vaultExe secrets list -detailed
```

If `transit/` is not listed, enable it once:

```powershell
& $vaultExe secrets enable -path=transit transit
```

If the mount already exists, do not enable it again. `VAULT_TOKEN` is read by the Vault CLI; the .NET API reads `VaultTransit__Token` (double underscore maps to the configuration path `VaultTransit:Token`). Setting only `VAULT_TOKEN` does not configure the API.

#### D. Build and run the API

From the repository root, the API project can be selected explicitly:

```powershell
$project = 'D:\SchoolWork\SEP490\FinalProject\AdviseCare\AdviseCare_API\AdviseCare.API\AdviseCare.API.csproj'
dotnet run --project $project --no-launch-profile --urls http://127.0.0.1:5168
```

Use `5168` only when that port is free and the run is isolated; the existing API already uses `5167`. Alternatively, first change to the project directory and then run `dotnet run`. Command order matters: `Set-Location` must happen **before** `dotnet run` if relying on the current directory. Running from `D:\Office` without `--project` produces “Couldn't find a project to run.”

The API must receive `VaultTransit__Address`, `VaultTransit__MountPath`, and `VaultTransit__Token` **before it starts**. Environment changes in a new PowerShell window do not reconfigure an already-running API process. Verify the database connection points to a safe development/test database before using write endpoints.

After stopping only the processes started for the isolated test, clear the temporary environment values in that PowerShell window:

```powershell
Remove-Item Env:VAULT_TOKEN, Env:VaultTransit__Token -ErrorAction SilentlyContinue
Remove-Item Env:VaultTransit__Address, Env:VaultTransit__MountPath -ErrorAction SilentlyContinue
Remove-Variable token, secureToken, vaultExe, project -ErrorAction SilentlyContinue
```

Do not stop the currently running port-5167 API or port-8200 Vault as part of this cleanup.

### Common setup errors and their fixes

| Error or symptom | Cause | Fix |
|---|---|---|
| `$env:...` is reported as an unknown command or invalid path | PowerShell commands were entered at a `D:\...>` CMD prompt. | Open Windows PowerShell (prompt begins `PS`) and run the commands there. |
| Vault defaults to `https://127.0.0.1:8200` but reports an HTTP response | The dev listener is plain HTTP and `VAULT_ADDR` was unset or set to HTTPS. | Set `$env:VAULT_ADDR = 'http://127.0.0.1:<port>'` in the same shell before invoking the CLI. |
| `vault` is not recognized, but the file exists in the current folder | PowerShell does not search the current folder for executables by bare name. | Use `.\vault.exe` or call the full path with `& 'D:\Office\vault.exe'`. |
| `dotnet run` says it cannot find a project in `D:\Office` | The shell is not in the API project directory and no project path was provided. | Change directory first or pass the full `.csproj` path with `--project`. |
| Transit key creation returns 404 | The `transit` secrets engine is not mounted at the configured path. | Check `vault secrets list`; enable `transit/` once, and ensure `VaultTransit__MountPath='transit'`. |
| “Invalid token” / Vault 403 | The API has no valid token for that Vault instance, or the wrong token type was used. | Use the current instance's dev Root Token, not an Unseal Key; set `VaultTransit__Token` before starting the API. |
| Key deletion preflight fails | The key is not configured to permit deletion. | The current API configures new keys with `deletion_allowed=true`; surface any failure and do not claim erasure completed until Vault confirms deletion. |

## Current data and cleanup state

Last verified SQL counts after the C-10 negative test were: 4 `STUDENTINDICATORS`, 3 `ATTRIBUTEKEYS`, 5 `ACCESSLOGS`, 82 `ACTIVITYLOGS`, 1 academic term, 7 accounts, and 1 StudentProfile. The C-10 negative test returned these counts to their pre-test baseline.

The following are intentionally retained:

- The C-08 development fixture remains in SQL/Vault for inspection, including its correction history and supporting data. Do not clean it up without separate approval.
- One C-09 `welfareStatisticsExported` audit record and four earlier C-10 cohort-export activity records remain as evidence.

The latest C-10 negative test left no synthetic accounts, purpose, policy, attribute, term, indicator rows, keys, or test access logs. The temporary build output, probe script, API/Vault logs, and test processes were removed. The current API/Vault on ports `5167`/`8200` remained running.

## A short team briefing

> “Member C completed the backend import, welfare, profile, correction, and statistics flows. Indicator values are encrypted before SQL persistence using an application data key wrapped by Vault Transit. Member B's shared access guard decides whether a caller may read each student's value based on purpose, role policy, and explicit consent; only permitted values are decrypted and access-logged. Corrections append versions rather than overwrite history, and aggregate outputs suppress small groups. The local Vault root token is a development secret, not an unseal key. Our current Vault is ephemeral and contains keys used by the retained C-08 fixture, so we must not restart it or rerun the destructive SQL script.”

## Useful implementation references

- [Member C task division](./AdviseCare_Backend_Task_Division.txt)
- [Cohort indicator service](../AdviseCare_API/AdviseCare.Application/Services/CohortIndicatorService.cs)
- [Shared access guard](../AdviseCare_API/AdviseCare.Application/Services/AccessGuardService.cs)
- [Indicator import service](../AdviseCare_API/AdviseCare.Application/Services/StudentIndicatorImportService.cs)
- [Vault Transit key-management client](../AdviseCare_API/AdviseCare.Infrastructure/Security/VaultTransitKeyManagementService.cs)
- [Envelope-encryption service](../AdviseCare_API/AdviseCare.Infrastructure/Security/AuthenticatedEnvelopeEncryptionService.cs)
- [Runtime validation evidence](./Member_C_Changes_and_Runtime_Validation.md)
