/* =====================================================================
   AdviseCare - Microsoft SQL Server database script (v5, single file)
   47 tables
   1. Creates database AdviseCareDB if it does not exist
   2. Drops every AdviseCare table that exists, child tables first
   3. Creates all 47 tables in dependency order (parents first)
   4. Seeds the 7 roles, one test account per role, plus one faculty,
      one programme and one student profile for testing
   WARNING: re-running this script deletes all data in these tables.
   Conventions: uuid -> UNIQUEIDENTIFIER (NEWSEQUENTIALID), date -> DATETIME2(3) in UTC,
   enum -> NVARCHAR(30) + CHECK constraint, bool -> BIT, FK delete rule = NO ACTION.

   Review fixes integrated (v5 retains the v4 schema and adds SQL-level hardening):
   - B1: encrypted indicator/correction values, HMAC fingerprints, student+attribute key binding,
         consent-withdrawal validation, and a two-step KMS key-destruction workflow.
   - B2: case-team history, referral-specific note sharing, and SQL Server row-level security
         for CASENOTES. Other visibility-tagged fields remain API-authorized and are documented.
   - B3: normalized STUDENTPROFILES, role protection, enrolled-profile completeness checks,
         filtered unique studentCode index (already present in v4), and nonblank-code validation.
   - V5: stricter consent timestamp consistency, nonblank KMS key-reference validation, and
         stronger CASENOTES write protection for updates/deletes.
   IMPORTANT: This is a full rebuild script. Back up the database before running it.
   Cryptographic erasure across backups requires a per-attribute key in an external KMS/HSM.
   The application must confirm permanent KMS destruction before calling the confirmation procedure.
   ===================================================================== */

IF DB_ID(N'AdviseCareDB') IS NULL
BEGIN
    CREATE DATABASE [AdviseCareDB];
END
GO

USE [AdviseCareDB];
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE [name] = N'POLICY_CASENOTES_access' AND [schema_id] = SCHEMA_ID(N'dbo'))
    DROP SECURITY POLICY [dbo].[POLICY_CASENOTES_access];
IF OBJECT_ID(N'dbo.fn_CASENOTES_writePredicate', N'IF') IS NOT NULL
    DROP FUNCTION [dbo].[fn_CASENOTES_writePredicate];
IF OBJECT_ID(N'dbo.fn_CASENOTES_accessPredicate', N'IF') IS NOT NULL
    DROP FUNCTION [dbo].[fn_CASENOTES_accessPredicate];
IF OBJECT_ID(N'dbo.VW_CASETEAM', N'V') IS NOT NULL
    DROP VIEW [dbo].[VW_CASETEAM];
GO

/* ---------- 2. Drop tables (children first) ---------- */
DROP TABLE IF EXISTS [dbo].[RESTOREOPERATIONS];
DROP TABLE IF EXISTS [dbo].[CASENOTEREFERRALSHARES];
DROP TABLE IF EXISTS [dbo].[CASETEAMMEMBERS];
DROP TABLE IF EXISTS [dbo].[BACKUPS];
DROP TABLE IF EXISTS [dbo].[REPORTS];
DROP TABLE IF EXISTS [dbo].[ERASUREREQUESTS];
DROP TABLE IF EXISTS [dbo].[CORRECTIONREQUESTS];
DROP TABLE IF EXISTS [dbo].[STUDENTINDICATORS];
DROP TABLE IF EXISTS [dbo].[ATTRIBUTEKEYS];
DROP TABLE IF EXISTS [dbo].[EMERGENCYACCESSREQUESTS];
DROP TABLE IF EXISTS [dbo].[ACCESSLOGS];
DROP TABLE IF EXISTS [dbo].[ACCESSPOLICIES];
DROP TABLE IF EXISTS [dbo].[CONSENTREQUESTS];
DROP TABLE IF EXISTS [dbo].[CONSENTHISTORIES];
DROP TABLE IF EXISTS [dbo].[CONSENTS];
DROP TABLE IF EXISTS [dbo].[PURPOSE_ATTRIBUTE];
DROP TABLE IF EXISTS [dbo].[ESCALATIONS];
DROP TABLE IF EXISTS [dbo].[EXTERNALREFERRALS];
DROP TABLE IF EXISTS [dbo].[EXTERNALSERVICES];
DROP TABLE IF EXISTS [dbo].[SUPPORTGRANTS];
DROP TABLE IF EXISTS [dbo].[INTERVENTIONS];
DROP TABLE IF EXISTS [dbo].[REFERRALS];
DROP TABLE IF EXISTS [dbo].[CASESTAGEHISTORIES];
DROP TABLE IF EXISTS [dbo].[CASENOTES];
DROP TABLE IF EXISTS [dbo].[CASES];
DROP TABLE IF EXISTS [dbo].[ASSIGNMENTCHANGEREQUESTS];
DROP TABLE IF EXISTS [dbo].[ADVISORASSIGNMENTS];
DROP TABLE IF EXISTS [dbo].[SUPPORTCATEGORIES];
DROP TABLE IF EXISTS [dbo].[CONSENTPURPOSES];
DROP TABLE IF EXISTS [dbo].[ACCOUNT_SUBJECT];
DROP TABLE IF EXISTS [dbo].[INGESTIONJOBS];
DROP TABLE IF EXISTS [dbo].[INGESTIONSCHEDULES];
DROP TABLE IF EXISTS [dbo].[DATASOURCES];
DROP TABLE IF EXISTS [dbo].[SUBJECTS];
DROP TABLE IF EXISTS [dbo].[INDICATORS];
DROP TABLE IF EXISTS [dbo].[DATAATTRIBUTES];
DROP TABLE IF EXISTS [dbo].[ACADEMICTERMS];
DROP TABLE IF EXISTS [dbo].[STUDENTPROFILES];
DROP TABLE IF EXISTS [dbo].[PROGRAMMES];
DROP TABLE IF EXISTS [dbo].[ACTIVITYLOGS];
DROP TABLE IF EXISTS [dbo].[NOTIFICATIONPREFERENCES];
DROP TABLE IF EXISTS [dbo].[NOTIFICATIONS];
DROP TABLE IF EXISTS [dbo].[SESSIONS];
DROP TABLE IF EXISTS [dbo].[ACCOUNT_ROLE];
DROP TABLE IF EXISTS [dbo].[ROLES];
DROP TABLE IF EXISTS [dbo].[ACCOUNTS];
DROP TABLE IF EXISTS [dbo].[FACULTIES];
GO

/* ---------- 3. Create tables (parents first) ---------- */
-- 8. FACULTIES
CREATE TABLE [dbo].[FACULTIES] (
    [facultyId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_FACULTIES_facultyId] DEFAULT NEWSEQUENTIALID(),
    [facultyCode] NVARCHAR(255) NOT NULL,
    [facultyName] NVARCHAR(255) NOT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FACULTIES_status] DEFAULT N'active',
    CONSTRAINT [PK_FACULTIES] PRIMARY KEY ([facultyId]),
    CONSTRAINT [UQ_FACULTIES_facultyCode] UNIQUE ([facultyCode]),
    CONSTRAINT [CK_FACULTIES_status] CHECK ([status] IN (N'active', N'archived'))
);
GO

-- 1. ACCOUNTS
CREATE TABLE [dbo].[ACCOUNTS] (
    [accountId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ACCOUNTS_accountId] DEFAULT NEWSEQUENTIALID(),
    [facultyId] UNIQUEIDENTIFIER NULL,
    [avatarUrl] NVARCHAR(MAX) NULL,
    [email] NVARCHAR(255) NOT NULL,
    [fullName] NVARCHAR(255) NOT NULL,
    [googleId] NVARCHAR(255) NULL,
    [authProvider] NVARCHAR(255) NULL,
    [passwordHash] NVARCHAR(255) NULL,
    [resetToken] NVARCHAR(255) NULL,
    [resetTokenExpiresAt] DATETIME2(3) NULL,
    [userName] NVARCHAR(255) NOT NULL,
    [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ACCOUNTS_createdAt] DEFAULT SYSUTCDATETIME(),
    [linkStatus] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ACCOUNTS_linkStatus] DEFAULT N'notLinked',
    [preferredLanguage] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ACCOUNTS_preferredLanguage] DEFAULT N'vi',
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ACCOUNTS_status] DEFAULT N'active',
    CONSTRAINT [PK_ACCOUNTS] PRIMARY KEY ([accountId]),
    CONSTRAINT [FK_ACCOUNTS_facultyId] FOREIGN KEY ([facultyId]) REFERENCES [dbo].[FACULTIES]([facultyId]),
    CONSTRAINT [UQ_ACCOUNTS_email] UNIQUE ([email]),
    CONSTRAINT [UQ_ACCOUNTS_userName] UNIQUE ([userName]),
    CONSTRAINT [CK_ACCOUNTS_linkStatus] CHECK ([linkStatus] IN (N'notLinked', N'emailSent', N'linked')),
    CONSTRAINT [CK_ACCOUNTS_preferredLanguage] CHECK ([preferredLanguage] IN (N'vi', N'en')),
    CONSTRAINT [CK_ACCOUNTS_status] CHECK ([status] IN (N'active', N'deactivated', N'banned'))
);
GO

-- 3. ROLES
CREATE TABLE [dbo].[ROLES] (
    [roleId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ROLES_roleId] DEFAULT NEWSEQUENTIALID(),
    [roleName] NVARCHAR(255) NOT NULL,
    CONSTRAINT [PK_ROLES] PRIMARY KEY ([roleId]),
    CONSTRAINT [UQ_ROLES_roleName] UNIQUE ([roleName])
);
GO

