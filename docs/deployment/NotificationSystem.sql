BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023211840_AddNotificationSystem'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] bigint NOT NULL IDENTITY,
        [RecipientUserId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Content] nvarchar(1000) NOT NULL,
        [Type] int NOT NULL,
        [RelatedRecordId] int NULL,
        [EventKey] nvarchar(200) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ReadAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023211840_AddNotificationSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Notifications_RecipientUserId_EventKey] ON [Notifications] ([RecipientUserId], [EventKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023211840_AddNotificationSystem'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientUserId_IsRead_CreatedAtUtc] ON [Notifications] ([RecipientUserId], [IsRead], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023211840_AddNotificationSystem'
)
BEGIN
    CREATE INDEX [IX_Notifications_Type_RelatedRecordId] ON [Notifications] ([Type], [RelatedRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023211840_AddNotificationSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023211840_AddNotificationSystem', N'9.0.5');
END;

COMMIT;
GO

