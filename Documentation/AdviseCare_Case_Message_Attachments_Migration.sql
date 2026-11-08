-- Brings an existing AdviseCare database up to date without resetting or reseeding its data. Run once before deploying the API; re-running is safe.
-- CASEMESSAGEATTACHMENTS (46): files sent with a case message (stored in the database, downloaded only through the API).
USE [AdviseCareDB];
GO

IF OBJECT_ID(N'[dbo].[CASEMESSAGEATTACHMENTS]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CASEMESSAGEATTACHMENTS] (
        [caseMessageAttachmentId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CASEMESSAGEATTACHMENTS_caseMessageAttachmentId] DEFAULT NEWSEQUENTIALID(),
        [caseMessageId] UNIQUEIDENTIFIER NOT NULL,
        [uploadedBy] UNIQUEIDENTIFIER NOT NULL,
        [contentType] NVARCHAR(255) NOT NULL,
        [fileContent] VARBINARY(MAX) NOT NULL,
        [fileName] NVARCHAR(255) NOT NULL,
        [fileSize] INT NOT NULL,
        [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_CASEMESSAGEATTACHMENTS_createdAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_CASEMESSAGEATTACHMENTS] PRIMARY KEY ([caseMessageAttachmentId]),
        CONSTRAINT [FK_CASEMESSAGEATTACHMENTS_caseMessageId] FOREIGN KEY ([caseMessageId]) REFERENCES [dbo].[CASEMESSAGES]([caseMessageId]),
        CONSTRAINT [FK_CASEMESSAGEATTACHMENTS_uploadedBy] FOREIGN KEY ([uploadedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
        CONSTRAINT [CK_CASEMESSAGEATTACHMENTS_fileSize] CHECK ([fileSize] BETWEEN 1 AND 10485760),
        CONSTRAINT [CK_CASEMESSAGEATTACHMENTS_fileName] CHECK (LEN(LTRIM(RTRIM([fileName]))) >= 1)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CASEMESSAGEATTACHMENTS_caseMessageId' AND [object_id] = OBJECT_ID(N'[dbo].[CASEMESSAGEATTACHMENTS]'))
    CREATE INDEX [IX_CASEMESSAGEATTACHMENTS_caseMessageId] ON [dbo].[CASEMESSAGEATTACHMENTS] ([caseMessageId]);
GO
