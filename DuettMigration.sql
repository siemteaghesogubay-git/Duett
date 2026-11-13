IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026004027_InitialIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026004027_InitialIdentity', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026144851_AddUserProfile'
)
BEGIN
    CREATE TABLE [UserProfiles] (
        [UserId] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(50) NOT NULL,
        [DateOfBirth] date NOT NULL,
        [Gender] nvarchar(50) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [Bio] nvarchar(1000) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026144851_AddUserProfile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026144851_AddUserProfile', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030023928_AddPartnerPreferences'
)
BEGIN
    CREATE TABLE [PartnerPreferences] (
        [UserId] nvarchar(450) NOT NULL,
        [MinAge] int NOT NULL,
        [MaxAge] int NOT NULL,
        [PreferredGenders] nvarchar(max) NOT NULL,
        [PreferredCity] nvarchar(100) NULL,
        [RelationshipGoal] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PartnerPreferences] PRIMARY KEY ([UserId]),
        CONSTRAINT [CK_PartnerPreferences_AgeRange] CHECK ([MinAge] >= 18 AND [MaxAge] >= [MinAge]),
        CONSTRAINT [FK_PartnerPreferences_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030023928_AddPartnerPreferences'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030023928_AddPartnerPreferences', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065426_AddProfileLikes'
)
BEGIN
    CREATE TABLE [ProfileLikes] (
        [FromUserId] nvarchar(450) NOT NULL,
        [ToUserId] nvarchar(450) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ProfileLikes] PRIMARY KEY ([FromUserId], [ToUserId]),
        CONSTRAINT [CK_ProfileLikes_NoSelfLike] CHECK ([FromUserId] <> [ToUserId]),
        CONSTRAINT [FK_ProfileLikes_AspNetUsers_FromUserId] FOREIGN KEY ([FromUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ProfileLikes_AspNetUsers_ToUserId] FOREIGN KEY ([ToUserId]) REFERENCES [AspNetUsers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065426_AddProfileLikes'
)
BEGIN
    CREATE INDEX [IX_ProfileLikes_ToUserId] ON [ProfileLikes] ([ToUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065426_AddProfileLikes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030065426_AddProfileLikes', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030091024_AddUserMatches'
)
BEGIN
    CREATE TABLE [UserMatches] (
        [Id] uniqueidentifier NOT NULL,
        [FirstUserId] nvarchar(450) NOT NULL,
        [SecondUserId] nvarchar(450) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UserMatches] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UserMatches_UserOrder] CHECK ([FirstUserId] COLLATE Latin1_General_100_BIN2 < [SecondUserId] COLLATE Latin1_General_100_BIN2),
        CONSTRAINT [FK_UserMatches_AspNetUsers_FirstUserId] FOREIGN KEY ([FirstUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_UserMatches_AspNetUsers_SecondUserId] FOREIGN KEY ([SecondUserId]) REFERENCES [AspNetUsers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030091024_AddUserMatches'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserMatches_FirstUserId_SecondUserId] ON [UserMatches] ([FirstUserId], [SecondUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030091024_AddUserMatches'
)
BEGIN
    CREATE INDEX [IX_UserMatches_SecondUserId] ON [UserMatches] ([SecondUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030091024_AddUserMatches'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030091024_AddUserMatches', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE TABLE [GameQuestions] (
        [Id] uniqueidentifier NOT NULL,
        [Text] nvarchar(300) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_GameQuestions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE TABLE [GameQuestionOptions] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        [Text] nvarchar(150) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_GameQuestionOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GameQuestionOptions_SortOrder] CHECK ([SortOrder] >= 1),
        CONSTRAINT [FK_GameQuestionOptions_GameQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [GameQuestions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE TABLE [GameRounds] (
        [Id] uniqueidentifier NOT NULL,
        [MatchId] uniqueidentifier NOT NULL,
        [RoundNumber] int NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CompletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_GameRounds] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GameRounds_RoundNumber] CHECK ([RoundNumber] >= 1),
        CONSTRAINT [FK_GameRounds_GameQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [GameQuestions] ([Id]),
        CONSTRAINT [FK_GameRounds_UserMatches_MatchId] FOREIGN KEY ([MatchId]) REFERENCES [UserMatches] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE TABLE [GameRoundSubmissions] (
        [RoundId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [AnswerOptionId] uniqueidentifier NOT NULL,
        [GuessOptionId] uniqueidentifier NOT NULL,
        [SubmittedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_GameRoundSubmissions] PRIMARY KEY ([RoundId], [UserId]),
        CONSTRAINT [FK_GameRoundSubmissions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_GameRoundSubmissions_GameQuestionOptions_AnswerOptionId] FOREIGN KEY ([AnswerOptionId]) REFERENCES [GameQuestionOptions] ([Id]),
        CONSTRAINT [FK_GameRoundSubmissions_GameQuestionOptions_GuessOptionId] FOREIGN KEY ([GuessOptionId]) REFERENCES [GameQuestionOptions] ([Id]),
        CONSTRAINT [FK_GameRoundSubmissions_GameRounds_RoundId] FOREIGN KEY ([RoundId]) REFERENCES [GameRounds] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GameQuestionOptions_QuestionId_SortOrder] ON [GameQuestionOptions] ([QuestionId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GameRounds_MatchId_QuestionId] ON [GameRounds] ([MatchId], [QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GameRounds_MatchId_RoundNumber] ON [GameRounds] ([MatchId], [RoundNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE INDEX [IX_GameRounds_QuestionId] ON [GameRounds] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE INDEX [IX_GameRoundSubmissions_AnswerOptionId] ON [GameRoundSubmissions] ([AnswerOptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE INDEX [IX_GameRoundSubmissions_GuessOptionId] ON [GameRoundSubmissions] ([GuessOptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    CREATE INDEX [IX_GameRoundSubmissions_UserId] ON [GameRoundSubmissions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031032131_AddMatchGame'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031032131_AddMatchGame', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103052455_AddRelationshipMode'
)
BEGIN
    ALTER TABLE [PartnerPreferences] ADD [RelationshipMode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103052455_AddRelationshipMode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103052455_AddRelationshipMode', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103075426_AddChatMessages'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] uniqueidentifier NOT NULL,
        [MatchId] uniqueidentifier NOT NULL,
        [SenderUserId] nvarchar(450) NOT NULL,
        [Text] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_AspNetUsers_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ChatMessages_UserMatches_MatchId] FOREIGN KEY ([MatchId]) REFERENCES [UserMatches] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103075426_AddChatMessages'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_MatchId_CreatedAtUtc_Id] ON [ChatMessages] ([MatchId], [CreatedAtUtc], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103075426_AddChatMessages'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SenderUserId] ON [ChatMessages] ([SenderUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103075426_AddChatMessages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103075426_AddChatMessages', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105111040_AddMatchQuestions'
)
BEGIN
    CREATE TABLE [MatchQuestions] (
        [Id] uniqueidentifier NOT NULL,
        [MatchId] uniqueidentifier NOT NULL,
        [AskedByUserId] nvarchar(450) NOT NULL,
        [QuestionNumber] int NOT NULL,
        [Text] nvarchar(300) NOT NULL,
        [AnswerText] nvarchar(2000) NULL,
        [AnsweredByUserId] nvarchar(450) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [AnsweredAtUtc] datetime2 NULL,
        CONSTRAINT [PK_MatchQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MatchQuestions_AnswerState] CHECK (([AnswerText] IS NULL AND [AnsweredByUserId] IS NULL AND [AnsweredAtUtc] IS NULL) OR ([AnswerText] IS NOT NULL AND [AnsweredByUserId] IS NOT NULL AND [AnsweredAtUtc] IS NOT NULL)),
        CONSTRAINT [CK_MatchQuestions_NoSelfAnswer] CHECK ([AnsweredByUserId] IS NULL OR [AnsweredByUserId] <> [AskedByUserId]),
        CONSTRAINT [CK_MatchQuestions_QuestionNumber] CHECK ([QuestionNumber] BETWEEN 1 AND 3),
        CONSTRAINT [FK_MatchQuestions_AspNetUsers_AnsweredByUserId] FOREIGN KEY ([AnsweredByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_MatchQuestions_AspNetUsers_AskedByUserId] FOREIGN KEY ([AskedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_MatchQuestions_UserMatches_MatchId] FOREIGN KEY ([MatchId]) REFERENCES [UserMatches] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105111040_AddMatchQuestions'
)
BEGIN
    CREATE INDEX [IX_MatchQuestions_AnsweredByUserId] ON [MatchQuestions] ([AnsweredByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105111040_AddMatchQuestions'
)
BEGIN
    CREATE INDEX [IX_MatchQuestions_AskedByUserId] ON [MatchQuestions] ([AskedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105111040_AddMatchQuestions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MatchQuestions_MatchId_AskedByUserId_QuestionNumber] ON [MatchQuestions] ([MatchId], [AskedByUserId], [QuestionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105111040_AddMatchQuestions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105111040_AddMatchQuestions', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105124735_AddProfilePhotos'
)
BEGIN
    CREATE TABLE [ProfilePhotos] (
        [UserId] nvarchar(450) NOT NULL,
        [StorageKey] nvarchar(200) NOT NULL,
        [ContentType] nvarchar(50) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [UploadedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ProfilePhotos] PRIMARY KEY ([UserId]),
        CONSTRAINT [CK_ProfilePhotos_SizeBytes] CHECK ([SizeBytes] > 0),
        CONSTRAINT [FK_ProfilePhotos_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105124735_AddProfilePhotos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProfilePhotos_StorageKey] ON [ProfilePhotos] ([StorageKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105124735_AddProfilePhotos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105124735_AddProfilePhotos', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113120446_AddEmailVerificationCode'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [EmailVerificationExpiresAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113120446_AddEmailVerificationCode'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [EmailVerificationFailedAttempts] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113120446_AddEmailVerificationCode'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [EmailVerificationNextSendAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113120446_AddEmailVerificationCode'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [ProtectedEmailVerificationCode] nvarchar(2048) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113120446_AddEmailVerificationCode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113120446_AddEmailVerificationCode', N'10.0.14');
END;

COMMIT;
GO