-- 2. ACCOUNT_ROLE
CREATE TABLE [dbo].[ACCOUNT_ROLE] (
    [accountId] UNIQUEIDENTIFIER NOT NULL,
    [roleId] UNIQUEIDENTIFIER NOT NULL,
    [assignedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ACCOUNT_ROLE_assignedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ACCOUNT_ROLE] PRIMARY KEY ([accountId], [roleId]),
    CONSTRAINT [FK_ACCOUNT_ROLE_accountId] FOREIGN KEY ([accountId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ACCOUNT_ROLE_roleId] FOREIGN KEY ([roleId]) REFERENCES [dbo].[ROLES]([roleId])
);
GO

-- 4. SESSIONS
CREATE TABLE [dbo].[SESSIONS] (
    [sessionId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SESSIONS_sessionId] DEFAULT NEWSEQUENTIALID(),
    [accountId] UNIQUEIDENTIFIER NOT NULL,
    [deviceInfo] NVARCHAR(MAX) NULL,
    [loginAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_SESSIONS_loginAt] DEFAULT SYSUTCDATETIME(),
    [logoutAt] DATETIME2(3) NULL,
    [isActive] BIT NOT NULL CONSTRAINT [DF_SESSIONS_isActive] DEFAULT 1,
    CONSTRAINT [PK_SESSIONS] PRIMARY KEY ([sessionId]),
    CONSTRAINT [FK_SESSIONS_accountId] FOREIGN KEY ([accountId]) REFERENCES [dbo].[ACCOUNTS]([accountId])
);
GO

-- 5. NOTIFICATIONS
CREATE TABLE [dbo].[NOTIFICATIONS] (
    [notificationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_NOTIFICATIONS_notificationId] DEFAULT NEWSEQUENTIALID(),
    [accountId] UNIQUEIDENTIFIER NOT NULL,
    [message] NVARCHAR(MAX) NOT NULL,
    [redirectUrl] NVARCHAR(MAX) NULL,
    [title] NVARCHAR(255) NOT NULL,
    [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_NOTIFICATIONS_createdAt] DEFAULT SYSUTCDATETIME(),
    [isRead] BIT NOT NULL CONSTRAINT [DF_NOTIFICATIONS_isRead] DEFAULT 0,
    CONSTRAINT [PK_NOTIFICATIONS] PRIMARY KEY ([notificationId]),
    CONSTRAINT [FK_NOTIFICATIONS_accountId] FOREIGN KEY ([accountId]) REFERENCES [dbo].[ACCOUNTS]([accountId])
);
GO

-- 6. NOTIFICATIONPREFERENCES
CREATE TABLE [dbo].[NOTIFICATIONPREFERENCES] (
    [notificationPreferenceId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_NOTIFICATIONPREFERENCES_notificationPreferenceId] DEFAULT NEWSEQUENTIALID(),
    [accountId] UNIQUEIDENTIFIER NOT NULL,
    [channel] NVARCHAR(30) NOT NULL CONSTRAINT [DF_NOTIFICATIONPREFERENCES_channel] DEFAULT N'email',
    [isEnabled] BIT NOT NULL CONSTRAINT [DF_NOTIFICATIONPREFERENCES_isEnabled] DEFAULT 1,
    CONSTRAINT [PK_NOTIFICATIONPREFERENCES] PRIMARY KEY ([notificationPreferenceId]),
    CONSTRAINT [FK_NOTIFICATIONPREFERENCES_accountId] FOREIGN KEY ([accountId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_NOTIFICATIONPREFERENCES_channel] CHECK ([channel] IN (N'email', N'inApp'))
);
GO

-- 7. ACTIVITYLOGS
CREATE TABLE [dbo].[ACTIVITYLOGS] (
    [activityLogId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ACTIVITYLOGS_activityLogId] DEFAULT NEWSEQUENTIALID(),
    [accountId] UNIQUEIDENTIFIER NOT NULL,
    [activityDetail] NVARCHAR(MAX) NULL,
    -- B2.1: access classification for activityDetail; the API must enforce it.
    [activityDetailVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ACTIVITYLOGS_activityDetailVisibility] DEFAULT N'adminOnly',
    [activityType] NVARCHAR(255) NOT NULL,
    [occurredAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ACTIVITYLOGS_occurredAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ACTIVITYLOGS] PRIMARY KEY ([activityLogId]),
    CONSTRAINT [FK_ACTIVITYLOGS_accountId] FOREIGN KEY ([accountId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_ACTIVITYLOGS_activityDetailVisibility] CHECK ([activityDetailVisibility] IN (N'adminOnly', N'stewardAndAdmin'))
);
GO

-- 9. PROGRAMMES
CREATE TABLE [dbo].[PROGRAMMES] (
    [programmeId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_PROGRAMMES_programmeId] DEFAULT NEWSEQUENTIALID(),
    [facultyId] UNIQUEIDENTIFIER NOT NULL,
    -- B3.4: head of this programme (must hold the Programme Head role, see trigger below).
    [programmeHeadId] UNIQUEIDENTIFIER NULL,
    [programmeCode] NVARCHAR(255) NOT NULL,
    [programmeName] NVARCHAR(255) NOT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_PROGRAMMES_status] DEFAULT N'active',
    CONSTRAINT [PK_PROGRAMMES] PRIMARY KEY ([programmeId]),
    CONSTRAINT [FK_PROGRAMMES_facultyId] FOREIGN KEY ([facultyId]) REFERENCES [dbo].[FACULTIES]([facultyId]),
    CONSTRAINT [FK_PROGRAMMES_programmeHeadId] FOREIGN KEY ([programmeHeadId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [UQ_PROGRAMMES_programmeCode] UNIQUE ([programmeCode]),
    CONSTRAINT [CK_PROGRAMMES_status] CHECK ([status] IN (N'active', N'archived'))
);
GO

-- B3.4: only accounts holding the Programme Head role may be set as a programme head.
CREATE TRIGGER [dbo].[TR_PROGRAMMES_requireHeadRole]
ON [dbo].[PROGRAMMES]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE i.[programmeHeadId] IS NOT NULL
          AND NOT EXISTS (
            SELECT 1
            FROM [dbo].[ACCOUNT_ROLE] ar
            JOIN [dbo].[ROLES] r ON r.[roleId] = ar.[roleId]
            WHERE ar.[accountId] = i.[programmeHeadId]
              AND r.[roleName] = N'Programme Head'
        )
    )
    BEGIN
        THROW 50002, N'PROGRAMMES.programmeHeadId must belong to an account with the Programme Head role.', 1;
    END
END;
GO

-- 45. STUDENTPROFILES
CREATE TABLE [dbo].[STUDENTPROFILES] (
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [studentCode] NVARCHAR(50) NULL,
    [programmeId] UNIQUEIDENTIFIER NULL,
    [academicStatus] NVARCHAR(30) NOT NULL CONSTRAINT [DF_STUDENTPROFILES_academicStatus] DEFAULT N'unknown',
    CONSTRAINT [PK_STUDENTPROFILES] PRIMARY KEY ([studentId]),
    CONSTRAINT [FK_STUDENTPROFILES_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_STUDENTPROFILES_programmeId] FOREIGN KEY ([programmeId]) REFERENCES [dbo].[PROGRAMMES]([programmeId]),
    CONSTRAINT [CK_STUDENTPROFILES_academicStatus] CHECK ([academicStatus] IN (N'enrolled', N'onLeave', N'graduated', N'withdrawn', N'unknown')),
    CONSTRAINT [CK_STUDENTPROFILES_enrolled_requirements] CHECK ([academicStatus] <> N'enrolled' OR ([studentCode] IS NOT NULL AND [programmeId] IS NOT NULL))
);
GO

-- B3: only accounts holding the Student role may have a student profile.
CREATE TRIGGER [dbo].[TR_STUDENTPROFILES_requireStudentRole]
ON [dbo].[STUDENTPROFILES]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1
            FROM [dbo].[ACCOUNT_ROLE] ar
            JOIN [dbo].[ROLES] r ON r.[roleId] = ar.[roleId]
            WHERE ar.[accountId] = i.[studentId]
              AND r.[roleName] = N'Student'
        )
    )
    BEGIN
        THROW 50001, N'STUDENTPROFILES.studentId must belong to an account with the Student role.', 1;
    END
END;
GO

-- B3.2: the Student / Programme Head role cannot be removed while something depends on it.
CREATE TRIGGER [dbo].[TR_ACCOUNT_ROLE_protectRoles]
ON [dbo].[ACCOUNT_ROLE]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN [dbo].[ROLES] r ON r.[roleId] = d.[roleId]
        JOIN [dbo].[STUDENTPROFILES] sp ON sp.[studentId] = d.[accountId]
        WHERE r.[roleName] = N'Student'
          AND NOT EXISTS (SELECT 1 FROM inserted i WHERE i.[accountId] = d.[accountId] AND i.[roleId] = d.[roleId])
    )
    BEGIN
        THROW 50004, N'Cannot remove the Student role from an account that still has a student profile.', 1;
    END
    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN [dbo].[ROLES] r ON r.[roleId] = d.[roleId]
        JOIN [dbo].[PROGRAMMES] p ON p.[programmeHeadId] = d.[accountId]
        WHERE r.[roleName] = N'Programme Head'
          AND NOT EXISTS (SELECT 1 FROM inserted i WHERE i.[accountId] = d.[accountId] AND i.[roleId] = d.[roleId])
    )
    BEGIN
        THROW 50005, N'Cannot remove the Programme Head role from an account that still heads a programme.', 1;
    END
END;
GO

-- 10. ACADEMICTERMS
CREATE TABLE [dbo].[ACADEMICTERMS] (
    [termId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ACADEMICTERMS_termId] DEFAULT NEWSEQUENTIALID(),
    [endDate] DATETIME2(3) NOT NULL,
    [startDate] DATETIME2(3) NOT NULL,
    [termName] NVARCHAR(255) NOT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ACADEMICTERMS_status] DEFAULT N'upcoming',
    CONSTRAINT [PK_ACADEMICTERMS] PRIMARY KEY ([termId]),
    CONSTRAINT [CK_ACADEMICTERMS_status] CHECK ([status] IN (N'upcoming', N'current', N'past'))
);
GO

-- 13. DATAATTRIBUTES
CREATE TABLE [dbo].[DATAATTRIBUTES] (
    [dataAttributeId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_DATAATTRIBUTES_dataAttributeId] DEFAULT NEWSEQUENTIALID(),
    [attributeDescription] NVARCHAR(MAX) NULL,
    [attributeName] NVARCHAR(255) NOT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_DATAATTRIBUTES_status] DEFAULT N'active',
    CONSTRAINT [PK_DATAATTRIBUTES] PRIMARY KEY ([dataAttributeId]),
    CONSTRAINT [UQ_DATAATTRIBUTES_attributeName] UNIQUE ([attributeName]),
    CONSTRAINT [CK_DATAATTRIBUTES_status] CHECK ([status] IN (N'active', N'archived'))
);
GO

-- 11. INDICATORS
CREATE TABLE [dbo].[INDICATORS] (
    [indicatorId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_INDICATORS_indicatorId] DEFAULT NEWSEQUENTIALID(),
    [dataAttributeId] UNIQUEIDENTIFIER NOT NULL,
    [description] NVARCHAR(MAX) NULL,
    [indicatorName] NVARCHAR(255) NOT NULL,
    [dataType] NVARCHAR(30) NOT NULL CONSTRAINT [DF_INDICATORS_dataType] DEFAULT N'numeric',
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_INDICATORS_status] DEFAULT N'active',
    CONSTRAINT [PK_INDICATORS] PRIMARY KEY ([indicatorId]),
    CONSTRAINT [FK_INDICATORS_dataAttributeId] FOREIGN KEY ([dataAttributeId]) REFERENCES [dbo].[DATAATTRIBUTES]([dataAttributeId]),
    CONSTRAINT [UQ_INDICATORS_dataAttributeId] UNIQUE ([dataAttributeId]),
    -- B1.3: lets STUDENTINDICATORS prove (indicator, attribute) is a real pair.
    CONSTRAINT [UQ_INDICATORS_indicatorId_dataAttributeId] UNIQUE ([indicatorId], [dataAttributeId]),
    CONSTRAINT [UQ_INDICATORS_indicatorName] UNIQUE ([indicatorName]),
    CONSTRAINT [CK_INDICATORS_dataType] CHECK ([dataType] IN (N'numeric', N'categorical', N'boolean')),
    CONSTRAINT [CK_INDICATORS_status] CHECK ([status] IN (N'active', N'archived'))
);
GO

-- 12. SUBJECTS
CREATE TABLE [dbo].[SUBJECTS] (
    [subjectId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SUBJECTS_subjectId] DEFAULT NEWSEQUENTIALID(),
    [facultyId] UNIQUEIDENTIFIER NOT NULL,
    [subjectCode] NVARCHAR(255) NOT NULL,
    [subjectName] NVARCHAR(255) NOT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_SUBJECTS_status] DEFAULT N'active',
    CONSTRAINT [PK_SUBJECTS] PRIMARY KEY ([subjectId]),
    CONSTRAINT [FK_SUBJECTS_facultyId] FOREIGN KEY ([facultyId]) REFERENCES [dbo].[FACULTIES]([facultyId]),
    CONSTRAINT [UQ_SUBJECTS_subjectCode] UNIQUE ([subjectCode]),
    CONSTRAINT [CK_SUBJECTS_status] CHECK ([status] IN (N'active', N'archived'))
);
GO

-- 14. DATASOURCES
CREATE TABLE [dbo].[DATASOURCES] (
    [dataSourceId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_DATASOURCES_dataSourceId] DEFAULT NEWSEQUENTIALID(),
    [createdBy] UNIQUEIDENTIFIER NULL,
    [indicatorId] UNIQUEIDENTIFIER NOT NULL,
    [connectionConfig] NVARCHAR(MAX) NULL,
    [fieldMapping] NVARCHAR(MAX) NULL,
    [sourceName] NVARCHAR(255) NOT NULL,
    [sourceType] NVARCHAR(30) NOT NULL CONSTRAINT [DF_DATASOURCES_sourceType] DEFAULT N'file',
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_DATASOURCES_status] DEFAULT N'active',
    CONSTRAINT [PK_DATASOURCES] PRIMARY KEY ([dataSourceId]),
    CONSTRAINT [FK_DATASOURCES_createdBy] FOREIGN KEY ([createdBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_DATASOURCES_indicatorId] FOREIGN KEY ([indicatorId]) REFERENCES [dbo].[INDICATORS]([indicatorId]),
    CONSTRAINT [UQ_DATASOURCES_indicatorId] UNIQUE ([indicatorId]),
    CONSTRAINT [CK_DATASOURCES_sourceType] CHECK ([sourceType] IN (N'file', N'database', N'api')),
    CONSTRAINT [CK_DATASOURCES_status] CHECK ([status] IN (N'active', N'paused', N'disconnected'))
);
GO

