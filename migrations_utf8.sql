Build started...
Build succeeded.
The Entity Framework tools version '9.0.6' is older than that of the runtime '10.0.1'. Update the tools for the latest features and bug fixes. See https://aka.ms/AAc1fbw for more information.
[16:17:24 INF] Starting Blazor Quiz App
[16:17:24 INF] Using BaseUrl: https://localhost:7233
[16:17:24 FTL] Application terminated unexpectedly
Microsoft.Extensions.Hosting.HostAbortedException: The host was aborted.
   at Microsoft.Extensions.Hosting.HostFactoryResolver.HostingListener.ThrowHostAborted()
   at Microsoft.Extensions.Hosting.HostFactoryResolver.HostingListener.OnNext(KeyValuePair`2 value)
   at System.Diagnostics.DiagnosticListener.Write(String name, Object value)
   at Microsoft.Extensions.Hosting.HostBuilder.ResolveHost(IServiceProvider serviceProvider, DiagnosticListener diagnosticListener)
   at Microsoft.Extensions.Hosting.HostApplicationBuilder.Build()
   at Microsoft.AspNetCore.Builder.WebApplicationBuilder.Build()
   at Program.<Main>$(String[] args) in G:\From Desktop\Console,WinUI3, Blazor & MAUI\BlazorQuizApp\Program.cs:line 177
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
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
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

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

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

CREATE TABLE [QuizOptions] (
    [Id] int NOT NULL IDENTITY,
    [Text] nvarchar(500) NOT NULL,
    [QuizQuestionId] int NOT NULL,
    CONSTRAINT [PK_QuizOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuizOptions_QuizQuestions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [QuizQuestions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuizAnswers] (
    [Id] int NOT NULL IDENTITY,
    [QuestionId] int NOT NULL,
    [SelectedOptionId] int NOT NULL,
    [AnsweredAt] datetime2 NOT NULL,
    [QuizResultId] int NULL,
    CONSTRAINT [PK_QuizAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuizAnswers_QuizResults_QuizResultId] FOREIGN KEY ([QuizResultId]) REFERENCES [QuizResults] ([Id])
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_QuizAnswers_QuizResultId] ON [QuizAnswers] ([QuizResultId]);

CREATE INDEX [IX_QuizOptions_QuizQuestionId] ON [QuizOptions] ([QuizQuestionId]);

CREATE INDEX [IX_QuizResults_UserId] ON [QuizResults] ([UserId]);

CREATE UNIQUE INDEX [IX_Subscriptions_UserId] ON [Subscriptions] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260323161539_InitialCreate', N'10.0.1');

COMMIT;
GO


