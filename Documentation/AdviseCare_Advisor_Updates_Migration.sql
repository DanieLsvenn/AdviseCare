-- Add student-visible advisor updates without resetting or reseeding existing data.
-- Run this once against an existing AdviseCare database before deploying the API.
IF OBJECT_ID(N'[dbo].[CASEADVISORUPDATES]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CASEADVISORUPDATES] (
        [caseAdvisorUpdateId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CASEADVISORUPDATES_caseAdvisorUpdateId] DEFAULT NEWSEQUENTIALID(),
        [authorId] UNIQUEIDENTIFIER NOT NULL,
        [caseId] UNIQUEIDENTIFIER NOT NULL,
        [messageText] NVARCHAR(1000) NOT NULL,
        [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASEADVISORUPDATES_createdAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_CASEADVISORUPDATES] PRIMARY KEY ([caseAdvisorUpdateId]),
        CONSTRAINT [FK_CASEADVISORUPDATES_authorId] FOREIGN KEY ([authorId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
        CONSTRAINT [FK_CASEADVISORUPDATES_caseId] FOREIGN KEY ([caseId]) REFERENCES [dbo].[CASES]([caseId]),
        CONSTRAINT [CK_CASEADVISORUPDATES_messageText] CHECK (LEN(LTRIM(RTRIM([messageText]))) BETWEEN 1 AND 1000)
    );
END;
GO

IF OBJECT_ID(N'[dbo].[CASEADVISORUPDATES]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE [name] = N'IX_CASEADVISORUPDATES_caseId_createdAt'
         AND [object_id] = OBJECT_ID(N'[dbo].[CASEADVISORUPDATES]')
   )
BEGIN
    CREATE INDEX [IX_CASEADVISORUPDATES_caseId_createdAt]
        ON [dbo].[CASEADVISORUPDATES] ([caseId], [createdAt]);
END;
GO