-- 15. INGESTIONSCHEDULES
CREATE TABLE [dbo].[INGESTIONSCHEDULES] (
    [ingestionScheduleId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_INGESTIONSCHEDULES_ingestionScheduleId] DEFAULT NEWSEQUENTIALID(),
    [dataSourceId] UNIQUEIDENTIFIER NOT NULL,
    [scheduledStartTime] TIME(0) NOT NULL,
    [ingestionFrequency] INT NOT NULL,
    [lastRun] DATETIME2(3) NULL,
    [numberOfSuccesses] INT NOT NULL CONSTRAINT [DF_INGESTIONSCHEDULES_numberOfSuccesses] DEFAULT 0,
    [numberOfFails] INT NOT NULL CONSTRAINT [DF_INGESTIONSCHEDULES_numberOfFails] DEFAULT 0,
    [onFailure] NVARCHAR(30) NOT NULL CONSTRAINT [DF_INGESTIONSCHEDULES_onFailure] DEFAULT N'warn',
    [failureRetryInterval] INT NULL,
    [numberOfRetries] INT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_INGESTIONSCHEDULES_status] DEFAULT N'active',
    CONSTRAINT [PK_INGESTIONSCHEDULES] PRIMARY KEY ([ingestionScheduleId]),
    CONSTRAINT [FK_INGESTIONSCHEDULES_dataSourceId] FOREIGN KEY ([dataSourceId]) REFERENCES [dbo].[DATASOURCES]([dataSourceId]),
    CONSTRAINT [CK_INGESTIONSCHEDULES_onFailure] CHECK ([onFailure] IN (N'warn', N'skip', N'retry')),
    CONSTRAINT [CK_INGESTIONSCHEDULES_status] CHECK ([status] IN (N'active', N'paused', N'archived'))
);
GO

-- 16. INGESTIONJOBS
CREATE TABLE [dbo].[INGESTIONJOBS] (
    [ingestionJobId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_INGESTIONJOBS_ingestionJobId] DEFAULT NEWSEQUENTIALID(),
    [ingestionScheduleId] UNIQUEIDENTIFIER NOT NULL,
    [expectedStartTime] DATETIME2(3) NOT NULL,
    [completedAt] DATETIME2(3) NULL,
    [errorDetails] NVARCHAR(MAX) NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_INGESTIONJOBS_status] DEFAULT N'pending',
    CONSTRAINT [PK_INGESTIONJOBS] PRIMARY KEY ([ingestionJobId]),
    CONSTRAINT [FK_INGESTIONJOBS_ingestionScheduleId] FOREIGN KEY ([ingestionScheduleId]) REFERENCES [dbo].[INGESTIONSCHEDULES]([ingestionScheduleId]),
    CONSTRAINT [CK_INGESTIONJOBS_status] CHECK ([status] IN (N'pending', N'running', N'success', N'failed'))
);
GO

-- 17. STUDENTINDICATORS
-- FKs that need ATTRIBUTEKEYS are added after that table exists (see below).
CREATE TABLE [dbo].[STUDENTINDICATORS] (
    [studentIndicatorId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_STUDENTINDICATORS_studentIndicatorId] DEFAULT NEWSEQUENTIALID(),
    [dataSourceId] UNIQUEIDENTIFIER NULL,
    [indicatorId] UNIQUEIDENTIFIER NOT NULL,
    -- B1.3: copy of INDICATORS.dataAttributeId, enforced by the composite FK below,
    -- so the encryption key can be forced to belong to the same attribute.
    [dataAttributeId] UNIQUEIDENTIFIER NOT NULL,
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [attributeKeyId] UNIQUEIDENTIFIER NULL,
    [termId] UNIQUEIDENTIFIER NOT NULL,
    -- Raw authenticated-encryption payload (nonce + ciphertext + tag); never store plaintext here.
    [indicatorValue] VARBINARY(MAX) NULL,
    -- HMAC-SHA256 of normalized plaintext, using an HKDF-derived subkey never stored in SQL Server.
    [valueHmac] VARBINARY(32) NULL,
    -- Free text: must describe where the value came from, never contain the value itself.
    [provenanceNote] NVARCHAR(MAX) NULL,
    [transactionFrom] DATETIME2(3) NOT NULL CONSTRAINT [DF_STUDENTINDICATORS_transactionFrom] DEFAULT SYSUTCDATETIME(),
    [validFrom] DATETIME2(3) NOT NULL,
    [isMissing] BIT NOT NULL CONSTRAINT [DF_STUDENTINDICATORS_isMissing] DEFAULT 0,
    -- B1.2: set when the key is destroyed and the ciphertext is NULLed.
    [erasedAt] DATETIME2(3) NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_STUDENTINDICATORS_status] DEFAULT N'original',
    CONSTRAINT [PK_STUDENTINDICATORS] PRIMARY KEY ([studentIndicatorId]),
    CONSTRAINT [FK_STUDENTINDICATORS_dataSourceId] FOREIGN KEY ([dataSourceId]) REFERENCES [dbo].[DATASOURCES]([dataSourceId]),
    CONSTRAINT [FK_STUDENTINDICATORS_indicatorId_dataAttributeId] FOREIGN KEY ([indicatorId], [dataAttributeId]) REFERENCES [dbo].[INDICATORS]([indicatorId], [dataAttributeId]),
    CONSTRAINT [FK_STUDENTINDICATORS_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[STUDENTPROFILES]([studentId]),
    CONSTRAINT [FK_STUDENTINDICATORS_termId] FOREIGN KEY ([termId]) REFERENCES [dbo].[ACADEMICTERMS]([termId]),
    -- Three legal states: missing / live (ciphertext present) / erased (ciphertext NULLed, key kept for audit).
    CONSTRAINT [CK_STUDENTINDICATORS_missing_payload] CHECK (
        ([isMissing] = 1 AND [attributeKeyId] IS NULL AND [indicatorValue] IS NULL AND [valueHmac] IS NULL AND [erasedAt] IS NULL)
        OR ([isMissing] = 0 AND [attributeKeyId] IS NOT NULL AND [indicatorValue] IS NOT NULL AND [valueHmac] IS NOT NULL AND [erasedAt] IS NULL)
        OR ([isMissing] = 0 AND [attributeKeyId] IS NOT NULL AND [indicatorValue] IS NULL AND [valueHmac] IS NULL AND [erasedAt] IS NOT NULL)
    ),
    CONSTRAINT [CK_STUDENTINDICATORS_status] CHECK ([status] IN (N'original', N'correction'))
);
GO

