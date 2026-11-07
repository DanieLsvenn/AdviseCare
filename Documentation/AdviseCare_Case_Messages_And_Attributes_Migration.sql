-- Brings an existing AdviseCare database up to date without resetting or reseeding its data. Run once before deploying the API; re-running is safe.
-- 1. CASEMESSAGES (24): renames CASEADVISORUPDATES (from AdviseCare_Advisor_Updates_Migration.sql) if it exists, otherwise creates the table.
-- 2. CASE_ATTRIBUTE (21): the data attributes a student shared with a support request, with an optional note each.
--    Cases created before this migration keep their per-attribute notes inside CASES.requestReason; they get no rows here.
USE [AdviseCareDB];
GO

-- 1. CASEMESSAGES
IF OBJECT_ID(N'[dbo].[CASEADVISORUPDATES]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[CASEMESSAGES]', N'U') IS NULL
BEGIN
    EXEC sp_rename N'[dbo].[CASEADVISORUPDATES]', N'CASEMESSAGES';
    EXEC sp_rename N'[dbo].[CASEMESSAGES].[caseAdvisorUpdateId]', N'caseMessageId', N'COLUMN';
    EXEC sp_rename N'[dbo].[PK_CASEADVISORUPDATES]', N'PK_CASEMESSAGES', N'OBJECT';
    EXEC sp_rename N'[dbo].[FK_CASEADVISORUPDATES_authorId]', N'FK_CASEMESSAGES_authorId', N'OBJECT';
    EXEC sp_rename N'[dbo].[FK_CASEADVISORUPDATES_caseId]', N'FK_CASEMESSAGES_caseId', N'OBJECT';
    EXEC sp_rename N'[dbo].[CK_CASEADVISORUPDATES_messageText]', N'CK_CASEMESSAGES_messageText', N'OBJECT';
    EXEC sp_rename N'[dbo].[DF_CASEADVISORUPDATES_caseAdvisorUpdateId]', N'DF_CASEMESSAGES_caseMessageId', N'OBJECT';
    EXEC sp_rename N'[dbo].[DF_CASEADVISORUPDATES_createdAt]', N'DF_CASEMESSAGES_createdAt', N'OBJECT';
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CASEADVISORUPDATES_caseId_createdAt' AND [object_id] = OBJECT_ID(N'[dbo].[CASEMESSAGES]'))
        EXEC sp_rename N'[dbo].[CASEMESSAGES].[IX_CASEADVISORUPDATES_caseId_createdAt]', N'IX_CASEMESSAGES_caseId_createdAt', N'INDEX';
END;
GO

IF OBJECT_ID(N'[dbo].[CASEMESSAGES]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CASEMESSAGES] (
        [caseMessageId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CASEMESSAGES_caseMessageId] DEFAULT NEWSEQUENTIALID(),
        [authorId] UNIQUEIDENTIFIER NOT NULL,
        [caseId] UNIQUEIDENTIFIER NOT NULL,
        [messageText] NVARCHAR(1000) NOT NULL,
        [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASEMESSAGES_createdAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_CASEMESSAGES] PRIMARY KEY ([caseMessageId]),
        CONSTRAINT [FK_CASEMESSAGES_authorId] FOREIGN KEY ([authorId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
        CONSTRAINT [FK_CASEMESSAGES_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
        CONSTRAINT [CK_CASEMESSAGES_messageText] CHECK (LEN(LTRIM(RTRIM([messageText]))) BETWEEN 1 AND 1000)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CASEMESSAGES_caseId_createdAt' AND [object_id] = OBJECT_ID(N'[dbo].[CASEMESSAGES]'))
BEGIN
    CREATE INDEX [IX_CASEMESSAGES_caseId_createdAt]
        ON [dbo].[CASEMESSAGES] ([caseId], [createdAt]);
END;
GO

-- 2. CASE_ATTRIBUTE
IF OBJECT_ID(N'[dbo].[CASE_ATTRIBUTE]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CASE_ATTRIBUTE] (
        [caseId] UNIQUEIDENTIFIER NOT NULL,
        [dataAttributeId] UNIQUEIDENTIFIER NOT NULL,
        [shareReason] NVARCHAR(1000) NULL,
        [sharedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASE_ATTRIBUTE_sharedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_CASE_ATTRIBUTE] PRIMARY KEY ([caseId], [dataAttributeId]),
        CONSTRAINT [FK_CASE_ATTRIBUTE_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
        CONSTRAINT [FK_CASE_ATTRIBUTE_dataAttributeId] FOREIGN KEY ([dataAttributeId]) REFERENCES [dbo].[DATAATTRIBUTES]([dataAttributeId])
    );
END;
GO
