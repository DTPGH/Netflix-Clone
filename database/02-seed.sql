USE [NetflixCloneDb];
GO

SET XACT_ABORT ON;
GO

-- Seed data if not exists
BEGIN TRY
    BEGIN TRANSACTION;

    -- =========================
    -- Roles
    -- =========================

    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[Roles]
        WHERE [Name] = N'User'
    )
    BEGIN
        INSERT INTO [dbo].[Roles]
        (
            [Name],
            [Description],
            [IsSystem]
        )
        VALUES
        (
            N'User',
            N'Standard application user',
            1
        );
    END;


    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[Roles]
        WHERE [Name] = N'Admin'
    )
    BEGIN
        INSERT INTO [dbo].[Roles]
        (
            [Name],
            [Description],
            [IsSystem]
        )
        VALUES
        (
            N'Admin',
            N'System administrator',
            1
        );
    END;

        -- =========================
    -- Plans
    -- =========================

    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[Plans]
        WHERE [Name] = N'Basic'
    )
    BEGIN
        INSERT INTO [dbo].[Plans]
        (
            [Name],
            [Price],
            [MaxProfiles],
            [MaxConcurrentStreams],
            [MaxQuality],
            [IsActive]
        )
        VALUES
        (
            N'Basic',
            79000,
            1,
            1,
            N'720p',
            1
        );
    END;


    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[Plans]
        WHERE [Name] = N'Standard'
    )
    BEGIN
        INSERT INTO [dbo].[Plans]
        (
            [Name],
            [Price],
            [MaxProfiles],
            [MaxConcurrentStreams],
            [MaxQuality],
            [IsActive]
        )
        VALUES
        (
            N'Standard',
            129000,
            3,
            2,
            N'1080p',
            1
        );
    END;


    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[Plans]
        WHERE [Name] = N'Premium'
    )
    BEGIN
        INSERT INTO [dbo].[Plans]
        (
            [Name],
            [Price],
            [MaxProfiles],
            [MaxConcurrentStreams],
            [MaxQuality],
            [IsActive]
        )
        VALUES
        (
            N'Premium',
            199000,
            5,
            4,
            N'4K',
            1
        );
    END;

    -- =========================
    -- Genres
    -- =========================

    DECLARE @Genres TABLE
    (
        [Name] NVARCHAR(100) NOT NULL
    );

    INSERT INTO @Genres ([Name])
    VALUES
        (N'Action'),
        (N'Adventure'),
        (N'Animation'),
        (N'Comedy'),
        (N'Crime'),
        (N'Documentary'),
        (N'Drama'),
        (N'Family'),
        (N'Fantasy'),
        (N'Horror'),
        (N'Romance'),
        (N'Sci-Fi'),
        (N'Thriller');


    INSERT INTO [dbo].[Genres] ([Name])
    SELECT g.[Name]
    FROM @Genres g
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[Genres] existing
        WHERE existing.[Name] = g.[Name]
    );


    COMMIT TRANSACTION;
END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;

END CATCH;
GO

SELECT COUNT(*) AS RoleCount
FROM [dbo].[Roles];

SELECT COUNT(*) AS PlanCount
FROM [dbo].[Plans];

SELECT COUNT(*) AS GenreCount
FROM [dbo].[Genres];