-- 18. ACCOUNT_SUBJECT
CREATE TABLE [dbo].[ACCOUNT_SUBJECT] (
    [accountId] UNIQUEIDENTIFIER NOT NULL,
    [subjectId] UNIQUEIDENTIFIER NOT NULL,
    [assignedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ACCOUNT_SUBJECT_assignedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ACCOUNT_SUBJECT] PRIMARY KEY ([accountId], [subjectId]),
    CONSTRAINT [FK_ACCOUNT_SUBJECT_accountId] FOREIGN KEY ([accountId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ACCOUNT_SUBJECT_subjectId] FOREIGN KEY ([subjectId]) REFERENCES [dbo].[SUBJECTS]([subjectId])
);
GO

-- 32. CONSENTPURPOSES
CREATE TABLE [dbo].[CONSENTPURPOSES] (
    [consentPurposeId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CONSENTPURPOSES_consentPurposeId] DEFAULT NEWSEQUENTIALID(),
    [createdBy] UNIQUEIDENTIFIER NOT NULL,
    [description] NVARCHAR(MAX) NULL,
    [purposeName] NVARCHAR(255) NOT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CONSENTPURPOSES_status] DEFAULT N'inactive',
    CONSTRAINT [PK_CONSENTPURPOSES] PRIMARY KEY ([consentPurposeId]),
    CONSTRAINT [FK_CONSENTPURPOSES_createdBy] FOREIGN KEY ([createdBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [UQ_CONSENTPURPOSES_purposeName] UNIQUE ([purposeName]),
    CONSTRAINT [CK_CONSENTPURPOSES_status] CHECK ([status] IN (N'active', N'inactive', N'retired'))
);
GO

-- 19. SUPPORTCATEGORIES
CREATE TABLE [dbo].[SUPPORTCATEGORIES] (
    [supportCategoryId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SUPPORTCATEGORIES_supportCategoryId] DEFAULT NEWSEQUENTIALID(),
    [consentPurposeId] UNIQUEIDENTIFIER NOT NULL,
    [createdBy] UNIQUEIDENTIFIER NOT NULL,
    [categoryName] NVARCHAR(255) NOT NULL,
    [description] NVARCHAR(MAX) NULL,
    [supportType] NVARCHAR(30) NOT NULL CONSTRAINT [DF_SUPPORTCATEGORIES_supportType] DEFAULT N'academic',
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_SUPPORTCATEGORIES_status] DEFAULT N'active',
    CONSTRAINT [PK_SUPPORTCATEGORIES] PRIMARY KEY ([supportCategoryId]),
    CONSTRAINT [FK_SUPPORTCATEGORIES_consentPurposeId] FOREIGN KEY ([consentPurposeId]) REFERENCES [dbo].[CONSENTPURPOSES]([consentPurposeId]),
    CONSTRAINT [FK_SUPPORTCATEGORIES_createdBy] FOREIGN KEY ([createdBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_SUPPORTCATEGORIES_supportType] CHECK ([supportType] IN (N'academic', N'wellbeing')),
    CONSTRAINT [CK_SUPPORTCATEGORIES_status] CHECK ([status] IN (N'active', N'archived'))
);
GO

-- 20. ADVISORASSIGNMENTS
CREATE TABLE [dbo].[ADVISORASSIGNMENTS] (
    [advisorAssignmentId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ADVISORASSIGNMENTS_advisorAssignmentId] DEFAULT NEWSEQUENTIALID(),
    [advisorId] UNIQUEIDENTIFIER NOT NULL,
    [assignedBy] UNIQUEIDENTIFIER NOT NULL,
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [assignedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ADVISORASSIGNMENTS_assignedAt] DEFAULT SYSUTCDATETIME(),
    [revokedAt] DATETIME2(3) NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ADVISORASSIGNMENTS_status] DEFAULT N'active',
    CONSTRAINT [PK_ADVISORASSIGNMENTS] PRIMARY KEY ([advisorAssignmentId]),
    CONSTRAINT [FK_ADVISORASSIGNMENTS_advisorId] FOREIGN KEY ([advisorId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ADVISORASSIGNMENTS_assignedBy] FOREIGN KEY ([assignedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ADVISORASSIGNMENTS_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[STUDENTPROFILES]([studentId]),
    CONSTRAINT [CK_ADVISORASSIGNMENTS_status] CHECK ([status] IN (N'active', N'revoked'))
);
GO

-- 21. ASSIGNMENTCHANGEREQUESTS
CREATE TABLE [dbo].[ASSIGNMENTCHANGEREQUESTS] (
    [assignmentChangeRequestId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ASSIGNMENTCHANGEREQUESTS_assignmentChangeRequestId] DEFAULT NEWSEQUENTIALID(),
    [advisorAssignmentId] UNIQUEIDENTIFIER NOT NULL,
    [programmeHeadId] UNIQUEIDENTIFIER NULL,
    [proposedAdvisorId] UNIQUEIDENTIFIER NOT NULL,
    [requestedBy] UNIQUEIDENTIFIER NOT NULL,
    [reason] NVARCHAR(MAX) NOT NULL,
    -- B2.1: access classification for reason; the API must enforce it.
    -- headOnly = programme head only; parties = requester, proposed advisor and programme head.
    [reasonVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ASSIGNMENTCHANGEREQUESTS_reasonVisibility] DEFAULT N'headOnly',
    [decidedAt] DATETIME2(3) NULL,
    [requestedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ASSIGNMENTCHANGEREQUESTS_requestedAt] DEFAULT SYSUTCDATETIME(),
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ASSIGNMENTCHANGEREQUESTS_status] DEFAULT N'pending',
    CONSTRAINT [PK_ASSIGNMENTCHANGEREQUESTS] PRIMARY KEY ([assignmentChangeRequestId]),
    CONSTRAINT [FK_ASSIGNMENTCHANGEREQUESTS_advisorAssignmentId] FOREIGN KEY ([advisorAssignmentId]) REFERENCES [dbo].[ADVISORASSIGNMENTS]([advisorAssignmentId]),
    CONSTRAINT [FK_ASSIGNMENTCHANGEREQUESTS_programmeHeadId] FOREIGN KEY ([programmeHeadId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ASSIGNMENTCHANGEREQUESTS_proposedAdvisorId] FOREIGN KEY ([proposedAdvisorId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ASSIGNMENTCHANGEREQUESTS_requestedBy] FOREIGN KEY ([requestedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_ASSIGNMENTCHANGEREQUESTS_reasonVisibility] CHECK ([reasonVisibility] IN (N'headOnly', N'parties')),
    CONSTRAINT [CK_ASSIGNMENTCHANGEREQUESTS_status] CHECK ([status] IN (N'pending', N'approved', N'rejected'))
);
GO

-- B3.4: when a programme head is recorded, it must be the head of the student's own programme.
CREATE TRIGGER [dbo].[TR_ASSIGNMENTCHANGEREQUESTS_programmeHead]
ON [dbo].[ASSIGNMENTCHANGEREQUESTS]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN [dbo].[ADVISORASSIGNMENTS] aa ON aa.[advisorAssignmentId] = i.[advisorAssignmentId]
        LEFT JOIN [dbo].[STUDENTPROFILES] sp ON sp.[studentId] = aa.[studentId]
        LEFT JOIN [dbo].[PROGRAMMES] p ON p.[programmeId] = sp.[programmeId]
        WHERE i.[programmeHeadId] IS NOT NULL
          AND (p.[programmeHeadId] IS NULL OR p.[programmeHeadId] <> i.[programmeHeadId])
    )
    BEGIN
        THROW 50003, N'ASSIGNMENTCHANGEREQUESTS.programmeHeadId must be the head of the student''s programme.', 1;
    END
END;
GO

-- 22. CASES
CREATE TABLE [dbo].[CASES] (
    [caseId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CASES_caseId] DEFAULT NEWSEQUENTIALID(),
    [advisorId] UNIQUEIDENTIFIER NULL,
    [openedBy] UNIQUEIDENTIFIER NOT NULL,
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [subjectId] UNIQUEIDENTIFIER NULL,
    [supportCategoryId] UNIQUEIDENTIFIER NOT NULL,
    [requestReason] NVARCHAR(MAX) NOT NULL,
    -- This is an access classification, not indicator consent; the API must enforce it for every case read.
    [requestReasonVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CASES_requestReasonVisibility] DEFAULT N'caseTeam',
    [closedAt] DATETIME2(3) NULL,
    [openedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASES_openedAt] DEFAULT SYSUTCDATETIME(),
    [currentStage] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CASES_currentStage] DEFAULT N'pending',
    [priority] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CASES_priority] DEFAULT N'medium',
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CASES_status] DEFAULT N'open',
    CONSTRAINT [PK_CASES] PRIMARY KEY ([caseId]),
    CONSTRAINT [FK_CASES_advisorId] FOREIGN KEY ([advisorId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_CASES_openedBy] FOREIGN KEY ([openedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_CASES_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[STUDENTPROFILES]([studentId]),
    CONSTRAINT [FK_CASES_subjectId] FOREIGN KEY ([subjectId]) REFERENCES [dbo].[SUBJECTS]([subjectId]),
    CONSTRAINT [FK_CASES_supportCategoryId] FOREIGN KEY ([supportCategoryId]) REFERENCES [dbo].[SUPPORTCATEGORIES]([supportCategoryId]),
    CONSTRAINT [CK_CASES_requestReasonVisibility] CHECK ([requestReasonVisibility] IN (N'assignedAdvisor', N'caseTeam')),
    CONSTRAINT [CK_CASES_currentStage] CHECK ([currentStage] IN (N'pending', N'assigned', N'contacted', N'referred', N'followUp', N'closed')),
    CONSTRAINT [CK_CASES_priority] CHECK ([priority] IN (N'low', N'medium', N'high', N'urgent')),
    CONSTRAINT [CK_CASES_status] CHECK ([status] IN (N'open', N'closed', N'reopened'))
);
GO

-- B2: persistent case-team membership history. Keep ended memberships for audit/history access.
-- The API must write membership rows whenever it adds/removes a case handler.
CREATE TABLE [dbo].[CASETEAMMEMBERS] (
    [caseTeamMemberId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CASETEAMMEMBERS_id] DEFAULT NEWSEQUENTIALID(),
    [caseId] UNIQUEIDENTIFIER NOT NULL,
    [accountId] UNIQUEIDENTIFIER NOT NULL,
    [membershipType] NVARCHAR(30) NOT NULL,
    [joinedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASETEAMMEMBERS_joinedAt] DEFAULT SYSUTCDATETIME(),
    [endedAt] DATETIME2(3) NULL,
    [canViewHistory] BIT NOT NULL CONSTRAINT [DF_CASETEAMMEMBERS_canViewHistory] DEFAULT 1,
    CONSTRAINT [PK_CASETEAMMEMBERS] PRIMARY KEY ([caseTeamMemberId]),
    CONSTRAINT [FK_CASETEAMMEMBERS_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
    CONSTRAINT [FK_CASETEAMMEMBERS_accountId] FOREIGN KEY ([accountId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_CASETEAMMEMBERS_membershipType] CHECK ([membershipType] IN (N'advisor', N'referralSender', N'welfareOfficer', N'escalationSender', N'escalationRecipient', N'other')),
    CONSTRAINT [CK_CASETEAMMEMBERS_dates] CHECK ([endedAt] IS NULL OR [endedAt] >= [joinedAt])
);
GO

-- 23. CASENOTES
CREATE TABLE [dbo].[CASENOTES] (
    [caseNoteId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CASENOTES_caseNoteId] DEFAULT NEWSEQUENTIALID(),
    [authorId] UNIQUEIDENTIFIER NOT NULL,
    [caseId] UNIQUEIDENTIFIER NOT NULL,
    [noteText] NVARCHAR(MAX) NOT NULL,
    -- caseTeam includes authorized current/former case handlers; the API must enforce the audience.
    [visibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CASENOTES_visibility] DEFAULT N'caseTeam',
    -- Referral recipients are granted access through CASENOTEREFERRALSHARES (one specific referral).
    [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASENOTES_createdAt] DEFAULT SYSUTCDATETIME(),
    [editedAt] DATETIME2(3) NULL,
    [isFinalized] BIT NOT NULL CONSTRAINT [DF_CASENOTES_isFinalized] DEFAULT 0,
    CONSTRAINT [PK_CASENOTES] PRIMARY KEY ([caseNoteId]),
    CONSTRAINT [UQ_CASENOTES_note_case] UNIQUE ([caseNoteId], [caseId]),
    CONSTRAINT [FK_CASENOTES_authorId] FOREIGN KEY ([authorId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_CASENOTES_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
    CONSTRAINT [CK_CASENOTES_visibility] CHECK ([visibility] IN (N'assignedAdvisor', N'caseTeam'))
);
GO

-- 24. CASESTAGEHISTORIES
CREATE TABLE [dbo].[CASESTAGEHISTORIES] (
    [caseStageHistoryId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CASESTAGEHISTORIES_caseStageHistoryId] DEFAULT NEWSEQUENTIALID(),
    [caseId] UNIQUEIDENTIFIER NOT NULL,
    [changedBy] UNIQUEIDENTIFIER NOT NULL,
    [reason] NVARCHAR(MAX) NOT NULL,
    -- Access classification for the free-text reason; the API must enforce it.
    [reasonVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CASESTAGEHISTORIES_reasonVisibility] DEFAULT N'caseTeam',
    [changedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASESTAGEHISTORIES_changedAt] DEFAULT SYSUTCDATETIME(),
    [fromStage] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CASESTAGEHISTORIES_fromStage] DEFAULT N'pending',
    [toStage] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CASESTAGEHISTORIES_toStage] DEFAULT N'pending',
    CONSTRAINT [PK_CASESTAGEHISTORIES] PRIMARY KEY ([caseStageHistoryId]),
    CONSTRAINT [FK_CASESTAGEHISTORIES_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
    CONSTRAINT [FK_CASESTAGEHISTORIES_changedBy] FOREIGN KEY ([changedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_CASESTAGEHISTORIES_reasonVisibility] CHECK ([reasonVisibility] IN (N'assignedAdvisor', N'caseTeam')),
    CONSTRAINT [CK_CASESTAGEHISTORIES_fromStage] CHECK ([fromStage] IN (N'pending', N'assigned', N'contacted', N'referred', N'followUp', N'closed')),
    CONSTRAINT [CK_CASESTAGEHISTORIES_toStage] CHECK ([toStage] IN (N'pending', N'assigned', N'contacted', N'referred', N'followUp', N'closed'))
);
GO

-- 25. REFERRALS
CREATE TABLE [dbo].[REFERRALS] (
    [referralId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_REFERRALS_referralId] DEFAULT NEWSEQUENTIALID(),
    [caseId] UNIQUEIDENTIFIER NOT NULL,
    [fromAccountId] UNIQUEIDENTIFIER NOT NULL,
    [toAccountId] UNIQUEIDENTIFIER NOT NULL,
    [referralReason] NVARCHAR(MAX) NOT NULL,
    -- Access classification for the free-text reason; the API must enforce it.
    [referralReasonVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_REFERRALS_referralReasonVisibility] DEFAULT N'caseTeam',
    [acceptedAt] DATETIME2(3) NULL,
    [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_REFERRALS_createdAt] DEFAULT SYSUTCDATETIME(),
    [priority] NVARCHAR(30) NOT NULL CONSTRAINT [DF_REFERRALS_priority] DEFAULT N'medium',
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_REFERRALS_status] DEFAULT N'pending',
    CONSTRAINT [PK_REFERRALS] PRIMARY KEY ([referralId]),
    CONSTRAINT [UQ_REFERRALS_referral_case] UNIQUE ([referralId], [caseId]),
    CONSTRAINT [FK_REFERRALS_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
    CONSTRAINT [FK_REFERRALS_fromAccountId] FOREIGN KEY ([fromAccountId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_REFERRALS_toAccountId] FOREIGN KEY ([toAccountId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_REFERRALS_referralReasonVisibility] CHECK ([referralReasonVisibility] IN (N'assignedAdvisor', N'caseTeam')),
    CONSTRAINT [CK_REFERRALS_priority] CHECK ([priority] IN (N'low', N'medium', N'high', N'urgent')),
    CONSTRAINT [CK_REFERRALS_status] CHECK ([status] IN (N'pending', N'accepted', N'returned'))
);
GO

-- B2: a referral recipient can see only the exact case notes shared with that referral.
CREATE TABLE [dbo].[CASENOTEREFERRALSHARES] (
    [caseNoteId] UNIQUEIDENTIFIER NOT NULL,
    [referralId] UNIQUEIDENTIFIER NOT NULL,
    [caseId] UNIQUEIDENTIFIER NOT NULL,
    [sharedBy] UNIQUEIDENTIFIER NOT NULL,
    [sharedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASENOTEREFERRALSHARES_sharedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_CASENOTEREFERRALSHARES] PRIMARY KEY ([caseNoteId], [referralId]),
    CONSTRAINT [FK_CASENOTEREFERRALSHARES_note_case] FOREIGN KEY ([caseNoteId], [caseId]) REFERENCES [dbo].[CASENOTES]([caseNoteId], [caseId]),
    CONSTRAINT [FK_CASENOTEREFERRALSHARES_referral_case] FOREIGN KEY ([referralId], [caseId]) REFERENCES [dbo].[REFERRALS]([referralId], [caseId]),
    CONSTRAINT [FK_CASENOTEREFERRALSHARES_sharedBy] FOREIGN KEY ([sharedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId])
);
GO

-- 26. CORRECTIONREQUESTS
-- B1.4: currentValue / proposedValue are encrypted with an ATTRIBUTEKEYS key (VARBINARY), never plaintext.
-- The composite FK to ATTRIBUTEKEYS is added after that table exists (see below).
CREATE TABLE [dbo].[CORRECTIONREQUESTS] (
    [correctionRequestId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CORRECTIONREQUESTS_correctionRequestId] DEFAULT NEWSEQUENTIALID(),
    [reviewedBy] UNIQUEIDENTIFIER NULL,
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [dataAttributeId] UNIQUEIDENTIFIER NULL,
    [attributeKeyId] UNIQUEIDENTIFIER NULL,
    [currentValue] VARBINARY(MAX) NULL,
    [currentValueHmac] VARBINARY(32) NULL,
    [decisionNotes] NVARCHAR(MAX) NULL,
    [explanation] NVARCHAR(MAX) NOT NULL,
    -- B2.1: access classification for explanation/decisionNotes; the API must enforce it.
    [textVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CORRECTIONREQUESTS_textVisibility] DEFAULT N'studentAndReviewer',
    [fieldName] NVARCHAR(255) NOT NULL,
    [proposedValue] VARBINARY(MAX) NULL,
    [proposedValueHmac] VARBINARY(32) NULL,
    [decidedAt] DATETIME2(3) NULL,
    [submittedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CORRECTIONREQUESTS_submittedAt] DEFAULT SYSUTCDATETIME(),
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CORRECTIONREQUESTS_status] DEFAULT N'pending',
    CONSTRAINT [PK_CORRECTIONREQUESTS] PRIMARY KEY ([correctionRequestId]),
    CONSTRAINT [FK_CORRECTIONREQUESTS_reviewedBy] FOREIGN KEY ([reviewedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_CORRECTIONREQUESTS_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[STUDENTPROFILES]([studentId]),
    CONSTRAINT [FK_CORRECTIONREQUESTS_dataAttributeId] FOREIGN KEY ([dataAttributeId]) REFERENCES [dbo].[DATAATTRIBUTES]([dataAttributeId]),
    CONSTRAINT [CK_CORRECTIONREQUESTS_encrypted_values] CHECK (
        (([currentValue] IS NULL AND [currentValueHmac] IS NULL) OR ([currentValue] IS NOT NULL AND [currentValueHmac] IS NOT NULL))
        AND (([proposedValue] IS NULL AND [proposedValueHmac] IS NULL) OR ([proposedValue] IS NOT NULL AND [proposedValueHmac] IS NOT NULL))
        AND (([currentValue] IS NULL AND [proposedValue] IS NULL) OR ([attributeKeyId] IS NOT NULL AND [dataAttributeId] IS NOT NULL))
    ),
    CONSTRAINT [CK_CORRECTIONREQUESTS_textVisibility] CHECK ([textVisibility] IN (N'studentAndReviewer', N'stewardOnly')),
    CONSTRAINT [CK_CORRECTIONREQUESTS_status] CHECK ([status] IN (N'pending', N'approved', N'rejected', N'withdrawn'))
);
GO

-- 27. INTERVENTIONS
CREATE TABLE [dbo].[INTERVENTIONS] (
    [interventionId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_INTERVENTIONS_interventionId] DEFAULT NEWSEQUENTIALID(),
    [caseId] UNIQUEIDENTIFIER NOT NULL,
    [officerId] UNIQUEIDENTIFIER NOT NULL,
    [notes] NVARCHAR(MAX) NULL,
    [outcome] NVARCHAR(MAX) NULL,
    -- Access classification for notes/outcome; the API must enforce it.
    [visibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_INTERVENTIONS_visibility] DEFAULT N'caseTeam',
    [interventionDate] DATETIME2(3) NOT NULL,
    [interventionType] NVARCHAR(30) NOT NULL CONSTRAINT [DF_INTERVENTIONS_interventionType] DEFAULT N'loanArrangement',
    [isClosed] BIT NOT NULL CONSTRAINT [DF_INTERVENTIONS_isClosed] DEFAULT 0,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_INTERVENTIONS_status] DEFAULT N'scheduled',
    CONSTRAINT [PK_INTERVENTIONS] PRIMARY KEY ([interventionId]),
    CONSTRAINT [FK_INTERVENTIONS_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
    CONSTRAINT [FK_INTERVENTIONS_officerId] FOREIGN KEY ([officerId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_INTERVENTIONS_visibility] CHECK ([visibility] IN (N'assignedAdvisor', N'caseTeam')),
    CONSTRAINT [CK_INTERVENTIONS_interventionType] CHECK ([interventionType] IN (N'loanArrangement', N'financialAid', N'bankingPaperwork', N'other')),
    CONSTRAINT [CK_INTERVENTIONS_status] CHECK ([status] IN (N'scheduled', N'inProgress', N'completed', N'cancelled'))
);
GO

-- 28. SUPPORTGRANTS
CREATE TABLE [dbo].[SUPPORTGRANTS] (
    [supportGrantId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SUPPORTGRANTS_supportGrantId] DEFAULT NEWSEQUENTIALID(),
    [interventionId] UNIQUEIDENTIFIER NOT NULL,
    [grantDescription] NVARCHAR(MAX) NULL,
    -- B2.1: access classification for grantDescription; the API must enforce it.
    [grantDescriptionVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_SUPPORTGRANTS_grantDescriptionVisibility] DEFAULT N'caseTeam',
    [grantType] NVARCHAR(255) NOT NULL,
    [issuedAt] DATETIME2(3) NOT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_SUPPORTGRANTS_status] DEFAULT N'issued',
    CONSTRAINT [PK_SUPPORTGRANTS] PRIMARY KEY ([supportGrantId]),
    CONSTRAINT [FK_SUPPORTGRANTS_interventionId] FOREIGN KEY ([interventionId]) REFERENCES [dbo].[INTERVENTIONS]([interventionId]),
    CONSTRAINT [CK_SUPPORTGRANTS_grantDescriptionVisibility] CHECK ([grantDescriptionVisibility] IN (N'assignedAdvisor', N'caseTeam')),
    CONSTRAINT [CK_SUPPORTGRANTS_status] CHECK ([status] IN (N'issued', N'revoked'))
);
GO

-- 29. EXTERNALSERVICES
CREATE TABLE [dbo].[EXTERNALSERVICES] (
    [externalServiceId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_EXTERNALSERVICES_externalServiceId] DEFAULT NEWSEQUENTIALID(),
    [contactInfo] NVARCHAR(MAX) NULL,
    [serviceName] NVARCHAR(255) NOT NULL,
    [serviceType] NVARCHAR(255) NULL,
    [isActive] BIT NOT NULL CONSTRAINT [DF_EXTERNALSERVICES_isActive] DEFAULT 1,
    CONSTRAINT [PK_EXTERNALSERVICES] PRIMARY KEY ([externalServiceId])
);
GO

-- 30. EXTERNALREFERRALS
CREATE TABLE [dbo].[EXTERNALREFERRALS] (
    [externalReferralId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_EXTERNALREFERRALS_externalReferralId] DEFAULT NEWSEQUENTIALID(),
    [caseId] UNIQUEIDENTIFIER NOT NULL,
    [externalServiceId] UNIQUEIDENTIFIER NOT NULL,
    [referredBy] UNIQUEIDENTIFIER NOT NULL,
    [followUpDueAt] DATETIME2(3) NULL,
    [followUpOutcome] NVARCHAR(MAX) NULL,
    -- B2.1: access classification for followUpOutcome; the API must enforce it.
    [followUpOutcomeVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_EXTERNALREFERRALS_followUpOutcomeVisibility] DEFAULT N'caseTeam',
    [referredAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_EXTERNALREFERRALS_referredAt] DEFAULT SYSUTCDATETIME(),
    [isOverdue] BIT NOT NULL CONSTRAINT [DF_EXTERNALREFERRALS_isOverdue] DEFAULT 0,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_EXTERNALREFERRALS_status] DEFAULT N'sent',
    CONSTRAINT [PK_EXTERNALREFERRALS] PRIMARY KEY ([externalReferralId]),
    CONSTRAINT [FK_EXTERNALREFERRALS_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
    CONSTRAINT [FK_EXTERNALREFERRALS_externalServiceId] FOREIGN KEY ([externalServiceId]) REFERENCES [dbo].[EXTERNALSERVICES]([externalServiceId]),
    CONSTRAINT [FK_EXTERNALREFERRALS_referredBy] FOREIGN KEY ([referredBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_EXTERNALREFERRALS_followUpOutcomeVisibility] CHECK ([followUpOutcomeVisibility] IN (N'assignedAdvisor', N'caseTeam')),
    CONSTRAINT [CK_EXTERNALREFERRALS_status] CHECK ([status] IN (N'sent', N'followedUp', N'closed'))
);
GO

-- 31. ESCALATIONS
CREATE TABLE [dbo].[ESCALATIONS] (
    [escalationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ESCALATIONS_escalationId] DEFAULT NEWSEQUENTIALID(),
    [caseId] UNIQUEIDENTIFIER NOT NULL,
    [escalatedBy] UNIQUEIDENTIFIER NOT NULL,
    [escalatedTo] UNIQUEIDENTIFIER NOT NULL,
    [reason] NVARCHAR(MAX) NOT NULL,
    [response] NVARCHAR(MAX) NULL,
    -- Access classification for reason/response; the API must enforce it.
    [visibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ESCALATIONS_visibility] DEFAULT N'caseTeam',
    [escalatedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ESCALATIONS_escalatedAt] DEFAULT SYSUTCDATETIME(),
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ESCALATIONS_status] DEFAULT N'pending',
    CONSTRAINT [PK_ESCALATIONS] PRIMARY KEY ([escalationId]),
    CONSTRAINT [FK_ESCALATIONS_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
    CONSTRAINT [FK_ESCALATIONS_escalatedBy] FOREIGN KEY ([escalatedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ESCALATIONS_escalatedTo] FOREIGN KEY ([escalatedTo]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_ESCALATIONS_visibility] CHECK ([visibility] IN (N'assignedAdvisor', N'caseTeam')),
    CONSTRAINT [CK_ESCALATIONS_status] CHECK ([status] IN (N'pending', N'responded'))
);
GO

-- 33. PURPOSE_ATTRIBUTE
CREATE TABLE [dbo].[PURPOSE_ATTRIBUTE] (
    [consentPurposeId] UNIQUEIDENTIFIER NOT NULL,
    [dataAttributeId] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_PURPOSE_ATTRIBUTE] PRIMARY KEY ([consentPurposeId], [dataAttributeId]),
    CONSTRAINT [FK_PURPOSE_ATTRIBUTE_consentPurposeId] FOREIGN KEY ([consentPurposeId]) REFERENCES [dbo].[CONSENTPURPOSES]([consentPurposeId]),
    CONSTRAINT [FK_PURPOSE_ATTRIBUTE_dataAttributeId] FOREIGN KEY ([dataAttributeId]) REFERENCES [dbo].[DATAATTRIBUTES]([dataAttributeId])
);
GO

-- 34. CONSENTS
CREATE TABLE [dbo].[CONSENTS] (
    [consentId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CONSENTS_consentId] DEFAULT NEWSEQUENTIALID(),
    [consentPurposeId] UNIQUEIDENTIFIER NOT NULL,
    [dataAttributeId] UNIQUEIDENTIFIER NOT NULL,
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [expiresAt] DATETIME2(3) NULL,
    [grantedAt] DATETIME2(3) NOT NULL,
    [withdrawnAt] DATETIME2(3) NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CONSENTS_status] DEFAULT N'granted',
    CONSTRAINT [PK_CONSENTS] PRIMARY KEY ([consentId]),
    -- B1.5: lets ERASUREREQUESTS prove the consent belongs to the same student + attribute as the key.
    CONSTRAINT [UQ_CONSENTS_consentId_studentId_dataAttributeId] UNIQUE ([consentId], [studentId], [dataAttributeId]),
    CONSTRAINT [FK_CONSENTS_consentPurposeId] FOREIGN KEY ([consentPurposeId]) REFERENCES [dbo].[CONSENTPURPOSES]([consentPurposeId]),
    CONSTRAINT [FK_CONSENTS_dataAttributeId] FOREIGN KEY ([dataAttributeId]) REFERENCES [dbo].[DATAATTRIBUTES]([dataAttributeId]),
    CONSTRAINT [FK_CONSENTS_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[STUDENTPROFILES]([studentId]),
    CONSTRAINT [CK_CONSENTS_status] CHECK ([status] IN (N'granted', N'withdrawn'))
);
GO

-- 35. CONSENTHISTORIES
CREATE TABLE [dbo].[CONSENTHISTORIES] (
    [consentHistoryId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CONSENTHISTORIES_consentHistoryId] DEFAULT NEWSEQUENTIALID(),
    [changedBy] UNIQUEIDENTIFIER NOT NULL,
    [consentId] UNIQUEIDENTIFIER NOT NULL,
    [changedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CONSENTHISTORIES_changedAt] DEFAULT SYSUTCDATETIME(),
    [newStatus] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CONSENTHISTORIES_newStatus] DEFAULT N'granted',
    [previousStatus] NVARCHAR(30) NULL,
    CONSTRAINT [PK_CONSENTHISTORIES] PRIMARY KEY ([consentHistoryId]),
    CONSTRAINT [FK_CONSENTHISTORIES_changedBy] FOREIGN KEY ([changedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_CONSENTHISTORIES_consentId] FOREIGN KEY ([consentId]) REFERENCES [dbo].[CONSENTS]([consentId]),
    CONSTRAINT [CK_CONSENTHISTORIES_newStatus] CHECK ([newStatus] IN (N'granted', N'withdrawn')),
    CONSTRAINT [CK_CONSENTHISTORIES_previousStatus] CHECK ([previousStatus] IN (N'granted', N'withdrawn'))
);
GO

-- 36. CONSENTREQUESTS
CREATE TABLE [dbo].[CONSENTREQUESTS] (
    [consentRequestId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CONSENTREQUESTS_consentRequestId] DEFAULT NEWSEQUENTIALID(),
    [advisorId] UNIQUEIDENTIFIER NOT NULL,
    [consentPurposeId] UNIQUEIDENTIFIER NOT NULL,
    [dataAttributeId] UNIQUEIDENTIFIER NOT NULL,
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [purposeStated] NVARCHAR(MAX) NOT NULL,
    [requestedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CONSENTREQUESTS_requestedAt] DEFAULT SYSUTCDATETIME(),
    [respondedAt] DATETIME2(3) NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_CONSENTREQUESTS_status] DEFAULT N'pending',
    CONSTRAINT [PK_CONSENTREQUESTS] PRIMARY KEY ([consentRequestId]),
    CONSTRAINT [FK_CONSENTREQUESTS_advisorId] FOREIGN KEY ([advisorId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_CONSENTREQUESTS_consentPurposeId] FOREIGN KEY ([consentPurposeId]) REFERENCES [dbo].[CONSENTPURPOSES]([consentPurposeId]),
    CONSTRAINT [FK_CONSENTREQUESTS_dataAttributeId] FOREIGN KEY ([dataAttributeId]) REFERENCES [dbo].[DATAATTRIBUTES]([dataAttributeId]),
    CONSTRAINT [FK_CONSENTREQUESTS_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[STUDENTPROFILES]([studentId]),
    CONSTRAINT [CK_CONSENTREQUESTS_status] CHECK ([status] IN (N'pending', N'granted', N'denied', N'withdrawn'))
);
GO

-- 37. ACCESSPOLICIES
CREATE TABLE [dbo].[ACCESSPOLICIES] (
    [accessPolicyId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ACCESSPOLICIES_accessPolicyId] DEFAULT NEWSEQUENTIALID(),
    [consentPurposeId] UNIQUEIDENTIFIER NOT NULL,
    [createdBy] UNIQUEIDENTIFIER NOT NULL,
    [roleId] UNIQUEIDENTIFIER NOT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ACCESSPOLICIES_status] DEFAULT N'draft',
    CONSTRAINT [PK_ACCESSPOLICIES] PRIMARY KEY ([accessPolicyId]),
    CONSTRAINT [FK_ACCESSPOLICIES_consentPurposeId] FOREIGN KEY ([consentPurposeId]) REFERENCES [dbo].[CONSENTPURPOSES]([consentPurposeId]),
    CONSTRAINT [FK_ACCESSPOLICIES_createdBy] FOREIGN KEY ([createdBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ACCESSPOLICIES_roleId] FOREIGN KEY ([roleId]) REFERENCES [dbo].[ROLES]([roleId]),
    CONSTRAINT [CK_ACCESSPOLICIES_status] CHECK ([status] IN (N'draft', N'active', N'revoked'))
);
GO

-- 38. ACCESSLOGS
CREATE TABLE [dbo].[ACCESSLOGS] (
    [accessLogId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ACCESSLOGS_accessLogId] DEFAULT NEWSEQUENTIALID(),
    [accessorId] UNIQUEIDENTIFIER NOT NULL,
    [consentPurposeId] UNIQUEIDENTIFIER NOT NULL,
    [recordedUserId] UNIQUEIDENTIFIER NOT NULL,
    [accessedResource] NVARCHAR(255) NOT NULL,
    [accessedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ACCESSLOGS_accessedAt] DEFAULT SYSUTCDATETIME(),
    [isFlagged] BIT NOT NULL CONSTRAINT [DF_ACCESSLOGS_isFlagged] DEFAULT 0,
    [status] NVARCHAR(30) NULL,
    CONSTRAINT [PK_ACCESSLOGS] PRIMARY KEY ([accessLogId]),
    CONSTRAINT [FK_ACCESSLOGS_accessorId] FOREIGN KEY ([accessorId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_ACCESSLOGS_consentPurposeId] FOREIGN KEY ([consentPurposeId]) REFERENCES [dbo].[CONSENTPURPOSES]([consentPurposeId]),
    CONSTRAINT [FK_ACCESSLOGS_recordedUserId] FOREIGN KEY ([recordedUserId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_ACCESSLOGS_status] CHECK ([status] IN (N'unreviewed', N'cleared', N'confirmedIssue'))
);
GO

-- 39. EMERGENCYACCESSREQUESTS
CREATE TABLE [dbo].[EMERGENCYACCESSREQUESTS] (
    [emergencyAccessRequestId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_EMERGENCYACCESSREQUESTS_emergencyAccessRequestId] DEFAULT NEWSEQUENTIALID(),
    [requestedBy] UNIQUEIDENTIFIER NOT NULL,
    [stewardId] UNIQUEIDENTIFIER NULL,
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [justification] NVARCHAR(MAX) NOT NULL,
    -- B2.1: access classification for justification; the API must enforce it.
    [justificationVisibility] NVARCHAR(30) NOT NULL CONSTRAINT [DF_EMERGENCYACCESSREQUESTS_justificationVisibility] DEFAULT N'requesterAndSteward',
    [requestedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_EMERGENCYACCESSREQUESTS_requestedAt] DEFAULT SYSUTCDATETIME(),
    [decidedAt] DATETIME2(3) NULL,
    [scopeExpiresAt] DATETIME2(3) NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_EMERGENCYACCESSREQUESTS_status] DEFAULT N'pending',
    CONSTRAINT [PK_EMERGENCYACCESSREQUESTS] PRIMARY KEY ([emergencyAccessRequestId]),
    CONSTRAINT [FK_EMERGENCYACCESSREQUESTS_requestedBy] FOREIGN KEY ([requestedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_EMERGENCYACCESSREQUESTS_stewardId] FOREIGN KEY ([stewardId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_EMERGENCYACCESSREQUESTS_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[STUDENTPROFILES]([studentId]),
    CONSTRAINT [CK_EMERGENCYACCESSREQUESTS_justificationVisibility] CHECK ([justificationVisibility] IN (N'requesterAndSteward', N'stewardOnly')),
    CONSTRAINT [CK_EMERGENCYACCESSREQUESTS_status] CHECK ([status] IN (N'pending', N'approved', N'rejected'))
);
GO

-- 41. ATTRIBUTEKEYS
CREATE TABLE [dbo].[ATTRIBUTEKEYS] (
    [attributeKeyId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ATTRIBUTEKEYS_attributeKeyId] DEFAULT NEWSEQUENTIALID(),
    [dataAttributeId] UNIQUEIDENTIFIER NOT NULL,
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    -- Envelope-encrypted data key. The KMS key reference must identify a per-attribute destroyable key.
    [wrappedKey] NVARCHAR(MAX) NULL,
    [keyManagementKeyId] NVARCHAR(255) NOT NULL,
    [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ATTRIBUTEKEYS_createdAt] DEFAULT SYSUTCDATETIME(),
    [destroyedAt] DATETIME2(3) NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ATTRIBUTEKEYS_status] DEFAULT N'active',
    CONSTRAINT [PK_ATTRIBUTEKEYS] PRIMARY KEY ([attributeKeyId]),
    -- B1.3: referenced by composite FKs so a key can only be used for its own student + attribute.
    CONSTRAINT [UQ_ATTRIBUTEKEYS_key_student_attribute] UNIQUE ([attributeKeyId], [studentId], [dataAttributeId]),
    CONSTRAINT [FK_ATTRIBUTEKEYS_dataAttributeId] FOREIGN KEY ([dataAttributeId]) REFERENCES [dbo].[DATAATTRIBUTES]([dataAttributeId]),
    CONSTRAINT [FK_ATTRIBUTEKEYS_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[STUDENTPROFILES]([studentId]),
    CONSTRAINT [CK_ATTRIBUTEKEYS_status] CHECK (
        ([status] IN (N'active', N'pendingKmsDestruction') AND [wrappedKey] IS NOT NULL AND [destroyedAt] IS NULL)
        OR ([status] = N'destroyed' AND [wrappedKey] IS NULL AND [destroyedAt] IS NOT NULL)
    )
);
GO

-- B1.3: an encrypted indicator row must use a key of the same student AND the same data attribute.
-- (attributeKeyId is NULL for missing rows, so the FK is simply not checked for them.)
ALTER TABLE [dbo].[STUDENTINDICATORS]
ADD CONSTRAINT [FK_STUDENTINDICATORS_key_student_attribute]
    FOREIGN KEY ([attributeKeyId], [studentId], [dataAttributeId])
    REFERENCES [dbo].[ATTRIBUTEKEYS]([attributeKeyId], [studentId], [dataAttributeId]);
GO

-- B1.4: encrypted correction values must use a key of the same student and attribute.
ALTER TABLE [dbo].[CORRECTIONREQUESTS]
ADD CONSTRAINT [FK_CORRECTIONREQUESTS_key_student_attribute]
    FOREIGN KEY ([attributeKeyId], [studentId], [dataAttributeId])
    REFERENCES [dbo].[ATTRIBUTEKEYS]([attributeKeyId], [studentId], [dataAttributeId]);
GO

-- 40. ERASUREREQUESTS
CREATE TABLE [dbo].[ERASUREREQUESTS] (
    [erasureRequestId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ERASUREREQUESTS_erasureRequestId] DEFAULT NEWSEQUENTIALID(),
    [attributeKeyId] UNIQUEIDENTIFIER NOT NULL,
    [consentId] UNIQUEIDENTIFIER NOT NULL,
    -- B1.5: student + attribute are repeated here so both FKs below can prove key and consent match.
    [studentId] UNIQUEIDENTIFIER NOT NULL,
    [dataAttributeId] UNIQUEIDENTIFIER NOT NULL,
    [executedBy] UNIQUEIDENTIFIER NULL,
    [executedAt] DATETIME2(3) NULL,
    [requestedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ERASUREREQUESTS_requestedAt] DEFAULT SYSUTCDATETIME(),
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ERASUREREQUESTS_status] DEFAULT N'pending',
    CONSTRAINT [PK_ERASUREREQUESTS] PRIMARY KEY ([erasureRequestId]),
    CONSTRAINT [FK_ERASUREREQUESTS_key_student_attribute] FOREIGN KEY ([attributeKeyId], [studentId], [dataAttributeId]) REFERENCES [dbo].[ATTRIBUTEKEYS]([attributeKeyId], [studentId], [dataAttributeId]),
    CONSTRAINT [FK_ERASUREREQUESTS_consent_student_attribute] FOREIGN KEY ([consentId], [studentId], [dataAttributeId]) REFERENCES [dbo].[CONSENTS]([consentId], [studentId], [dataAttributeId]),
    CONSTRAINT [FK_ERASUREREQUESTS_executedBy] FOREIGN KEY ([executedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_ERASUREREQUESTS_status] CHECK ([status] IN (N'pending', N'pendingKeyDestruction', N'completed'))
);
GO

-- 42. REPORTS
CREATE TABLE [dbo].[REPORTS] (
    [reportId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_REPORTS_reportId] DEFAULT NEWSEQUENTIALID(),
    [generatedBy] UNIQUEIDENTIFIER NOT NULL,
    [termId] UNIQUEIDENTIFIER NOT NULL,
    [filePath] NVARCHAR(MAX) NULL,
    [reportType] NVARCHAR(30) NOT NULL CONSTRAINT [DF_REPORTS_reportType] DEFAULT N'caseReport',
    [generatedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_REPORTS_generatedAt] DEFAULT SYSUTCDATETIME(),
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_REPORTS_status] DEFAULT N'generating',
    CONSTRAINT [PK_REPORTS] PRIMARY KEY ([reportId]),
    CONSTRAINT [FK_REPORTS_generatedBy] FOREIGN KEY ([generatedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [FK_REPORTS_termId] FOREIGN KEY ([termId]) REFERENCES [dbo].[ACADEMICTERMS]([termId]),
    CONSTRAINT [CK_REPORTS_reportType] CHECK ([reportType] IN (N'caseReport', N'caseloadClosure', N'welfareStatistics', N'processingRecord', N'studentRecordExport')),
    CONSTRAINT [CK_REPORTS_status] CHECK ([status] IN (N'generating', N'ready', N'failed'))
);
GO

-- 43. BACKUPS
CREATE TABLE [dbo].[BACKUPS] (
    [backupId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_BACKUPS_backupId] DEFAULT NEWSEQUENTIALID(),
    [createdBy] UNIQUEIDENTIFIER NULL,
    [backupTimestamp] DATETIME2(3) NOT NULL CONSTRAINT [DF_BACKUPS_backupTimestamp] DEFAULT SYSUTCDATETIME(),
    [sizeBytes] BIGINT NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_BACKUPS_status] DEFAULT N'completed',
    CONSTRAINT [PK_BACKUPS] PRIMARY KEY ([backupId]),
    CONSTRAINT [FK_BACKUPS_createdBy] FOREIGN KEY ([createdBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_BACKUPS_status] CHECK ([status] IN (N'completed', N'failed'))
);
GO

-- 44. RESTOREOPERATIONS
CREATE TABLE [dbo].[RESTOREOPERATIONS] (
    [restoreOperationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_RESTOREOPERATIONS_restoreOperationId] DEFAULT NEWSEQUENTIALID(),
    [backupId] UNIQUEIDENTIFIER NOT NULL,
    [executedBy] UNIQUEIDENTIFIER NOT NULL,
    [integrityCheckResult] NVARCHAR(MAX) NULL,
    [executedAt] DATETIME2(3) NULL,
    [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_RESTOREOPERATIONS_status] DEFAULT N'running',
    CONSTRAINT [PK_RESTOREOPERATIONS] PRIMARY KEY ([restoreOperationId]),
    CONSTRAINT [FK_RESTOREOPERATIONS_backupId] FOREIGN KEY ([backupId]) REFERENCES [dbo].[BACKUPS]([backupId]),
    CONSTRAINT [FK_RESTOREOPERATIONS_executedBy] FOREIGN KEY ([executedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
    CONSTRAINT [CK_RESTOREOPERATIONS_status] CHECK ([status] IN (N'running', N'verified', N'failed'))
);
GO

/* ---------- Extra indexes ---------- */
SET QUOTED_IDENTIFIER ON;
-- googleId must be unique once linked (NULL allowed many times)
CREATE UNIQUE INDEX [UX_ACCOUNTS_googleId] ON [dbo].[ACCOUNTS]([googleId]) WHERE [googleId] IS NOT NULL;
-- only one academic term can be the current one
CREATE UNIQUE INDEX [UX_ACADEMICTERMS_status_current] ON [dbo].[ACADEMICTERMS]([status]) WHERE [status] = N'current';
-- student codes are unique when supplied; students not yet assigned a code may have none
CREATE UNIQUE INDEX [UX_STUDENTPROFILES_studentCode] ON [dbo].[STUDENTPROFILES]([studentCode]) WHERE [studentCode] IS NOT NULL;
CREATE INDEX [IX_STUDENTPROFILES_programme_status] ON [dbo].[STUDENTPROFILES]([programmeId], [academicStatus]);
-- only one unfinished erasure workflow may target a key at a time
CREATE UNIQUE INDEX [UX_ERASUREREQUESTS_key_unfinished] ON [dbo].[ERASUREREQUESTS]([attributeKeyId]) WHERE [status] <> N'completed';
-- case-team membership history lookup
CREATE INDEX [IX_CASETEAMMEMBERS_case_account_history] ON [dbo].[CASETEAMMEMBERS]([caseId], [accountId], [canViewHistory]);
-- referral note grants lookup
CREATE INDEX [IX_CASENOTEREFERRALSHARES_referral] ON [dbo].[CASENOTEREFERRALSHARES]([referralId], [caseNoteId]);
-- one active key per student per attribute (a new key may be created after an erasure if the student shares again)
CREATE UNIQUE INDEX [UX_ATTRIBUTEKEYS_student_attribute_active] ON [dbo].[ATTRIBUTEKEYS]([studentId], [dataAttributeId]) WHERE [status] = N'active';
-- one granted consent per student, purpose and attribute (withdrawn rows stay as history)
CREATE UNIQUE INDEX [UX_CONSENTS_student_purpose_attribute_granted] ON [dbo].[CONSENTS]([studentId], [consentPurposeId], [dataAttributeId]) WHERE [status] = N'granted';
-- one live (draft or active) access policy per role and purpose; revoked policies stay as history
CREATE UNIQUE INDEX [UX_ACCESSPOLICIES_role_purpose_live] ON [dbo].[ACCESSPOLICIES]([roleId], [consentPurposeId]) WHERE [status] IN (N'draft', N'active');
-- fast lookup of the next row for point-in-time reconstruction
CREATE INDEX [IX_STUDENTINDICATORS_student_indicator_time] ON [dbo].[STUDENTINDICATORS]([studentId], [indicatorId], [transactionFrom], [validFrom]);
-- find every retained ciphertext protected by a key during an erasure audit
CREATE INDEX [IX_STUDENTINDICATORS_attributeKeyId_studentId] ON [dbo].[STUDENTINDICATORS]([attributeKeyId], [studentId]) WHERE [attributeKeyId] IS NOT NULL;
-- find every encrypted correction value protected by a key during an erasure audit
CREATE INDEX [IX_CORRECTIONREQUESTS_attributeKeyId_studentId] ON [dbo].[CORRECTIONREQUESTS]([attributeKeyId], [studentId]) WHERE [attributeKeyId] IS NOT NULL;
-- find the programme (and its head) quickly
CREATE INDEX [IX_PROGRAMMES_programmeHeadId] ON [dbo].[PROGRAMMES]([programmeHeadId]) WHERE [programmeHeadId] IS NOT NULL;
GO

/* ---------- 4. Seed data ---------- */
-- All 7 accounts use password 123456 (BCrypt hash, same format as BCrypt.Net-Next HashPassword).
-- For local development only: delete or change these accounts before any real deployment.
INSERT INTO [dbo].[ROLES] ([roleName]) VALUES
    (N'System Administrator'),
    (N'Data Protection Steward'),
    (N'Academic Advisor'),
    (N'Wellbeing Advisor'),
    (N'Student Welfare Officer'),
    (N'Student'),
    (N'Programme Head');
GO

INSERT INTO [dbo].[ACCOUNTS] ([email], [fullName], [userName], [passwordHash], [status]) VALUES
    (N'admin@gmail.com', N'System Administrator', N'admin', N'$2a$11$1liYqIr7kcDxTzoTN59b6us3o6m6EMaya6FZ9tjsDN3g0231ERA7m', N'active'),
    (N'steward@gmail.com', N'Data Protection Steward', N'steward', N'$2a$11$lTeQom.w4NNhqDBbm.GcteEf7ZxdV6ZLH5Y2Zx0lxAdc/abs8DAjG', N'active'),
    (N'academicadvisor@gmail.com', N'Academic Advisor', N'academicadvisor', N'$2a$11$.M/cKreFct0akjeuyMIGj.eyuHnZ9D8C5anorpNHe0m2pU9LLdwnO', N'active'),
    (N'wellbeingadvisor@gmail.com', N'Wellbeing Advisor', N'wellbeingadvisor', N'$2a$11$1wgRiVlejGNcTh6I9RWE4O2L6wIVFs0Hy3ClgJULcTRTmHpxvCeKW', N'active'),
    (N'welfareofficer@gmail.com', N'Student Welfare Officer', N'welfareofficer', N'$2a$11$QPB3eTzt1Wz/1cc9QVRxBeEASLzFmLvGFEt5hmf8GMs78PZUVabBW', N'active'),
    (N'student@gmail.com', N'Student', N'student', N'$2a$11$1qow.v9rCZj8gaUToiAd8.VTPgfzBFEzWw6SK6vHa.kWx9LPPmzw.', N'active'),
    (N'programmehead@gmail.com', N'Programme Head', N'programmehead', N'$2a$11$L.qbXWklG.R4RMzNMa25c.v.LHdbMdE244vgFogVXfFQCBxcvMSRu', N'active');
GO

INSERT INTO [dbo].[ACCOUNT_ROLE] ([accountId], [roleId])
SELECT a.[accountId], r.[roleId]
FROM [dbo].[ACCOUNTS] a
JOIN [dbo].[ROLES] r ON r.[roleName] = CASE a.[userName]
    WHEN N'admin' THEN N'System Administrator'
    WHEN N'steward' THEN N'Data Protection Steward'
    WHEN N'academicadvisor' THEN N'Academic Advisor'
    WHEN N'wellbeingadvisor' THEN N'Wellbeing Advisor'
    WHEN N'welfareofficer' THEN N'Student Welfare Officer'
    WHEN N'student' THEN N'Student'
    WHEN N'programmehead' THEN N'Programme Head'
END;
GO

-- Test faculty + programme (headed by the test Programme Head) + profile for the test student,
-- so student-code import and programme-head routing can be tried right away.
-- Must run after ACCOUNT_ROLE is seeded because the triggers check roles.
INSERT INTO [dbo].[FACULTIES] ([facultyCode], [facultyName]) VALUES (N'FIT', N'Faculty of Information Technology');
GO

INSERT INTO [dbo].[PROGRAMMES] ([facultyId], [programmeHeadId], [programmeCode], [programmeName])
SELECT f.[facultyId], a.[accountId], N'SE', N'Software Engineering'
FROM [dbo].[FACULTIES] f
CROSS JOIN [dbo].[ACCOUNTS] a
WHERE f.[facultyCode] = N'FIT' AND a.[userName] = N'programmehead';
GO

INSERT INTO [dbo].[STUDENTPROFILES] ([studentId], [studentCode], [programmeId], [academicStatus])
SELECT a.[accountId], N'SV000001', p.[programmeId], N'enrolled'
FROM [dbo].[ACCOUNTS] a
CROSS JOIN [dbo].[PROGRAMMES] p
WHERE a.[userName] = N'student' AND p.[programmeCode] = N'SE';
GO

/* ---------- 5. Procedures, views and row-level security ---------- */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- PHASE 1: erase all ciphertext and HMAC fingerprints in the live database.
-- The data key becomes pendingKmsDestruction; it is NOT declared destroyed yet.
-- The API must then permanently destroy the per-attribute key in the external KMS/HSM.
-- Only after the KMS confirms destruction may the API call usp_ConfirmKmsKeyDestruction.
CREATE OR ALTER PROCEDURE [dbo].[usp_ExecuteErasure]
    @erasureRequestId UNIQUEIDENTIFIER,
    @executedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;

    DECLARE @key UNIQUEIDENTIFIER;
    DECLARE @consent UNIQUEIDENTIFIER;
    DECLARE @kmsKeyId NVARCHAR(255);

    SELECT @key = er.[attributeKeyId], @consent = er.[consentId]
    FROM [dbo].[ERASUREREQUESTS] er WITH (UPDLOCK, HOLDLOCK)
    JOIN [dbo].[CONSENTS] c WITH (UPDLOCK, HOLDLOCK)
      ON c.[consentId] = er.[consentId]
     AND c.[studentId] = er.[studentId]
     AND c.[dataAttributeId] = er.[dataAttributeId]
    JOIN [dbo].[ATTRIBUTEKEYS] ak WITH (UPDLOCK, HOLDLOCK)
      ON ak.[attributeKeyId] = er.[attributeKeyId]
     AND ak.[studentId] = er.[studentId]
     AND ak.[dataAttributeId] = er.[dataAttributeId]
    WHERE er.[erasureRequestId] = @erasureRequestId
      AND er.[status] = N'pending'
      AND c.[status] = N'withdrawn'
      AND ak.[status] = N'active';

    IF @key IS NULL
    BEGIN
        ROLLBACK;
        THROW 50010, N'Erasure request must be pending, its consent must be withdrawn, and its key must be active.', 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[ACCOUNT_ROLE] ar
        JOIN [dbo].[ROLES] r ON r.[roleId] = ar.[roleId]
        WHERE ar.[accountId] = @executedBy
          AND r.[roleName] IN (N'Data Protection Steward', N'System Administrator')
    )
    BEGIN
        ROLLBACK;
        THROW 50011, N'Only a Data Protection Steward or System Administrator may execute erasure.', 1;
    END;

    SELECT @kmsKeyId = [keyManagementKeyId]
    FROM [dbo].[ATTRIBUTEKEYS]
    WHERE [attributeKeyId] = @key;

    UPDATE [dbo].[STUDENTINDICATORS]
       SET [indicatorValue] = NULL,
           [valueHmac] = NULL,
           [erasedAt] = SYSUTCDATETIME()
     WHERE [attributeKeyId] = @key AND [erasedAt] IS NULL AND [isMissing] = 0;

    UPDATE [dbo].[CORRECTIONREQUESTS]
       SET [currentValue] = NULL,
           [currentValueHmac] = NULL,
           [proposedValue] = NULL,
           [proposedValueHmac] = NULL
     WHERE [attributeKeyId] = @key;

    UPDATE [dbo].[ATTRIBUTEKEYS]
       SET [status] = N'pendingKmsDestruction'
     WHERE [attributeKeyId] = @key AND [status] = N'active';

    UPDATE [dbo].[ERASUREREQUESTS]
       SET [status] = N'pendingKeyDestruction',
           [executedBy] = @executedBy,
           [executedAt] = SYSUTCDATETIME()
     WHERE [erasureRequestId] = @erasureRequestId;

    COMMIT;

    -- The API uses this reference to destroy the key in KMS/HSM, then calls the confirmation procedure.
    SELECT @key AS [attributeKeyId], @kmsKeyId AS [keyManagementKeyId], N'pendingKmsDestruction' AS [nextStep];
END;
GO

-- PHASE 2: call only after the external KMS/HSM confirms permanent destruction of this exact key.
-- This procedure cannot call or independently verify an external KMS; restrict EXECUTE permission
-- to the trusted backend service and record the KMS confirmation in its audit log.
CREATE OR ALTER PROCEDURE [dbo].[usp_ConfirmKmsKeyDestruction]
    @erasureRequestId UNIQUEIDENTIFIER,
    @keyManagementKeyId NVARCHAR(255),
    @executedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;

    DECLARE @key UNIQUEIDENTIFIER;
    SELECT @key = er.[attributeKeyId]
    FROM [dbo].[ERASUREREQUESTS] er WITH (UPDLOCK, HOLDLOCK)
    JOIN [dbo].[ATTRIBUTEKEYS] ak WITH (UPDLOCK, HOLDLOCK)
      ON ak.[attributeKeyId] = er.[attributeKeyId]
    WHERE er.[erasureRequestId] = @erasureRequestId
      AND er.[status] = N'pendingKeyDestruction'
      AND ak.[status] = N'pendingKmsDestruction'
      AND ak.[keyManagementKeyId] = @keyManagementKeyId;

    IF @key IS NULL
    BEGIN
        ROLLBACK;
        THROW 50012, N'No matching erasure workflow is awaiting KMS destruction confirmation.', 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[ACCOUNT_ROLE] ar
        JOIN [dbo].[ROLES] r ON r.[roleId] = ar.[roleId]
        WHERE ar.[accountId] = @executedBy
          AND r.[roleName] IN (N'Data Protection Steward', N'System Administrator')
    )
    BEGIN
        ROLLBACK;
        THROW 50013, N'Only a Data Protection Steward or System Administrator may confirm erasure.', 1;
    END;

    UPDATE [dbo].[ATTRIBUTEKEYS]
       SET [wrappedKey] = NULL,
           [destroyedAt] = SYSUTCDATETIME(),
           [status] = N'destroyed'
     WHERE [attributeKeyId] = @key;

    UPDATE [dbo].[ERASUREREQUESTS]
       SET [status] = N'completed',
           [executedBy] = @executedBy,
           [executedAt] = SYSUTCDATETIME()
     WHERE [erasureRequestId] = @erasureRequestId;

    COMMIT;
END;
GO

-- Membership view: referral recipients are deliberately NOT included as general case-team members.
-- They receive only the notes explicitly linked to their accepted referral in CASENOTEREFERRALSHARES.
-- CASETEAMMEMBERS preserves former handlers; the API must insert/close membership rows as assignments change.
CREATE OR ALTER VIEW [dbo].[VW_CASETEAM] AS
SELECT [caseId], [advisorId] AS [accountId] FROM [dbo].[CASES] WHERE [advisorId] IS NOT NULL
UNION SELECT [caseId], [fromAccountId] FROM [dbo].[REFERRALS]
UNION SELECT [caseId], [officerId] FROM [dbo].[INTERVENTIONS]
UNION SELECT [caseId], [escalatedBy] FROM [dbo].[ESCALATIONS]
UNION SELECT [caseId], [escalatedTo] FROM [dbo].[ESCALATIONS]
UNION SELECT [caseId], [accountId] FROM [dbo].[CASETEAMMEMBERS] WHERE [canViewHistory] = 1;
GO

-- Row-level security for case notes. Set SESSION_CONTEXT('AccountId') on every pooled DB connection
-- before querying; clear/reset it before returning the connection to the pool. The application DB
-- principal must NOT be db_owner/sysadmin, since privileged principals can bypass RLS.
CREATE OR ALTER FUNCTION [dbo].[fn_CASENOTES_accessPredicate]
(
    @caseNoteId UNIQUEIDENTIFIER,
    @caseId UNIQUEIDENTIFIER,
    @authorId UNIQUEIDENTIFIER,
    @visibility NVARCHAR(30)
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS [accessResult]
    WHERE TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId')) IS NOT NULL
      AND
      (
        @authorId = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId'))
        OR
        (
          @visibility = N'assignedAdvisor'
          AND EXISTS
          (
            SELECT 1 FROM [dbo].[CASES] c
            WHERE c.[caseId] = @caseId
              AND c.[advisorId] = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId'))
          )
        )
        OR
        (
          @visibility = N'caseTeam'
          AND
          (
            EXISTS
            (
              SELECT 1 FROM [dbo].[CASETEAMMEMBERS] m
              WHERE m.[caseId] = @caseId
                AND m.[accountId] = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId'))
                AND m.[canViewHistory] = 1
            )
            OR EXISTS (SELECT 1 FROM [dbo].[CASES] c WHERE c.[caseId] = @caseId AND c.[advisorId] = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId')))
            OR EXISTS (SELECT 1 FROM [dbo].[REFERRALS] r WHERE r.[caseId] = @caseId AND r.[fromAccountId] = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId')))
            OR EXISTS (SELECT 1 FROM [dbo].[INTERVENTIONS] i WHERE i.[caseId] = @caseId AND i.[officerId] = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId')))
            OR EXISTS (SELECT 1 FROM [dbo].[ESCALATIONS] e WHERE e.[caseId] = @caseId AND (e.[escalatedBy] = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId')) OR e.[escalatedTo] = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId'))))
          )
        )
        OR EXISTS
        (
          SELECT 1
          FROM [dbo].[CASENOTEREFERRALSHARES] s
          JOIN [dbo].[REFERRALS] r ON r.[referralId] = s.[referralId] AND r.[caseId] = s.[caseId]
          WHERE s.[caseNoteId] = @caseNoteId
            AND s.[caseId] = @caseId
            AND r.[status] = N'accepted'
            AND r.[toAccountId] = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId'))
        )
      )
);
GO

-- Write predicate: only the signed-in author who is an authorized case handler may create/edit a note.
CREATE OR ALTER FUNCTION [dbo].[fn_CASENOTES_writePredicate]
(
    @caseId UNIQUEIDENTIFIER,
    @authorId UNIQUEIDENTIFIER
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS [writeResult]
    WHERE @authorId = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId'))
      AND TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'AccountId')) IS NOT NULL
      AND
      (
        EXISTS (SELECT 1 FROM [dbo].[CASES] c WHERE c.[caseId] = @caseId AND c.[advisorId] = @authorId)
        OR EXISTS (SELECT 1 FROM [dbo].[CASETEAMMEMBERS] m WHERE m.[caseId] = @caseId AND m.[accountId] = @authorId AND m.[canViewHistory] = 1)
        OR EXISTS (SELECT 1 FROM [dbo].[REFERRALS] r WHERE r.[caseId] = @caseId AND r.[fromAccountId] = @authorId)
        OR EXISTS (SELECT 1 FROM [dbo].[INTERVENTIONS] i WHERE i.[caseId] = @caseId AND i.[officerId] = @authorId)
        OR EXISTS (SELECT 1 FROM [dbo].[ESCALATIONS] e WHERE e.[caseId] = @caseId AND (e.[escalatedBy] = @authorId OR e.[escalatedTo] = @authorId))
      )
);
GO

CREATE SECURITY POLICY [dbo].[POLICY_CASENOTES_access]
ADD FILTER PREDICATE [dbo].[fn_CASENOTES_accessPredicate]([caseNoteId], [caseId], [authorId], [visibility])
    ON [dbo].[CASENOTES],
ADD BLOCK PREDICATE [dbo].[fn_CASENOTES_writePredicate]([caseId], [authorId])
    ON [dbo].[CASENOTES] AFTER INSERT,
ADD BLOCK PREDICATE [dbo].[fn_CASENOTES_writePredicate]([caseId], [authorId])
    ON [dbo].[CASENOTES] BEFORE UPDATE,
-- BEFORE UPDATE checks the old row; AFTER UPDATE also checks the new row so authorId cannot be spoofed.
ADD BLOCK PREDICATE [dbo].[fn_CASENOTES_writePredicate]([caseId], [authorId])
    ON [dbo].[CASENOTES] AFTER UPDATE,
ADD BLOCK PREDICATE [dbo].[fn_CASENOTES_writePredicate]([caseId], [authorId])
    ON [dbo].[CASENOTES] BEFORE DELETE
WITH (STATE = ON);
GO

-- Other visibility-tagged free-text columns (e.g. correction explanations, interventions,
-- escalations, emergency-access justifications) still require API authorization. Do not grant
-- the application principal unrestricted direct SELECT access to those base tables.

/* =====================================================================
   V5 SQL-LEVEL HARDENING
   These checks are enforceable in SQL Server and intentionally do not pretend
   to implement external KMS/HSM behavior or the application's full policy engine.
   ===================================================================== */
GO

-- A supplied student code must not be empty/whitespace. The filtered unique index
-- UX_STUDENTPROFILES_studentCode above already ensures uniqueness for non-NULL codes.
ALTER TABLE [dbo].[STUDENTPROFILES]
ADD CONSTRAINT [CK_STUDENTPROFILES_studentCode_nonblank]
CHECK ([studentCode] IS NULL OR LEN(LTRIM(RTRIM([studentCode]))) > 0);
GO

-- Consent status and withdrawal timestamp must agree. The application must update
-- status and withdrawnAt together in one transaction when consent is withdrawn/restored.
ALTER TABLE [dbo].[CONSENTS]
ADD CONSTRAINT [CK_CONSENTS_status_withdrawnAt]
CHECK
(
    ([status] = N'granted' AND [withdrawnAt] IS NULL)
    OR
    ([status] = N'withdrawn' AND [withdrawnAt] IS NOT NULL)
);
GO

-- The KMS/HSM key reference is required for lifecycle operations and must not be blank.
ALTER TABLE [dbo].[ATTRIBUTEKEYS]
ADD CONSTRAINT [CK_ATTRIBUTEKEYS_keyManagementKeyId_nonblank]
CHECK (LEN(LTRIM(RTRIM([keyManagementKeyId]))) > 0);
GO

/* =====================================================================
   ITEMS INTENTIONALLY LEFT FOR BACKEND / TEAM DECISIONS (not DB-only fixes)

   1. Backend must encrypt/decrypt indicator and correction payloads using authenticated
      encryption, derive a dedicated HMAC subkey from each data key, and store only ciphertext in VARBINARY.
   2. Backend must call the external KMS/HSM to destroy the exact per-attribute key and
      call usp_ConfirmKmsKeyDestruction only after a trusted successful confirmation.
      SQL Server cannot independently prove external key destruction or purge every backup.
   3. Team must approve access policy for CASES.requestReason and other free-text columns
      (correction explanations/decision notes, interventions, escalations, emergency access,
      support grants, etc.). Their visibility labels are not a replacement for authorization.
   4. Deployment must use a least-privilege application DB principal, grant EXECUTE only as
      needed, avoid db_owner/sysadmin for the application, set SESSION_CONTEXT('AccountId')
      per connection, and clear/reset it before returning pooled connections.
   5. Test the actual application workflows, multi-role RLS behavior, KMS partial failures,
      and backup/restore retention. This script does not claim those external controls are done.

   SAFETY: this remains a FULL REBUILD script. It drops and recreates AdviseCare tables and
   therefore deletes existing table data when executed. Back up first and run only against
   the intended development/test database unless a planned migration has been approved.
   ===================================================================== */
