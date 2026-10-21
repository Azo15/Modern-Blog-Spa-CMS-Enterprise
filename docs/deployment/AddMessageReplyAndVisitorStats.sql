BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    ALTER TABLE [ContactForms] ADD [IsReplied] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    ALTER TABLE [ContactForms] ADD [LastRepliedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE TABLE [ContactReplies] (
        [Id] int NOT NULL IDENTITY,
        [ContactFormId] int NOT NULL,
        [ReplyMessage] nvarchar(max) NOT NULL,
        [RepliedByUserId] nvarchar(256) NOT NULL,
        [RepliedByUserName] nvarchar(256) NOT NULL,
        [RepliedAt] datetime2 NOT NULL,
        [IsMailSent] bit NOT NULL,
        [RecipientEmail] nvarchar(254) NOT NULL,
        [Subject] nvarchar(250) NOT NULL,
        [ClientRequestId] uniqueidentifier NOT NULL,
        [MessageId] nvarchar(160) NOT NULL,
        [LeaseToken] nvarchar(64) NULL,
        [LeaseExpiresAtUtc] datetime2 NULL,
        [SentAtUtc] datetime2 NULL,
        [LastEnqueuedAtUtc] datetime2 NULL,
        [HangfireJobId] nvarchar(100) NULL,
        [LastError] nvarchar(128) NULL,
        [AttemptCount] int NOT NULL,
        CONSTRAINT [PK_ContactReplies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContactReplies_ContactForms_ContactFormId] FOREIGN KEY ([ContactFormId]) REFERENCES [ContactForms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE TABLE [DailyVisitorStats] (
        [Date] date NOT NULL,
        [TotalPageViews] int NOT NULL,
        [UniqueVisitors] int NOT NULL,
        [MobileVisits] int NOT NULL,
        [DesktopVisits] int NOT NULL,
        [TopVisitedPagesJson] nvarchar(4000) NULL,
        CONSTRAINT [PK_DailyVisitorStats] PRIMARY KEY ([Date])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE TABLE [VisitorLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [VisitorHash] nvarchar(64) NOT NULL,
        [Path] nvarchar(256) NOT NULL,
        [IsMobile] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_VisitorLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContactReplies_ClientRequestId] ON [ContactReplies] ([ClientRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE INDEX [IX_ContactReplies_ContactFormId_RepliedAt] ON [ContactReplies] ([ContactFormId], [RepliedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE INDEX [IX_ContactReplies_IsMailSent_LastEnqueuedAtUtc] ON [ContactReplies] ([IsMailSent], [LastEnqueuedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE INDEX [IX_VisitorLogs_CreatedDate] ON [VisitorLogs] ([CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE INDEX [IX_VisitorLogs_Date_Path] ON [VisitorLogs] ([Date], [Path]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    CREATE INDEX [IX_VisitorLogs_Date_VisitorHash] ON [VisitorLogs] ([Date], [VisitorHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202150_AddMessageReplyAndVisitorStats'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021202150_AddMessageReplyAndVisitorStats', N'9.0.5');
END;

COMMIT;
GO

