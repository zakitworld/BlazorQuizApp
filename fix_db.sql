SET QUOTED_IDENTIFIER ON;
GO

-- Ensure EFMigrationsHistory exists
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
GO

-- 1. Update AspNetUsers with missing columns if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[AspNetUsers]') AND name = N'FullName')
BEGIN
    ALTER TABLE [AspNetUsers] ADD [FullName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[AspNetUsers]') AND name = N'CreatedAt')
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE();
END;
GO

-- 2. Create new tables if they don't exist
IF OBJECT_ID(N'[AuditLogs]') IS NULL
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NULL,
        [UserName] nvarchar(256) NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NULL,
        [EntityId] int NULL,
        [Details] nvarchar(2000) NULL,
        [IpAddress] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'[QuizQuestions]') IS NULL
BEGIN
    CREATE TABLE [QuizQuestions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionText] nvarchar(1000) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Difficulty] nvarchar(50) NOT NULL,
        [SelectedOptionId] int NOT NULL,
        [CorrectOptionId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_QuizQuestions] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'[Subscriptions]') IS NULL
BEGIN
    CREATE TABLE [Subscriptions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Tier] int NOT NULL,
        [StripeSubscriptionId] nvarchar(100) NULL,
        [StripeCustomerId] nvarchar(100) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [AutoRenew] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'[UsageTrackings]') IS NULL
BEGIN
    CREATE TABLE [UsageTrackings] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Month] int NOT NULL,
        [Year] int NOT NULL,
        [QuizzesTaken] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UsageTrackings] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'[QuizResults]') IS NULL
BEGIN
    CREATE TABLE [QuizResults] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NOT NULL,
        [TotalQuestions] int NOT NULL,
        [CorrectAnswers] int NOT NULL,
        [CompletedAt] datetime2 NOT NULL,
        [Duration] time NOT NULL,
        CONSTRAINT [PK_QuizResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizResults_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'[QuizOptions]') IS NULL
BEGIN
    CREATE TABLE [QuizOptions] (
        [Id] int NOT NULL IDENTITY,
        [Text] nvarchar(500) NOT NULL,
        [QuizQuestionId] int NOT NULL,
        CONSTRAINT [PK_QuizOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizOptions_QuizQuestions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [QuizQuestions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'[QuizAnswers]') IS NULL
BEGIN
    CREATE TABLE [QuizAnswers] (
        [Id] int NOT NULL IDENTITY,
        [QuestionId] int NOT NULL,
        [SelectedOptionId] int NOT NULL,
        [AnsweredAt] datetime2 NOT NULL,
        [QuizResultId] int NULL,
        CONSTRAINT [PK_QuizAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizAnswers_QuizResults_QuizResultId] FOREIGN KEY ([QuizResultId]) REFERENCES [QuizResults] ([Id])
    );
END;
GO

-- 3. Register Migration
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260323161539_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260323161539_InitialCreate', N'10.0.1');
END;
GO

COMMIT;
GO
