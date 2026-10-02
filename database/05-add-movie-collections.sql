USE [NetflixCloneDb]
GO

-- One-time additive migration for an existing database. Do not run 01-schema.sql again.
-- No data is seeded. Existing Movies and their historical references are unchanged.
-- If either table already exists, stop instead of silently accepting a partial/different schema.
SET XACT_ABORT ON;

BEGIN TRY
  BEGIN TRANSACTION;

  IF OBJECT_ID(N'dbo.Movies', N'U') IS NULL
    THROW 50001, 'Movies must exist before adding movie collections.', 1;

  IF OBJECT_ID(N'dbo.MovieCollections', N'U') IS NOT NULL
     OR OBJECT_ID(N'dbo.MovieCollectionItems', N'U') IS NOT NULL
    THROW 50002, 'Movie collection tables already exist. Inspect the schema before applying this migration.', 1;

  -- Manually curated catalog rows; UTC timestamps are advanced by Application on mutation.
  CREATE TABLE [dbo].[MovieCollections] (
    [Id] int IDENTITY(1, 1) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [IsPublished] bit NOT NULL CONSTRAINT [DF_MovieCollections_IsPublished] DEFAULT (0),
    [DisplayOrder] int NOT NULL CONSTRAINT [DF_MovieCollections_DisplayOrder] DEFAULT (0),
    [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_MovieCollections_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_MovieCollections_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_MovieCollections] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_MovieCollections_Title] CHECK (LEN(LTRIM(RTRIM([Title]))) > 0),
    CONSTRAINT [CK_MovieCollections_DisplayOrder] CHECK ([DisplayOrder] >= 0)
  );

  CREATE INDEX [IX_MovieCollections_IsPublished_DisplayOrder]
    ON [dbo].[MovieCollections] ([IsPublished], [DisplayOrder], [Id]);

  CREATE TABLE [dbo].[MovieCollectionItems] (
    [CollectionId] int NOT NULL,
    [MovieId] int NOT NULL,
    [Position] int NOT NULL,
    CONSTRAINT [PK_MovieCollectionItems] PRIMARY KEY ([CollectionId], [MovieId]),
    CONSTRAINT [CK_MovieCollectionItems_Position] CHECK ([Position] >= 0),
    CONSTRAINT [FK_MovieCollectionItems_MovieCollections] FOREIGN KEY ([CollectionId])
      REFERENCES [dbo].[MovieCollections] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MovieCollectionItems_Movies] FOREIGN KEY ([MovieId])
      REFERENCES [dbo].[Movies] ([Id]) ON DELETE NO ACTION
  );

  -- Position is intentionally not unique: EF can update several positions in one commit.
  -- Read order is Position, MovieId; Application writes consecutive zero-based positions.
  CREATE INDEX [IX_MovieCollectionItems_CollectionId_Position]
    ON [dbo].[MovieCollectionItems] ([CollectionId], [Position], [MovieId]);

  CREATE INDEX [IX_MovieCollectionItems_MovieId]
    ON [dbo].[MovieCollectionItems] ([MovieId]);

  COMMIT TRANSACTION;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
  THROW;
END CATCH;
GO
