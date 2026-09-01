USE [NetflixCloneDb]
GO

CREATE TABLE [dbo].[UserAccounts] (
  [Id] int IDENTITY(1, 1),
  [Email] nvarchar(255) UNIQUE NOT NULL,
  [PasswordHash] nvarchar(500) NOT NULL,
  [IsLocked] bit NOT NULL DEFAULT (0),
  [FailedLoginCount] int NOT NULL DEFAULT (0),
  [LockoutEnd] datetime2,
  [LastLoginAt] datetime2,
  [EmailConfirmed] bit NOT NULL DEFAULT (0),
  [EmailConfirmedAt] datetime2,
  [EmailConfirmationTokenHash] nvarchar(500),
  [EmailConfirmationTokenExpiresAt] datetime2,
  [PasswordResetTokenHash] nvarchar(500),
  [PasswordResetTokenExpiresAt] datetime2,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [UpdatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [PK_UserAccounts] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_UserAccounts_FailedLoginCount] CHECK (FailedLoginCount >= 0)
)
GO

CREATE TABLE [dbo].[Roles] (
  [Id] int IDENTITY(1, 1),
  [Name] nvarchar(50) UNIQUE NOT NULL,
  [Description] nvarchar(255),
  [IsSystem] bit NOT NULL DEFAULT (0),
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[UserRoles] (
  [Id] int IDENTITY(1, 1),
  [UserAccountId] int NOT NULL,
  [RoleId] int NOT NULL,
  [AssignedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[Devices] (
  [Id] int IDENTITY(1, 1),
  [UserAccountId] int NOT NULL,
  [DeviceIdentifierHash] nvarchar(500) NOT NULL,
  [DeviceName] nvarchar(200) NOT NULL,
  [DeviceType] nvarchar(50) NOT NULL,
  [UserAgent] nvarchar(500),
  [IpAddress] nvarchar(45),
  [FirstSeenAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [LastActiveAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [RevokedAt] datetime2,
  CONSTRAINT [CK_Devices_ActivityTime] CHECK (LastActiveAt >= FirstSeenAt),
  CONSTRAINT [CK_Devices_RevokedAt] CHECK (RevokedAt IS NULL OR RevokedAt >= FirstSeenAt),
    CONSTRAINT [PK_Devices] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[Profiles] (
  [Id] int IDENTITY(1, 1),
  [UserAccountId] int NOT NULL,
  [Name] nvarchar(100) NOT NULL,
  [AvatarUrl] nvarchar(500),
  [IsKids] bit NOT NULL DEFAULT (0),
  [MaturityLevel] tinyint NOT NULL DEFAULT (18),
  [PinHash] nvarchar(500),
  [OnboardingCompleted] bit NOT NULL DEFAULT (0),
  [IsDeleted] bit NOT NULL DEFAULT (0),
  [DeletedAt] datetime2,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [UpdatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_Profiles_SoftDeleteState] CHECK ((IsDeleted = 0 AND DeletedAt IS NULL) OR (IsDeleted = 1 AND DeletedAt IS NOT NULL)),
    CONSTRAINT [PK_Profiles] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[RefreshTokens] (
  [Id] int IDENTITY(1, 1),
  [UserAccountId] int NOT NULL,
  [DeviceId] int NOT NULL,
  [TokenHash] nvarchar(500) UNIQUE NOT NULL,
  [ExpiresAt] datetime2 NOT NULL,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [RevokedAt] datetime2,
  [ReplacedByTokenId] int,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[Plans] (
  [Id] int IDENTITY(1, 1),
  [Name] nvarchar(100) UNIQUE NOT NULL,
  [Price] decimal(18,2) NOT NULL,
  [MaxProfiles] int NOT NULL,
  [MaxConcurrentStreams] int NOT NULL,
  [MaxQuality] nvarchar(20) NOT NULL,
  [IsActive] bit NOT NULL DEFAULT (1),
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [UpdatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_Plans_Price] CHECK (Price >= 0),
  CONSTRAINT [CK_Plans_MaxProfiles] CHECK (MaxProfiles > 0),
  CONSTRAINT [CK_Plans_MaxConcurrentStreams] CHECK (MaxConcurrentStreams > 0),
    CONSTRAINT [PK_Plans] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[Subscriptions] (
  [Id] int  IDENTITY(1, 1),
  [UserAccountId] int NOT NULL,
  [PlanId] int NOT NULL,
  [StartDate] datetime2 NOT NULL,
  [EndDate] datetime2,
  [Status] nvarchar(20) NOT NULL,
  [AutoRenew] bit NOT NULL DEFAULT (0),
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [UpdatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_Subscriptions_Status] CHECK (Status IN ('Pending', 'Active', 'Expired', 'Cancelled')),
  CONSTRAINT [CK_Subscriptions_DateRange] CHECK (EndDate IS NULL OR EndDate >= StartDate),
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[PaymentTransactions] (
  [Id] int  IDENTITY(1, 1),
  [SubscriptionId] int NOT NULL,
  [Amount] decimal(18,2) NOT NULL,
  [Method] nvarchar(50) NOT NULL,
  [TransactionCode] nvarchar(100) UNIQUE NOT NULL,
  [Status] nvarchar(20) NOT NULL,
  [PaidAt] datetime2,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_PaymentTransactions_Amount] CHECK (Amount >= 0),
  CONSTRAINT [CK_PaymentTransactions_Status] CHECK (Status IN ('Pending', 'Succeeded', 'Failed')),
  CONSTRAINT [CK_PaymentTransactions_PaidAt] CHECK ((Status = 'Succeeded' AND PaidAt IS NOT NULL) OR (Status IN ('Pending', 'Failed') AND PaidAt IS NULL)),
    CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[Movies] (
  [Id] int  IDENTITY(1, 1),
  [Title] nvarchar(255) NOT NULL,
  [Description] nvarchar(2000),
  [ReleaseDate] date,
  [DurationSeconds] int NOT NULL,
  [ThumbnailUrl] nvarchar(500),
  [BackdropUrl] nvarchar(500),
  [TrailerUrl] nvarchar(500),
  [VideoUrl] nvarchar(500),
  [MaturityRating] nvarchar(10) NOT NULL DEFAULT 'P',
  [MinAge] tinyint NOT NULL DEFAULT (0),
  [IsDeleted] bit NOT NULL DEFAULT (0),
  [IsFeatured] bit NOT NULL DEFAULT (0),
  [IsAvailable] bit NOT NULL DEFAULT (1),
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [UpdatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_Movies_DurationSeconds] CHECK (DurationSeconds > 0),
    CONSTRAINT [PK_Movies] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[Genres] (
  [Id] int  IDENTITY(1, 1),
  [Name] nvarchar(100) UNIQUE NOT NULL,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_Genres] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[MovieGenres] (
  [MovieId] int NOT NULL,
  [GenreId] int NOT NULL,
  PRIMARY KEY ([MovieId], [GenreId]),
  --   CONSTRAINT [PK_MovieGenres] PRIMARY KEY ([MovieId], [GenreId])
)
GO

CREATE TABLE [dbo].[People] (
  [Id] int  IDENTITY(1, 1),
  [FullName] nvarchar(200) NOT NULL,
  [PhotoUrl] nvarchar(500),
  [BirthDate] date,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [UpdatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_People] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[MovieCredits] (
  [Id] int  IDENTITY(1, 1),
  [MovieId] int NOT NULL,
  [PersonId] int NOT NULL,
  [CreditType] nvarchar(20) NOT NULL,
  [CharacterName] nvarchar(200),
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_MovieCredits_CreditType] CHECK (CreditType IN ('Actor', 'Director')),
    CONSTRAINT [PK_MovieCredits] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[MyListItems] (
  [Id] int  IDENTITY(1, 1),
  [ProfileId] int NOT NULL,
  [MovieId] int NOT NULL,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_MyListItems] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[Ratings] (
  [Id] int  IDENTITY(1, 1),
  [ProfileId] int NOT NULL,
  [MovieId] int NOT NULL,
  [Value] nvarchar(20) NOT NULL,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [UpdatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_Ratings_Value] CHECK (Value IN ('NotForMe', 'Like', 'Love')),
    CONSTRAINT [PK_Ratings] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[ProfilePreferences] (
  [Id] int  IDENTITY(1, 1),
  [ProfileId] int NOT NULL,
  [MovieId] int NOT NULL,
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_ProfilePreferences] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[WatchHistories] (
  [Id] int  IDENTITY(1, 1),
  [ProfileId] int NOT NULL,
  [MovieId] int NOT NULL,
  [LastPositionSeconds] int NOT NULL DEFAULT (0),
  [LastWatchedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [IsCompleted] bit NOT NULL DEFAULT (0),
  [CompletedAt] datetime2,
  [IsHidden] bit NOT NULL DEFAULT (0),
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [UpdatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_WatchHistories_LastPositionSeconds] CHECK (LastPositionSeconds >= 0),
  CONSTRAINT [CK_WatchHistories_CompletedAt] CHECK ((IsCompleted = 0 AND CompletedAt IS NULL) OR (IsCompleted = 1 AND CompletedAt IS NOT NULL)),
    CONSTRAINT [PK_WatchHistories] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[ViewingSessions] (
  [Id] int  IDENTITY(1, 1),
  [ProfileId] int NOT NULL,
  [MovieId] int NOT NULL,
  [DeviceId] int NOT NULL,
  [StartedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  [EndedAt] datetime2,
  [WatchedSeconds] int NOT NULL DEFAULT (0),
  [IsQualifiedView] bit NOT NULL DEFAULT (0),
  [Quality] nvarchar(20),
  [EndReason] nvarchar(30),
  CONSTRAINT [CK_ViewingSessions_WatchedSeconds] CHECK (WatchedSeconds >= 0),
  CONSTRAINT [CK_ViewingSessions_TimeRange] CHECK (EndedAt IS NULL OR EndedAt >= StartedAt),
  CONSTRAINT [CK_ViewingSessions_QualifiedView] CHECK (IsQualifiedView = 0 OR WatchedSeconds >= 120),
  CONSTRAINT [CK_ViewingSessions_EndReason] CHECK (EndReason IS NULL OR EndReason IN ('Finished', 'Closed', 'Timeout')),
  CONSTRAINT [CK_ViewingSessions_EndState] CHECK ((EndedAt IS NULL AND EndReason IS NULL) OR (EndedAt IS NOT NULL AND EndReason IS NOT NULL)),
    CONSTRAINT [PK_ViewingSessions] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [dbo].[AdminActionLogs] (
  [Id] int  IDENTITY(1, 1),
  [ActorUserAccountId] int NOT NULL,
  [TargetUserAccountId] int NOT NULL,
  [Action] nvarchar(50) NOT NULL,
  [Reason] nvarchar(500),
  [CreatedAt] datetime2 NOT NULL DEFAULT (sysdatetime()),
  CONSTRAINT [CK_AdminActionLogs_Action] CHECK (Action IN ('AccountLocked', 'AccountUnlocked')),
    CONSTRAINT [PK_AdminActionLogs] PRIMARY KEY ([Id])
)
GO

CREATE UNIQUE INDEX [UQ_UserRoles] ON [dbo].[UserRoles] ("UserAccountId", "RoleId")
GO

CREATE INDEX [IX_Devices_UserAccountId] ON [dbo].[Devices] ("UserAccountId")
GO

CREATE UNIQUE INDEX [UQ_Devices_UserAccountId_DeviceIdentifierHash] ON [dbo].[Devices] ("UserAccountId", "DeviceIdentifierHash")
GO

CREATE INDEX [IX_Devices_UserAccountId_RevokedAt] ON [dbo].[Devices] ("UserAccountId", "RevokedAt")
GO

CREATE INDEX [IX_Devices_LastActiveAt] ON [dbo].[Devices] ("LastActiveAt")
GO

CREATE INDEX [IX_Profiles_UserAccountId] ON [dbo].[Profiles] ("UserAccountId")
GO

CREATE INDEX [IX_Profiles_UserAccountId_IsDeleted] ON [dbo].[Profiles] ("UserAccountId", "IsDeleted")
GO

CREATE INDEX [IX_RefreshTokens_UserAccountId] ON [dbo].[RefreshTokens] ("UserAccountId")
GO

CREATE INDEX [IX_RefreshTokens_DeviceId] ON [dbo].[RefreshTokens] ("DeviceId")
GO

CREATE INDEX [IX_RefreshTokens_Device_Active] ON [dbo].[RefreshTokens] ("DeviceId", "RevokedAt", "ExpiresAt")
GO

CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [dbo].[RefreshTokens] ("ExpiresAt")
GO

CREATE INDEX [IX_RefreshTokens_ReplacedByTokenId] ON [dbo].[RefreshTokens] ("ReplacedByTokenId")
GO

CREATE INDEX [IX_Subscriptions_UserAccountId] ON [dbo].[Subscriptions] ("UserAccountId")
GO

CREATE INDEX [IX_Subscriptions_PlanId] ON [dbo].[Subscriptions] ("PlanId")
GO

CREATE INDEX [IX_Subscriptions_UserAccountId_Status] ON [dbo].[Subscriptions] ("UserAccountId", "Status")
GO

CREATE INDEX [IX_Subscriptions_EndDate] ON [dbo].[Subscriptions] ("EndDate")
GO

CREATE INDEX [IX_PaymentTransactions_SubscriptionId] ON [dbo].[PaymentTransactions] ("SubscriptionId")
GO

CREATE INDEX [IX_PaymentTransactions_Status] ON [dbo].[PaymentTransactions] ("Status")
GO

CREATE INDEX [IX_PaymentTransactions_PaidAt] ON [dbo].[PaymentTransactions] ("PaidAt")
GO

CREATE INDEX [IX_Movies_Title] ON [dbo].[Movies] ("Title")
GO

CREATE INDEX [IX_Movies_ReleaseDate] ON [dbo].[Movies] ("ReleaseDate")
GO

CREATE INDEX [IX_Movies_IsFeatured] ON [dbo].[Movies] ("IsFeatured")
GO

CREATE INDEX [IX_Movies_IsAvailable] ON [dbo].[Movies] ("IsAvailable")
GO

CREATE INDEX [IX_MovieGenres_GenreId] ON [dbo].[MovieGenres] ("GenreId")
GO

CREATE INDEX [IX_People_FullName] ON [dbo].[People] ("FullName")
GO

CREATE INDEX [IX_MovieCredits_MovieId] ON [dbo].[MovieCredits] ("MovieId")
GO

CREATE INDEX [IX_MovieCredits_PersonId] ON [dbo].[MovieCredits] ("PersonId")
GO

CREATE INDEX [IX_MovieCredits_MovieId_CreditType] ON [dbo].[MovieCredits] ("MovieId", "CreditType")
GO

CREATE UNIQUE INDEX [UQ_MyListItems_ProfileId_MovieId] ON [dbo].[MyListItems] ("ProfileId", "MovieId")
GO

CREATE INDEX [IX_MyListItems_MovieId] ON [dbo].[MyListItems] ("MovieId")
GO

CREATE UNIQUE INDEX [UQ_Ratings_ProfileId_MovieId] ON [dbo].[Ratings] ("ProfileId", "MovieId")
GO

CREATE INDEX [IX_Ratings_MovieId] ON [dbo].[Ratings] ("MovieId")
GO

CREATE UNIQUE INDEX [UQ_ProfilePreferences_ProfileId_MovieId] ON [dbo].[ProfilePreferences] ("ProfileId", "MovieId")
GO

CREATE INDEX [IX_ProfilePreferences_MovieId] ON [dbo].[ProfilePreferences] ("MovieId")
GO

CREATE UNIQUE INDEX [UQ_WatchHistories_ProfileId_MovieId] ON [dbo].[WatchHistories] ("ProfileId", "MovieId")
GO

CREATE INDEX [IX_WatchHistories_ContinueWatching] ON [dbo].[WatchHistories] ("ProfileId", "IsCompleted", "IsHidden", "LastWatchedAt")
GO

CREATE INDEX [IX_WatchHistories_MovieId] ON [dbo].[WatchHistories] ("MovieId")
GO

CREATE INDEX [IX_ViewingSessions_ProfileId] ON [dbo].[ViewingSessions] ("ProfileId")
GO

CREATE INDEX [IX_ViewingSessions_MovieId] ON [dbo].[ViewingSessions] ("MovieId")
GO

CREATE INDEX [IX_ViewingSessions_DeviceId] ON [dbo].[ViewingSessions] ("DeviceId")
GO

CREATE INDEX [IX_ViewingSessions_Device_Active] ON [dbo].[ViewingSessions] ("DeviceId", "EndedAt")
GO

CREATE INDEX [IX_ViewingSessions_Movie_QualifiedView] ON [dbo].[ViewingSessions] ("MovieId", "IsQualifiedView")
GO

CREATE INDEX [IX_ViewingSessions_StartedAt] ON [dbo].[ViewingSessions] ("StartedAt")
GO

CREATE INDEX [IX_AdminActionLogs_ActorUserAccountId] ON [dbo].[AdminActionLogs] ("ActorUserAccountId")
GO

CREATE INDEX [IX_AdminActionLogs_TargetUserAccountId] ON [dbo].[AdminActionLogs] ("TargetUserAccountId")
GO

CREATE INDEX [IX_AdminActionLogs_TargetUserAccountId_CreatedAt] ON [dbo].[AdminActionLogs] ("TargetUserAccountId", "CreatedAt")
GO

CREATE INDEX [IX_AdminActionLogs_CreatedAt] ON [dbo].[AdminActionLogs] ("CreatedAt")
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Authentication identity. IsLocked is reserved for manual Admin lock/unlock, while FailedLoginCount + LockoutEnd handle temporary automatic lockout after repeated failed logins. Email confirmation and password reset store only token hashes plus expiry timestamps. Account deletion is outside the current MVP. The maximum active Profile count is determined by the active Subscription Plan.MaxProfiles rather than a hard-coded account limit.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'UserAccounts';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Admin lock/unlock flag',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'UserAccounts',
@level2type = N'Column', @level2name = 'IsLocked';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Consecutive failed login attempts used for temporary lockout',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'UserAccounts',
@level2type = N'Column', @level2name = 'FailedLoginCount';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Temporary system lockout end time after repeated failed login attempts',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'UserAccounts',
@level2type = N'Column', @level2name = 'LockoutEnd';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Timestamp of the most recent successful login',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'UserAccounts',
@level2type = N'Column', @level2name = 'LastLoginAt';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Hash of the one-time email confirmation token; never store the raw token',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'UserAccounts',
@level2type = N'Column', @level2name = 'EmailConfirmationTokenHash';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Hash of the one-time password reset token; never store the raw token',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'UserAccounts',
@level2type = N'Column', @level2name = 'PasswordResetTokenHash';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Allowed MVP values: User, Admin',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Roles',
@level2type = N'Column', @level2name = 'Name';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'chặn xoá role gốc',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Roles',
@level2type = N'Column', @level2name = 'IsSystem';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Represents one authenticated browser/device instance for Manage Devices. Removing a device revokes access by setting RevokedAt and revoking all active RefreshTokens linked to that DeviceId. Device identity is based on a random persisted identifier, not fingerprinting by IP/User-Agent.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Devices';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Hash of a server-issued/client-persisted random device identifier; do not use IP or User-Agent as the unique identity',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Devices',
@level2type = N'Column', @level2name = 'DeviceIdentifierHash';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Friendly display name such as Chrome on Windows or Safari on iPhone',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Devices',
@level2type = N'Column', @level2name = 'DeviceName';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Example values: Web, Mobile, Tablet, TV; exact values are defined by application logic',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Devices',
@level2type = N'Column', @level2name = 'DeviceType';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Most recently observed IPv4/IPv6 address; informational only, not a stable device identifier',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Devices',
@level2type = N'Column', @level2name = 'IpAddress';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Set when the user removes/signs out this device. The row is retained for history instead of being hard-deleted.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Devices',
@level2type = N'Column', @level2name = 'RevokedAt';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Viewing/personalization identity. PIN is optional and must be stored as a hash when configured. Profile deletion uses soft delete: active profiles have IsDeleted = 0 and DeletedAt = NULL; deleted profiles remain in the database for historical/analytics integrity but cannot be selected or used for new viewing/personalization actions. The maximum number of active Profiles is enforced from the owning account active Subscription Plan.MaxProfiles.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Profiles';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'hồ sơ Kids để 13; lọc WHERE Movies.MinAge <= MaturityLevel',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Profiles',
@level2type = N'Column', @level2name = 'MaturityLevel';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Refresh token lifecycle for UserAccount and Device. RevokedAt is null while token is active. ReplacedByTokenId links a rotated token to its replacement token. DeviceId allows all token chains for one device to be revoked during remote sign-out. Application/domain logic must ensure DeviceId belongs to the same UserAccountId.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'RefreshTokens';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Self-reference to the next refresh token created during token rotation',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'RefreshTokens',
@level2type = N'Column', @level2name = 'ReplacedByTokenId';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Subscription plan configuration. MaxProfiles limits active Profiles for an account; MaxConcurrentStreams limits simultaneous active ViewingSessions across all Profiles owned by that account. MaxQuality is reserved for playback-quality enforcement when multi-quality video assets are introduced.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Plans';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Example values: 480p, 720p, 1080p, 4K',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Plans',
@level2type = N'Column', @level2name = 'MaxQuality';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Subscription history for a UserAccount. An account may have many subscriptions over time, but application/domain logic must ensure at most one Active subscription at a time.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Subscriptions';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Allowed MVP values: Pending, Active, Expired, Cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Subscriptions',
@level2type = N'Column', @level2name = 'Status';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Simulated payment transaction history. TransactionCode uniquely identifies a simulated payment attempt. Successful payment activates or renews the related Subscription through application/domain logic.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'PaymentTransactions';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Simulated payment method; exact allowed values are defined by application logic',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'PaymentTransactions',
@level2type = N'Column', @level2name = 'Method';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Allowed MVP values: Pending, Succeeded, Failed',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'PaymentTransactions',
@level2type = N'Column', @level2name = 'Status';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Movie-only MVP. Admin deletion uses soft delete through IsDeleted so historical references are retained. IsAvailable controls whether non-deleted content can currently be watched and is separate from soft-delete state.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Movies';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'nhãn hiển thị: P, T13, T16, T18',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Movies',
@level2type = N'Column', @level2name = 'MaturityRating';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'số tuổi tối thiểu — dùng để SO SÁNH',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Movies',
@level2type = N'Column', @level2name = 'MinAge';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Movie genre such as Action, Sci-Fi, Comedy.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Genres';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Many-to-many join table between Movies and Genres.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'MovieGenres';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Person that can be credited on a Movie, such as an actor or director. Movie-specific role information belongs in MovieCredits.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'People';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Credits shown on Movie detail pages. MovieCredits is used instead of MovieCasts because the table contains both cast (Actor) and crew (Director) relationships.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'MovieCredits';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Allowed MVP values: Actor, Director',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'MovieCredits',
@level2type = N'Column', @level2name = 'CreditType';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Used mainly for Actor credits; null for Director credits',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'MovieCredits',
@level2type = N'Column', @level2name = 'CharacterName';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'User-facing feature: My List. Each Movie can appear at most once in the same Profile My List.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'MyListItems';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Stores the current rating of a Profile for a Movie. Changing rating updates this record instead of creating a duplicate.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Ratings';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Allowed MVP values: NotForMe, Like, Love',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'Ratings',
@level2type = N'Column', @level2name = 'Value';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Initial movie preferences selected during first-time Profile onboarding. This is not the same as My List.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'ProfilePreferences';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Current playback state for one Profile + Movie. MVP marks Completed when progress reaches at least 90% of Movie runtime. IsHidden supports Remove from Continue Watching while retaining history for analytics/recommendation.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'WatchHistories';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Timestamp when the Movie was marked completed; null while IsCompleted = 0',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'WatchHistories',
@level2type = N'Column', @level2name = 'CompletedAt';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Hide this Movie from Continue Watching without deleting playback history or analytics data',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'WatchHistories',
@level2type = N'Column', @level2name = 'IsHidden';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Analytics playback session. StartedAt marks session start; EndedAt is NULL while active and set when playback finishes, the user leaves/closes it, or stale-session timeout logic closes it. Pause/Resume stays in the same session. WatchedSeconds counts actual watched time only. DeviceId identifies where playback occurred but concurrent-stream enforcement still counts active ViewingSessions, not Devices. Application/domain logic must ensure the Device and Profile belong to the same UserAccount. MVP Qualified View rule: WatchedSeconds >= 120, at most once per session.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'ViewingSessions';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Authenticated device/browser instance that started this playback session',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'ViewingSessions',
@level2type = N'Column', @level2name = 'DeviceId';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Informational/default playback quality in MVP; becomes authoritative when multi-quality VideoAssets are implemented',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'ViewingSessions',
@level2type = N'Column', @level2name = 'Quality';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Null while active. Allowed ended-session values: Finished, Closed, Timeout',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'ViewingSessions',
@level2type = N'Column', @level2name = 'EndReason';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'MVP audit trail for manual Admin account lock/unlock actions. It records who acted, which account was affected, what happened, why, and when. A broader future audit system may generalize this concept to Movies, Plans, Roles, and other administrative changes.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'AdminActionLogs';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Admin account that performed the action',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'AdminActionLogs',
@level2type = N'Column', @level2name = 'ActorUserAccountId';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'User account affected by the Admin action',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'AdminActionLogs',
@level2type = N'Column', @level2name = 'TargetUserAccountId';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Allowed MVP values: AccountLocked, AccountUnlocked',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'AdminActionLogs',
@level2type = N'Column', @level2name = 'Action';
GO

ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT[FK_UserRoles_UserAccounts] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccounts] ([Id])
GO

ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT[FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id])
GO

ALTER TABLE [dbo].[Devices] ADD CONSTRAINT[FK_Devices_UserAccounts] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccounts] ([Id])
GO

ALTER TABLE [dbo].[Profiles] ADD CONSTRAINT[FK_Profiles_UserAccounts] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccounts] ([Id])
GO

ALTER TABLE [dbo].[RefreshTokens] ADD CONSTRAINT[FK_RefreshTokens_UserAccounts] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccounts] ([Id])
GO

ALTER TABLE [dbo].[RefreshTokens] ADD CONSTRAINT[FK_RefreshTokens_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Devices] ([Id])
GO

ALTER TABLE [dbo].[RefreshTokens] ADD CONSTRAINT[FK_RefreshTokens_RefreshTokens] FOREIGN KEY ([ReplacedByTokenId]) REFERENCES [dbo].[RefreshTokens] ([Id])
GO

ALTER TABLE [dbo].[Subscriptions] ADD CONSTRAINT[FK_Subscriptions_UserAccounts] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccounts] ([Id])
GO

ALTER TABLE [dbo].[Subscriptions] ADD CONSTRAINT[FK_Subscriptions_Plans] FOREIGN KEY ([PlanId]) REFERENCES [dbo].[Plans] ([Id])
GO

ALTER TABLE [dbo].[PaymentTransactions] ADD CONSTRAINT[FK_PaymentTransactions_Subscriptions] FOREIGN KEY ([SubscriptionId]) REFERENCES [dbo].[Subscriptions] ([Id])
GO

ALTER TABLE [dbo].[MovieGenres] ADD CONSTRAINT[FK_MovieGenres_Movies] FOREIGN KEY ([MovieId]) REFERENCES [dbo].[Movies] ([Id])
GO

ALTER TABLE [dbo].[MovieGenres] ADD CONSTRAINT[FK_MovieGenres_Genres] FOREIGN KEY ([GenreId]) REFERENCES [dbo].[Genres] ([Id])
GO

ALTER TABLE [dbo].[MovieCredits] ADD CONSTRAINT[FK_MovieCredits_Movies] FOREIGN KEY ([MovieId]) REFERENCES [dbo].[Movies] ([Id])
GO

ALTER TABLE [dbo].[MovieCredits] ADD CONSTRAINT[FK_MovieCredits_People] FOREIGN KEY ([PersonId]) REFERENCES [dbo].[People] ([Id])
GO

ALTER TABLE [dbo].[MyListItems] ADD CONSTRAINT[FK_MyListItems_Profiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[Profiles] ([Id])
GO

ALTER TABLE [dbo].[MyListItems] ADD CONSTRAINT[FK_MyListItems_Movies] FOREIGN KEY ([MovieId]) REFERENCES [dbo].[Movies] ([Id])
GO

ALTER TABLE [dbo].[Ratings] ADD CONSTRAINT[FK_Ratings_Profiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[Profiles] ([Id])
GO

ALTER TABLE [dbo].[Ratings] ADD CONSTRAINT[FK_Ratings_Movies] FOREIGN KEY ([MovieId]) REFERENCES [dbo].[Movies] ([Id])
GO

ALTER TABLE [dbo].[ProfilePreferences] ADD CONSTRAINT[FK_ProfilePreferences_Profiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[Profiles] ([Id])
GO

ALTER TABLE [dbo].[ProfilePreferences] ADD CONSTRAINT[FK_ProfilePreferences_Movies] FOREIGN KEY ([MovieId]) REFERENCES [dbo].[Movies] ([Id])
GO

ALTER TABLE [dbo].[WatchHistories] ADD CONSTRAINT[FK_WatchHistories_Profiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[Profiles] ([Id])
GO

ALTER TABLE [dbo].[WatchHistories] ADD CONSTRAINT[FK_WatchHistories_Movies] FOREIGN KEY ([MovieId]) REFERENCES [dbo].[Movies] ([Id])
GO

ALTER TABLE [dbo].[ViewingSessions] ADD CONSTRAINT[FK_ViewingSessions_Profiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[Profiles] ([Id])
GO

ALTER TABLE [dbo].[ViewingSessions] ADD CONSTRAINT[FK_ViewingSessions_Movies] FOREIGN KEY ([MovieId]) REFERENCES [dbo].[Movies] ([Id])
GO

ALTER TABLE [dbo].[ViewingSessions] ADD CONSTRAINT[FK_ViewingSessions_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Devices] ([Id])
GO

ALTER TABLE [dbo].[AdminActionLogs] ADD CONSTRAINT[FK_AdminActionLogs_ActorUserAccounts] FOREIGN KEY ([ActorUserAccountId]) REFERENCES [dbo].[UserAccounts] ([Id])
GO

ALTER TABLE [dbo].[AdminActionLogs] ADD CONSTRAINT[FK_AdminActionLogs_TargetUserAccounts] FOREIGN KEY ([TargetUserAccountId]) REFERENCES [dbo].[UserAccounts] ([Id])
GO

-- -- check for tables and schemas
-- SELECT
--     s.name AS SchemaName,
--     t.name AS TableName
-- FROM sys.tables t
-- JOIN sys.schemas s
--     ON t.schema_id = s.schema_id
-- ORDER BY t.name;

-- -- check for foreign keys
-- SELECT
--     fk.name AS ForeignKeyName,
--     OBJECT_NAME(fk.parent_object_id) AS ChildTable,
--     OBJECT_NAME(fk.referenced_object_id) AS ParentTable
-- FROM sys.foreign_keys fk
-- ORDER BY ChildTable, ForeignKeyName;

-- -- check for check constraints
-- SELECT
--     OBJECT_NAME(parent_object_id) AS TableName,
--     name AS ConstraintName,
--     definition
-- FROM sys.check_constraints
-- ORDER BY TableName, ConstraintName;

-- -- check for unique constraints and indexes
-- SELECT
--     OBJECT_NAME(i.object_id) AS TableName,
--     i.name AS IndexName,
--     i.is_unique AS IsUnique
-- FROM sys.indexes i
-- WHERE
--     i.name IS NOT NULL
--     AND OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
-- ORDER BY TableName, IndexName;