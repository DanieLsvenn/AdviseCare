-- Brings an existing AdviseCare database up to date without resetting or reseeding its data. Run once before deploying the API; re-running is safe.
-- Academic results (50-52): per-subject grade and attendance of each student per term, imported by the System Administrator from transcript PDFs.
--   STUDENTPROFILES.curriculumCode    : curriculum printed on the transcript (e.g. BIT_SE_NET_18A), display only
--   TRANSCRIPTIMPORTBATCHES (50)      : one upload session of the admin (preview -> commit or cancel)
--   TRANSCRIPTIMPORTFILES (51)        : one PDF of a batch (one student, one type: grade or attendance), with its parsed rows until commit
--   STUDENTSUBJECTRESULTS (52)        : current grade and attendance of one student in one subject in one term (upserted by each import)
USE [AdviseCareDB];
GO

IF COL_LENGTH(N'dbo.STUDENTPROFILES', N'curriculumCode') IS NULL
    ALTER TABLE [dbo].[STUDENTPROFILES] ADD [curriculumCode] NVARCHAR(50) NULL;
GO

IF OBJECT_ID(N'[dbo].[TRANSCRIPTIMPORTBATCHES]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TRANSCRIPTIMPORTBATCHES] (
        [importBatchId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_TRANSCRIPTIMPORTBATCHES_importBatchId] DEFAULT NEWSEQUENTIALID(),
        [importedBy] UNIQUEIDENTIFIER NOT NULL,
        [createdAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_TRANSCRIPTIMPORTBATCHES_createdAt] DEFAULT SYSUTCDATETIME(),
        [committedAt] DATETIME2(3) NULL,
        [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_TRANSCRIPTIMPORTBATCHES_status] DEFAULT N'previewed',
        CONSTRAINT [PK_TRANSCRIPTIMPORTBATCHES] PRIMARY KEY ([importBatchId]),
        CONSTRAINT [FK_TRANSCRIPTIMPORTBATCHES_importedBy] FOREIGN KEY ([importedBy]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
        CONSTRAINT [CK_TRANSCRIPTIMPORTBATCHES_status] CHECK ([status] IN (N'previewed', N'committed', N'cancelled'))
    );
END;
GO

IF OBJECT_ID(N'[dbo].[TRANSCRIPTIMPORTFILES]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TRANSCRIPTIMPORTFILES] (
        [importFileId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_TRANSCRIPTIMPORTFILES_importFileId] DEFAULT NEWSEQUENTIALID(),
        [importBatchId] UNIQUEIDENTIFIER NOT NULL,
        [studentId] UNIQUEIDENTIFIER NULL,
        [fileName] NVARCHAR(255) NOT NULL,
        -- SHA-256 of the file, hex; finds a file uploaded twice (the PDF itself is not kept)
        [fileHash] NVARCHAR(64) NOT NULL,
        [importType] NVARCHAR(30) NULL,
        [studentCodeInFile] NVARCHAR(50) NULL,
        [fullNameInFile] NVARCHAR(255) NULL,
        [curriculumCode] NVARCHAR(50) NULL,
        [rowCount] INT NOT NULL CONSTRAINT [DF_TRANSCRIPTIMPORTFILES_rowCount] DEFAULT 0,
        -- JSON: the rows read from the PDF, kept between preview and commit
        [parsedRows] NVARCHAR(MAX) NULL,
        -- JSON: errors, warnings and the create/update/skip counts found at preview
        [validationResult] NVARCHAR(MAX) NULL,
        [uploadedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_TRANSCRIPTIMPORTFILES_uploadedAt] DEFAULT SYSUTCDATETIME(),
        [status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_TRANSCRIPTIMPORTFILES_status] DEFAULT N'invalid',
        CONSTRAINT [PK_TRANSCRIPTIMPORTFILES] PRIMARY KEY ([importFileId]),
        CONSTRAINT [FK_TRANSCRIPTIMPORTFILES_importBatchId] FOREIGN KEY ([importBatchId]) REFERENCES [dbo].[TRANSCRIPTIMPORTBATCHES]([importBatchId]),
        CONSTRAINT [FK_TRANSCRIPTIMPORTFILES_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
        CONSTRAINT [CK_TRANSCRIPTIMPORTFILES_importType] CHECK ([importType] IN (N'grade', N'attendance')),
        CONSTRAINT [CK_TRANSCRIPTIMPORTFILES_status] CHECK ([status] IN (N'valid', N'invalid', N'committed')),
        CONSTRAINT [CK_TRANSCRIPTIMPORTFILES_rowCount] CHECK ([rowCount] >= 0)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_TRANSCRIPTIMPORTFILES_importBatchId' AND [object_id] = OBJECT_ID(N'[dbo].[TRANSCRIPTIMPORTFILES]'))
    CREATE INDEX [IX_TRANSCRIPTIMPORTFILES_importBatchId] ON [dbo].[TRANSCRIPTIMPORTFILES] ([importBatchId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_TRANSCRIPTIMPORTFILES_fileHash' AND [object_id] = OBJECT_ID(N'[dbo].[TRANSCRIPTIMPORTFILES]'))
    CREATE INDEX [IX_TRANSCRIPTIMPORTFILES_fileHash] ON [dbo].[TRANSCRIPTIMPORTFILES] ([fileHash]);
GO

IF OBJECT_ID(N'[dbo].[STUDENTSUBJECTRESULTS]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[STUDENTSUBJECTRESULTS] (
        [studentSubjectResultId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_STUDENTSUBJECTRESULTS_studentSubjectResultId] DEFAULT NEWSEQUENTIALID(),
        [studentId] UNIQUEIDENTIFIER NOT NULL,
        [subjectId] UNIQUEIDENTIFIER NOT NULL,
        [termId] UNIQUEIDENTIFIER NOT NULL,
        -- "Term" column of the transcript: the curriculum semester the subject belongs to (0-9); null outside the curriculum
        [curriculumTerm] TINYINT NULL,
        [isInCurriculum] BIT NOT NULL CONSTRAINT [DF_STUDENTSUBJECTRESULTS_isInCurriculum] DEFAULT 1,
        -- final grade on a 10-point scale; null when the transcript shows "-" (exempt, studying, passed without a grade)
        [grade] DECIMAL(4,2) NULL,
        -- percentage of attended slots; absence rate = 100 - attendanceRate
        [attendanceRate] DECIMAL(5,2) NULL,
        [status] NVARCHAR(30) NULL,
        [gradeImportFileId] UNIQUEIDENTIFIER NULL,
        [attendanceImportFileId] UNIQUEIDENTIFIER NULL,
        [gradeUpdatedAt] DATETIME2(3) NULL,
        [attendanceUpdatedAt] DATETIME2(3) NULL,
        CONSTRAINT [PK_STUDENTSUBJECTRESULTS] PRIMARY KEY ([studentSubjectResultId]),
        CONSTRAINT [FK_STUDENTSUBJECTRESULTS_studentId] FOREIGN KEY ([studentId]) REFERENCES [dbo].[ACCOUNTS]([accountId]),
        CONSTRAINT [FK_STUDENTSUBJECTRESULTS_subjectId] FOREIGN KEY ([subjectId]) REFERENCES [dbo].[SUBJECTS]([subjectId]),
        CONSTRAINT [FK_STUDENTSUBJECTRESULTS_termId] FOREIGN KEY ([termId]) REFERENCES [dbo].[ACADEMICTERMS]([termId]),
        CONSTRAINT [FK_STUDENTSUBJECTRESULTS_gradeImportFileId] FOREIGN KEY ([gradeImportFileId]) REFERENCES [dbo].[TRANSCRIPTIMPORTFILES]([importFileId]),
        CONSTRAINT [FK_STUDENTSUBJECTRESULTS_attendanceImportFileId] FOREIGN KEY ([attendanceImportFileId]) REFERENCES [dbo].[TRANSCRIPTIMPORTFILES]([importFileId]),
        CONSTRAINT [UQ_STUDENTSUBJECTRESULTS_student_subject_term] UNIQUE ([studentId], [subjectId], [termId]),
        CONSTRAINT [CK_STUDENTSUBJECTRESULTS_grade] CHECK ([grade] BETWEEN 0 AND 10),
        CONSTRAINT [CK_STUDENTSUBJECTRESULTS_attendanceRate] CHECK ([attendanceRate] BETWEEN 0 AND 100),
        CONSTRAINT [CK_STUDENTSUBJECTRESULTS_curriculumTerm] CHECK ([curriculumTerm] BETWEEN 0 AND 20),
        CONSTRAINT [CK_STUDENTSUBJECTRESULTS_status] CHECK ([status] IN (N'passed', N'notPassed', N'attendanceFail', N'exempt', N'studying', N'notStarted'))
    );
END;
GO
