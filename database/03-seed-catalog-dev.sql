/* ============================================================
   NetflixClone - Development Catalog Seed
   Source metadata: TMDB; reviewed 2026-09-21.
   Sources: https://www.themoviedb.org/movie/693134
            https://www.themoviedb.org/movie/569094
            https://www.themoviedb.org/movie/872585
            https://www.themoviedb.org/movie/568124
            https://www.themoviedb.org/movie/447365
   Descriptions are short English paraphrases. Credits are a selected subset.
   Original US release dates and runtime fixtures are retained.
   P/T13/T18 + MinAge are dev fixtures, not verified Vietnamese certifications.
   Run sequentially, not concurrently; no schema change or asset download.
   Existing descriptions/status/assets are preserved; blank descriptions and image URLs are enriched.
   Image URLs are relative to the Web origin; files live in its wwwroot/images/movies.
   Purpose: local development/testing only
   ============================================================ */

USE [NetflixCloneDb];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT <> 0 THROW 51000, 'Run this seed outside an existing transaction.', 1;
BEGIN TRY
BEGIN TRANSACTION;

DECLARE @SeedPeople TABLE (FullName nvarchar(200) PRIMARY KEY);
INSERT INTO @SeedPeople VALUES (N'Denis Villeneuve'), (N'Timothée Chalamet'), (N'Zendaya'), (N'Joaquim Dos Santos'), (N'Kemp Powers'), (N'Justin K. Thompson'), (N'Shameik Moore'), (N'Hailee Steinfeld'), (N'Christopher Nolan'), (N'Cillian Murphy'), (N'Emily Blunt'), (N'Jared Bush'), (N'Byron Howard'), (N'Stephanie Beatriz'), (N'John Leguizamo'), (N'James Gunn'), (N'Chris Pratt'), (N'Zoe Saldaña');
IF EXISTS (SELECT p.FullName FROM dbo.People p JOIN @SeedPeople s ON s.FullName = p.FullName
    GROUP BY p.FullName HAVING COUNT(*) > 1)
    THROW 51001, 'Ambiguous person name. Resolve duplicates before seeding.', 1;

-- Required local taxonomy; Science Fiction from TMDB maps to Sci-Fi.
DECLARE @RequiredGenres TABLE (Name nvarchar(100) PRIMARY KEY);
INSERT INTO @RequiredGenres VALUES
(N'Sci-Fi'), (N'Adventure'), (N'Animation'), (N'Action'), (N'Drama'),
(N'History'), (N'Comedy'), (N'Family'), (N'Fantasy');
INSERT INTO dbo.Genres (Name, CreatedAt)
SELECT s.Name, SYSUTCDATETIME() FROM @RequiredGenres s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Genres g WHERE g.Name = s.Name);
IF EXISTS (SELECT 1 FROM @RequiredGenres s WHERE NOT EXISTS (SELECT 1 FROM dbo.Genres g WHERE g.Name = s.Name))
    THROW 51002, 'Required genre could not be resolved.', 1;


---------------------------------------------------------------
-- 1. PEOPLE
---------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Denis Villeneuve')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Denis Villeneuve', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Timothée Chalamet')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Timothée Chalamet', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Zendaya')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Zendaya', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;


IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Joaquim Dos Santos')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Joaquim Dos Santos', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Kemp Powers')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Kemp Powers', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Justin K. Thompson')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Justin K. Thompson', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Shameik Moore')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Shameik Moore', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Hailee Steinfeld')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Hailee Steinfeld', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;


IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Christopher Nolan')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Christopher Nolan', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Cillian Murphy')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Cillian Murphy', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Emily Blunt')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Emily Blunt', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;


IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Jared Bush')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Jared Bush', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Byron Howard')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Byron Howard', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Stephanie Beatriz')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Stephanie Beatriz', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'John Leguizamo')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'John Leguizamo', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;


IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'James Gunn')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'James Gunn', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Chris Pratt')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Chris Pratt', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE FullName = N'Zoe Saldaña')
BEGIN
    INSERT INTO dbo.People
        (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
    VALUES
        (N'Zoe Saldaña', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;


---------------------------------------------------------------
-- 2. MOVIES
---------------------------------------------------------------

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Movies
    WHERE Title = N'Dune: Part Two'
      AND ReleaseDate = '2024-03-01'
)
BEGIN
    INSERT INTO dbo.Movies
    (
        Title,
        Description,
        ReleaseDate,
        DurationSeconds,
        ThumbnailUrl,
        BackdropUrl,
        TrailerUrl,
        VideoUrl,
        MaturityRating,
        MinAge,
        IsDeleted,
        IsFeatured,
        IsAvailable,
        CreatedAt,
        UpdatedAt
    )
    VALUES
    (
        N'Dune: Part Two',
        N'Paul Atreides joins Chani and the Fremen against those who destroyed his family, while his choices threaten to shape a dangerous future.',
        '2024-03-01',
        10020, -- 167 minutes
        N'/images/movies/dune-part-two-poster.webp',
        N'/images/movies/dune-part-two-backdrop.webp',
        NULL,
        NULL,
        N'T13',
        13,
        0,
        1,
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Movies
    WHERE Title = N'Spider-Man: Across the Spider-Verse'
      AND ReleaseDate = '2023-06-02'
)
BEGIN
    INSERT INTO dbo.Movies
    (
        Title,
        Description,
        ReleaseDate,
        DurationSeconds,
        ThumbnailUrl,
        BackdropUrl,
        TrailerUrl,
        VideoUrl,
        MaturityRating,
        MinAge,
        IsDeleted,
        IsFeatured,
        IsAvailable,
        CreatedAt,
        UpdatedAt
    )
    VALUES
    (
        N'Spider-Man: Across the Spider-Verse',
        N'Reunited with Gwen, Miles Morales discovers a society of Spider-People across the multiverse. Their conflicting beliefs force him to choose his own way to protect those he loves.',
        '2023-06-02',
        8400, -- 140 minutes
        N'/images/movies/Spider-Man-Across-the-Spider-Verse-poster.webp',
        N'/images/movies/Spider-Man-Across-the-Spider-Verse-backdrop.webp',
        NULL,
        NULL,
        N'P',
        0,
        0,
        1,
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Movies
    WHERE Title = N'Oppenheimer'
      AND ReleaseDate = '2023-07-21'
)
BEGIN
    INSERT INTO dbo.Movies
    (
        Title,
        Description,
        ReleaseDate,
        DurationSeconds,
        ThumbnailUrl,
        BackdropUrl,
        TrailerUrl,
        VideoUrl,
        MaturityRating,
        MinAge,
        IsDeleted,
        IsFeatured,
        IsAvailable,
        CreatedAt,
        UpdatedAt
    )
    VALUES
    (
        N'Oppenheimer',
        N'Physicist J. Robert Oppenheimer plays a central role in developing the atomic bomb during World War II.',
        '2023-07-21',
        10860, -- 181 minutes
        N'/images/movies/Oppenheimer-poster.webp',
        N'/images/movies/Oppenheimer-backdrop.webp',
        NULL,
        NULL,
        N'T18',
        18,
        0,
        1,
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Movies
    WHERE Title = N'Encanto'
      AND ReleaseDate = '2021-11-24'
)
BEGIN
    INSERT INTO dbo.Movies
    (
        Title,
        Description,
        ReleaseDate,
        DurationSeconds,
        ThumbnailUrl,
        BackdropUrl,
        TrailerUrl,
        VideoUrl,
        MaturityRating,
        MinAge,
        IsDeleted,
        IsFeatured,
        IsAvailable,
        CreatedAt,
        UpdatedAt
    )
    VALUES
    (
        N'Encanto',
        N'Mirabel is the only child in the Madrigal family without a magical gift. When their enchanted home is threatened, she steps forward to protect her family.',
        '2021-11-24',
        6120, -- 102 minutes
        N'/images/movies/Encanto-poster.webp',
        N'/images/movies/Encanto-backdrop.webp',
        NULL,
        NULL,
        N'P',
        0,
        0,
        0,
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Movies
    WHERE Title = N'Guardians of the Galaxy Vol. 3'
      AND ReleaseDate = '2023-05-05'
)
BEGIN
    INSERT INTO dbo.Movies
    (
        Title,
        Description,
        ReleaseDate,
        DurationSeconds,
        ThumbnailUrl,
        BackdropUrl,
        TrailerUrl,
        VideoUrl,
        MaturityRating,
        MinAge,
        IsDeleted,
        IsFeatured,
        IsAvailable,
        CreatedAt,
        UpdatedAt
    )
    VALUES
    (
        N'Guardians of the Galaxy Vol. 3',
        N'Still grieving Gamora, Peter Quill brings the Guardians together to protect one of their own in a mission that could determine the team''s future.',
        '2023-05-05',
        9000, -- 150 minutes
        N'/images/movies/Guardians-of-the-Galaxy-Vol-3-poster.webp',
        N'/images/movies/Guardians-of-the-Galaxy-Vol-3-backdrop.webp',
        NULL,
        NULL,
        N'T13',
        13,
        0,
        0,
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;


---------------------------------------------------------------
-- 3. MOVIE GENRES
---------------------------------------------------------------

DECLARE @DuneId INT =
(
    SELECT Id
    FROM dbo.Movies
    WHERE Title = N'Dune: Part Two'
      AND ReleaseDate = '2024-03-01'
);

DECLARE @SpiderVerseId INT =
(
    SELECT Id
    FROM dbo.Movies
    WHERE Title = N'Spider-Man: Across the Spider-Verse'
      AND ReleaseDate = '2023-06-02'
);

DECLARE @OppenheimerId INT =
(
    SELECT Id
    FROM dbo.Movies
    WHERE Title = N'Oppenheimer'
      AND ReleaseDate = '2023-07-21'
);

DECLARE @EncantoId INT =
(
    SELECT Id
    FROM dbo.Movies
    WHERE Title = N'Encanto'
      AND ReleaseDate = '2021-11-24'
);

DECLARE @Guardians3Id INT =
(
    SELECT Id
    FROM dbo.Movies
    WHERE Title = N'Guardians of the Galaxy Vol. 3'
      AND ReleaseDate = '2023-05-05'
);


-- IMPORTANT:
-- Required local genres were seeded above. Scalar lookups fail on ambiguous movie matches.

DECLARE @ScienceFictionGenreId INT =
(
    SELECT Id
    FROM dbo.Genres
    WHERE Name = N'Sci-Fi'
);

DECLARE @AdventureGenreId INT =
(
    SELECT Id FROM dbo.Genres WHERE Name = N'Adventure'
);

DECLARE @AnimationGenreId INT =
(
    SELECT Id FROM dbo.Genres WHERE Name = N'Animation'
);

DECLARE @ActionGenreId INT =
(
    SELECT Id FROM dbo.Genres WHERE Name = N'Action'
);

DECLARE @DramaGenreId INT =
(
    SELECT Id FROM dbo.Genres WHERE Name = N'Drama'
);

DECLARE @HistoryGenreId INT =
(
    SELECT Id FROM dbo.Genres WHERE Name = N'History'
);

DECLARE @ComedyGenreId INT =
(
    SELECT Id FROM dbo.Genres WHERE Name = N'Comedy'
);

DECLARE @FamilyGenreId INT =
(
    SELECT Id FROM dbo.Genres WHERE Name = N'Family'
);

DECLARE @FantasyGenreId INT =
(
    SELECT Id FROM dbo.Genres WHERE Name = N'Fantasy'
);


---------------------------------------------------------------
-- Helper-style explicit inserts
---------------------------------------------------------------

-- Dune: Part Two
IF @DuneId IS NOT NULL
AND @ScienceFictionGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @DuneId
      AND GenreId = @ScienceFictionGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@DuneId, @ScienceFictionGenreId);
END;

IF @DuneId IS NOT NULL
AND @AdventureGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @DuneId
      AND GenreId = @AdventureGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@DuneId, @AdventureGenreId);
END;


-- Spider-Verse
IF @SpiderVerseId IS NOT NULL
AND @AnimationGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @SpiderVerseId
      AND GenreId = @AnimationGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@SpiderVerseId, @AnimationGenreId);
END;

IF @SpiderVerseId IS NOT NULL
AND @ActionGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @SpiderVerseId
      AND GenreId = @ActionGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@SpiderVerseId, @ActionGenreId);
END;

IF @SpiderVerseId IS NOT NULL
AND @AdventureGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @SpiderVerseId
      AND GenreId = @AdventureGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@SpiderVerseId, @AdventureGenreId);
END;

IF @SpiderVerseId IS NOT NULL
AND @ScienceFictionGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @SpiderVerseId
      AND GenreId = @ScienceFictionGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@SpiderVerseId, @ScienceFictionGenreId);
END;


-- Oppenheimer
IF @OppenheimerId IS NOT NULL
AND @DramaGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @OppenheimerId
      AND GenreId = @DramaGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@OppenheimerId, @DramaGenreId);
END;

IF @OppenheimerId IS NOT NULL
AND @HistoryGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @OppenheimerId
      AND GenreId = @HistoryGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@OppenheimerId, @HistoryGenreId);
END;


-- Encanto
IF @EncantoId IS NOT NULL
AND @AnimationGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @EncantoId
      AND GenreId = @AnimationGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@EncantoId, @AnimationGenreId);
END;

IF @EncantoId IS NOT NULL
AND @ComedyGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @EncantoId
      AND GenreId = @ComedyGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@EncantoId, @ComedyGenreId);
END;

IF @EncantoId IS NOT NULL
AND @FamilyGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @EncantoId
      AND GenreId = @FamilyGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@EncantoId, @FamilyGenreId);
END;

IF @EncantoId IS NOT NULL
AND @FantasyGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @EncantoId
      AND GenreId = @FantasyGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@EncantoId, @FantasyGenreId);
END;


-- Guardians Vol. 3
IF @Guardians3Id IS NOT NULL
AND @ScienceFictionGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @Guardians3Id
      AND GenreId = @ScienceFictionGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@Guardians3Id, @ScienceFictionGenreId);
END;

IF @Guardians3Id IS NOT NULL
AND @AdventureGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @Guardians3Id
      AND GenreId = @AdventureGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@Guardians3Id, @AdventureGenreId);
END;

IF @Guardians3Id IS NOT NULL
AND @ActionGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @Guardians3Id
      AND GenreId = @ActionGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@Guardians3Id, @ActionGenreId);
END;

IF @Guardians3Id IS NOT NULL
AND @ComedyGenreId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1 FROM dbo.MovieGenres
    WHERE MovieId = @Guardians3Id
      AND GenreId = @ComedyGenreId
)
BEGIN
    INSERT INTO dbo.MovieGenres(MovieId, GenreId)
    VALUES (@Guardians3Id, @ComedyGenreId);
END;

---------------------------------------------------------------
-- 4. CREDITS AND ENRICHMENT
---------------------------------------------------------------
IF @DuneId IS NULL OR @SpiderVerseId IS NULL OR @OppenheimerId IS NULL OR @EncantoId IS NULL OR @Guardians3Id IS NULL
    THROW 51003, 'Could not resolve all five movies.', 1;

DECLARE @SeedCredits TABLE
(
    MovieId int NOT NULL,
    FullName nvarchar(200) NOT NULL,
    CreditType nvarchar(20) NOT NULL,
    CharacterName nvarchar(200) NULL
);
INSERT INTO @SeedCredits VALUES
(@DuneId, N'Denis Villeneuve', N'Director', NULL),
(@DuneId, N'Timothée Chalamet', N'Actor', N'Paul Atreides'),
(@DuneId, N'Zendaya', N'Actor', N'Chani'),
(@SpiderVerseId, N'Joaquim Dos Santos', N'Director', NULL),
(@SpiderVerseId, N'Kemp Powers', N'Director', NULL),
(@SpiderVerseId, N'Justin K. Thompson', N'Director', NULL),
(@SpiderVerseId, N'Shameik Moore', N'Actor', N'Miles Morales (voice)'),
(@SpiderVerseId, N'Hailee Steinfeld', N'Actor', N'Gwen Stacy (voice)'),
(@OppenheimerId, N'Christopher Nolan', N'Director', NULL),
(@OppenheimerId, N'Cillian Murphy', N'Actor', N'J. Robert Oppenheimer'),
(@OppenheimerId, N'Emily Blunt', N'Actor', N'Kitty Oppenheimer'),
(@EncantoId, N'Jared Bush', N'Director', NULL),
(@EncantoId, N'Byron Howard', N'Director', NULL),
(@EncantoId, N'Stephanie Beatriz', N'Actor', N'Mirabel Madrigal (voice)'),
(@EncantoId, N'John Leguizamo', N'Actor', N'Bruno Madrigal (voice)'),
(@Guardians3Id, N'James Gunn', N'Director', NULL),
(@Guardians3Id, N'Chris Pratt', N'Actor', N'Peter Quill / Star-Lord'),
(@Guardians3Id, N'Zoe Saldaña', N'Actor', N'Gamora');

IF EXISTS (SELECT 1 FROM @SeedCredits s WHERE NOT EXISTS (SELECT 1 FROM dbo.People p WHERE p.FullName = s.FullName))
    THROW 51004, 'A required credit person is missing.', 1;

INSERT INTO dbo.MovieCredits (MovieId, PersonId, CreditType, CharacterName, CreatedAt)
SELECT s.MovieId, p.Id, s.CreditType, s.CharacterName, SYSUTCDATETIME()
FROM @SeedCredits s JOIN dbo.People p ON p.FullName = s.FullName
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.MovieCredits c
    WHERE c.MovieId = s.MovieId AND c.PersonId = p.Id AND c.CreditType = s.CreditType
      AND (c.CharacterName = s.CharacterName OR (c.CharacterName IS NULL AND s.CharacterName IS NULL))
);

-- Fill only blank descriptions from earlier runs; never overwrite authored text or resurrect a movie.
DECLARE @Descriptions TABLE (Title nvarchar(255), ReleaseDate date, Description nvarchar(2000));
INSERT INTO @Descriptions VALUES
(N'Dune: Part Two', N'2024-03-01', N'Paul Atreides joins Chani and the Fremen against those who destroyed his family, while his choices threaten to shape a dangerous future.'),
(N'Spider-Man: Across the Spider-Verse', N'2023-06-02', N'Reunited with Gwen, Miles Morales discovers a society of Spider-People across the multiverse. Their conflicting beliefs force him to choose his own way to protect those he loves.'),
(N'Oppenheimer', N'2023-07-21', N'Physicist J. Robert Oppenheimer plays a central role in developing the atomic bomb during World War II.'),
(N'Encanto', N'2021-11-24', N'Mirabel is the only child in the Madrigal family without a magical gift. When their enchanted home is threatened, she steps forward to protect her family.'),
(N'Guardians of the Galaxy Vol. 3', N'2023-05-05', N'Still grieving Gamora, Peter Quill brings the Guardians together to protect one of their own in a mission that could determine the team''s future.');
UPDATE m SET Description = s.Description, UpdatedAt = SYSUTCDATETIME()
FROM dbo.Movies m JOIN @Descriptions s ON m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate
WHERE m.IsDeleted = 0 AND NULLIF(LTRIM(RTRIM(m.Description)), N'') IS NULL;

-- Enrich earlier seed runs without overwriting existing images or resurrecting deleted movies.
DECLARE @MovieImages TABLE
(
    Title nvarchar(255),
    ReleaseDate date,
    ThumbnailUrl nvarchar(500),
    BackdropUrl nvarchar(500)
);
INSERT INTO @MovieImages VALUES
(N'Dune: Part Two', '2024-03-01', N'/images/movies/dune-part-two-poster.webp', N'/images/movies/dune-part-two-backdrop.webp'),
(N'Spider-Man: Across the Spider-Verse', '2023-06-02', N'/images/movies/Spider-Man-Across-the-Spider-Verse-poster.webp', N'/images/movies/Spider-Man-Across-the-Spider-Verse-backdrop.webp'),
(N'Oppenheimer', '2023-07-21', N'/images/movies/Oppenheimer-poster.webp', N'/images/movies/Oppenheimer-backdrop.webp'),
(N'Encanto', '2021-11-24', N'/images/movies/Encanto-poster.webp', N'/images/movies/Encanto-backdrop.webp'),
(N'Guardians of the Galaxy Vol. 3', '2023-05-05', N'/images/movies/Guardians-of-the-Galaxy-Vol-3-poster.webp', N'/images/movies/Guardians-of-the-Galaxy-Vol-3-backdrop.webp');

UPDATE m SET
    ThumbnailUrl = CASE WHEN NULLIF(LTRIM(RTRIM(m.ThumbnailUrl)), N'') IS NULL
        THEN s.ThumbnailUrl ELSE m.ThumbnailUrl END,
    BackdropUrl = CASE WHEN NULLIF(LTRIM(RTRIM(m.BackdropUrl)), N'') IS NULL
        THEN s.BackdropUrl ELSE m.BackdropUrl END,
    UpdatedAt = SYSUTCDATETIME()
FROM dbo.Movies m
JOIN @MovieImages s ON m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate
WHERE m.IsDeleted = 0
  AND (NULLIF(LTRIM(RTRIM(m.ThumbnailUrl)), N'') IS NULL
    OR NULLIF(LTRIM(RTRIM(m.BackdropUrl)), N'') IS NULL);


---------------------------------------------------------------
-- 5. TWENTY ADDITIONAL DEVELOPMENT MOVIES (25 TOTAL)
---------------------------------------------------------------
-- Sources: https://www.themoviedb.org/movie/<TmdbId>
-- TmdbId is a seed-only source key, NEVER the local dbo.Movies.Id.
-- US theatrical dates; theatrical runtimes (not extended editions).
-- Descriptions are paraphrases; credits are selected directors + two actors.
-- Your Name. is a 2016 Japanese film; its US theatrical release was 2017-04-07.
-- P/T13/T16/T18 are DEVELOPMENT FIXTURES, not verified local certifications.
DECLARE @AdditionalMovies TABLE
(
    TmdbId int PRIMARY KEY,
    Title nvarchar(255) NOT NULL,
    ReleaseDate date NOT NULL,
    DurationSeconds int NOT NULL,
    AssetStem nvarchar(100) NOT NULL,
    MinAge tinyint NOT NULL,
    IsFeatured bit NOT NULL,
    Description nvarchar(2000) NOT NULL,
    MovieId int NULL
);
INSERT INTO @AdditionalMovies
    (TmdbId, Title, ReleaseDate, DurationSeconds, AssetStem, MinAge, IsFeatured, Description)
VALUES
(157336, N'Interstellar', '2014-11-07', 10140, N'interstellar', 13, 1, N'A former pilot leaves his children behind to search beyond the solar system for a new home as Earth becomes increasingly uninhabitable.'),
(286217, N'The Martian', '2015-10-02', 8460, N'the-martian', 13, 0, N'Stranded on Mars, an astronaut uses science and ingenuity to survive while people on Earth race to bring him home.'),
(354912, N'Coco', '2017-11-22', 6300, N'coco', 0, 1, N'A young aspiring musician enters the Land of the Dead and uncovers the family history behind a generations-old ban on music.'),
(150540, N'Inside Out', '2015-06-19', 5700, N'inside-out', 0, 0, N'After Riley moves to a new city, Joy and Sadness must find their way through her mind as her other emotions struggle to guide her.'),
(155, N'The Dark Knight', '2008-07-18', 9120, N'the-dark-knight', 16, 1, N'Batman joins forces with Gotham''s authorities against organized crime, but the Joker turns their campaign into a test of the city''s moral limits.'),
(361743, N'Top Gun: Maverick', '2022-05-27', 7860, N'top-gun-maverick', 13, 1, N'Maverick returns to train elite pilots for a dangerous mission, confronting his past while preparing a new generation to face extraordinary risks.'),
(546554, N'Knives Out', '2019-11-27', 7860, N'knives-out', 13, 0, N'Detective Benoit Blanc investigates a wealthy novelist''s death, untangling conflicting accounts from a family with plenty to hide.'),
(313369, N'La La Land', '2016-12-09', 7740, N'la-la-land', 13, 0, N'An aspiring actress and a jazz pianist fall in love in Los Angeles, where their ambitions begin pulling their lives in different directions.'),
(138843, N'The Conjuring', '2013-07-19', 6720, N'the-conjuring', 18, 0, N'Paranormal investigators Ed and Lorraine Warren help a family whose rural home is haunted by an increasingly dangerous presence.'),
(120, N'The Lord of the Rings: The Fellowship of the Ring', '2001-12-19', 10740, N'lotr-fellowship-of-the-ring', 13, 1, N'Frodo leaves the Shire with a powerful ring and joins a fellowship tasked with destroying it before a dark ruler can reclaim it.');

-- Second batch. Source IDs also locate the corresponding TMDB movie pages.
INSERT INTO @AdditionalMovies
    (TmdbId, Title, ReleaseDate, DurationSeconds, AssetStem, MinAge, IsFeatured, Description)
VALUES
(27205, N'Inception', '2010-07-16', 8880, N'inception', 13, 1, N'A thief who steals secrets through dreams accepts a dangerous mission to plant an idea in a target''s mind.'),
(329865, N'Arrival', '2016-11-11', 6960, N'arrival', 13, 0, N'A linguist studies the language of alien visitors as governments struggle to understand their intentions and avoid global conflict.'),
(269149, N'Zootopia', '2016-03-04', 6540, N'zootopia', 0, 1, N'A rookie rabbit police officer teams up with a streetwise fox to investigate disappearances in a city shared by many animal species.'),
(1184918, N'The Wild Robot', '2024-09-27', 6120, N'the-wild-robot', 0, 1, N'A robot stranded on a remote island learns to live among wild animals and becomes the guardian of an orphaned gosling.'),
(496243, N'Parasite', '2019-10-11', 7920, N'parasite', 18, 0, N'Members of a struggling family find work in a wealthy household, setting off a chain of deception and increasingly dangerous discoveries.'),
(244786, N'Whiplash', '2014-10-10', 6420, N'whiplash', 16, 0, N'An ambitious young drummer trains under a demanding instructor whose methods push the pursuit of musical excellence toward obsession.'),
(76341, N'Mad Max: Fury Road', '2015-05-15', 7200, N'mad-max-fury-road', 18, 1, N'In a desert wasteland, Max joins Furiosa and a group of fugitives in a desperate escape from a tyrant and his pursuing army.'),
(120467, N'The Grand Budapest Hotel', '2014-03-07', 6000, N'the-grand-budapest-hotel', 16, 0, N'A celebrated concierge and his young lobby boy become entangled in a disputed inheritance, a stolen painting and a murder accusation.'),
(447332, N'A Quiet Place', '2018-04-06', 5460, N'a-quiet-place', 16, 0, N'A family survives in near silence while creatures that hunt by sound turn even ordinary daily activities into deadly risks.'),
(372058, N'Your Name.', '2017-04-07', 6360, N'your-name', 13, 1, N'Two teenagers living far apart mysteriously exchange bodies and search for each other as their connection reveals a threat beyond their understanding.');

IF EXISTS (
    SELECT s.TmdbId FROM @AdditionalMovies s
    JOIN dbo.Movies m ON m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate
    GROUP BY s.TmdbId HAVING COUNT(*) > 1
)
    THROW 51005, 'Ambiguous additional movie. Resolve duplicate title/date before seeding.', 1;

INSERT INTO dbo.Movies
    (Title, Description, ReleaseDate, DurationSeconds, ThumbnailUrl, BackdropUrl,
     TrailerUrl, VideoUrl, MaturityRating, MinAge, IsDeleted, IsFeatured, IsAvailable, CreatedAt, UpdatedAt)
SELECT s.Title, s.Description, s.ReleaseDate, s.DurationSeconds,
    N'/images/movies/' + s.AssetStem + N'-poster.webp',
    N'/images/movies/' + s.AssetStem + N'-backdrop.webp',
    NULL, NULL, CASE WHEN s.MinAge = 0 THEN N'P' ELSE N'T' + CONVERT(nvarchar(3), s.MinAge) END,
    s.MinAge, 0, s.IsFeatured, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @AdditionalMovies s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Movies m WHERE m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate);

UPDATE s SET MovieId = m.Id
FROM @AdditionalMovies s
JOIN dbo.Movies m ON m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate;
IF EXISTS (SELECT 1 FROM @AdditionalMovies WHERE MovieId IS NULL)
    THROW 51006, 'Could not resolve all additional movies.', 1;

-- Fill each blank field independently; preserve authored metadata and soft-delete state.
UPDATE m SET
    Description = CASE WHEN NULLIF(LTRIM(RTRIM(m.Description)), N'') IS NULL THEN s.Description ELSE m.Description END,
    ThumbnailUrl = CASE WHEN NULLIF(LTRIM(RTRIM(m.ThumbnailUrl)), N'') IS NULL
        THEN N'/images/movies/' + s.AssetStem + N'-poster.webp' ELSE m.ThumbnailUrl END,
    BackdropUrl = CASE WHEN NULLIF(LTRIM(RTRIM(m.BackdropUrl)), N'') IS NULL
        THEN N'/images/movies/' + s.AssetStem + N'-backdrop.webp' ELSE m.BackdropUrl END,
    UpdatedAt = SYSUTCDATETIME()
FROM dbo.Movies m JOIN @AdditionalMovies s ON s.MovieId = m.Id
WHERE m.IsDeleted = 0 AND (
    NULLIF(LTRIM(RTRIM(m.Description)), N'') IS NULL OR
    NULLIF(LTRIM(RTRIM(m.ThumbnailUrl)), N'') IS NULL OR
    NULLIF(LTRIM(RTRIM(m.BackdropUrl)), N'') IS NULL);

DECLARE @AdditionalGenres TABLE
(
    TmdbId int NOT NULL,
    Name nvarchar(100) NOT NULL,
    PRIMARY KEY (TmdbId, Name)
);
INSERT INTO @AdditionalGenres VALUES
(157336, N'Sci-Fi'),
(157336, N'Adventure'),
(157336, N'Drama'),
(286217, N'Sci-Fi'),
(286217, N'Adventure'),
(286217, N'Drama'),
(354912, N'Animation'),
(354912, N'Family'),
(354912, N'Adventure'),
(354912, N'Music'),
(150540, N'Animation'),
(150540, N'Family'),
(150540, N'Adventure'),
(150540, N'Drama'),
(150540, N'Comedy'),
(155, N'Action'),
(155, N'Crime'),
(155, N'Thriller'),
(361743, N'Action'),
(361743, N'Drama'),
(546554, N'Comedy'),
(546554, N'Crime'),
(546554, N'Mystery'),
(313369, N'Comedy'),
(313369, N'Drama'),
(313369, N'Romance'),
(313369, N'Music'),
(138843, N'Horror'),
(138843, N'Thriller'),
(120, N'Adventure'),
(120, N'Fantasy'),
(120, N'Action');

INSERT INTO @AdditionalGenres VALUES
(27205, N'Action'),
(27205, N'Sci-Fi'),
(27205, N'Adventure'),
(329865, N'Sci-Fi'),
(329865, N'Drama'),
(329865, N'Mystery'),
(269149, N'Animation'),
(269149, N'Family'),
(269149, N'Comedy'),
(269149, N'Adventure'),
(1184918, N'Animation'),
(1184918, N'Sci-Fi'),
(1184918, N'Family'),
(496243, N'Drama'),
(496243, N'Thriller'),
(496243, N'Comedy'),
(244786, N'Drama'),
(244786, N'Music'),
(76341, N'Action'),
(76341, N'Adventure'),
(76341, N'Sci-Fi'),
(120467, N'Comedy'),
(120467, N'Drama'),
(447332, N'Horror'),
(447332, N'Thriller'),
(447332, N'Sci-Fi'),
(372058, N'Animation'),
(372058, N'Romance'),
(372058, N'Fantasy'),
(372058, N'Drama');

INSERT INTO dbo.Genres (Name, CreatedAt)
SELECT s.Name, SYSUTCDATETIME()
FROM (SELECT DISTINCT Name FROM @AdditionalGenres) s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Genres g WHERE g.Name = s.Name);

INSERT INTO dbo.MovieGenres (MovieId, GenreId)
SELECT m.MovieId, g.Id
FROM @AdditionalGenres s
JOIN @AdditionalMovies m ON m.TmdbId = s.TmdbId
JOIN dbo.Genres g ON g.Name = s.Name
WHERE NOT EXISTS (SELECT 1 FROM dbo.MovieGenres mg WHERE mg.MovieId = m.MovieId AND mg.GenreId = g.Id);

DECLARE @AdditionalCredits TABLE
(
    TmdbId int NOT NULL,
    FullName nvarchar(200) NOT NULL,
    CreditType nvarchar(20) NOT NULL,
    CharacterName nvarchar(200) NULL,
    PRIMARY KEY (TmdbId, FullName, CreditType)
);
INSERT INTO @AdditionalCredits VALUES
(157336, N'Christopher Nolan', N'Director', NULL),
(157336, N'Matthew McConaughey', N'Actor', N'Cooper'),
(157336, N'Anne Hathaway', N'Actor', N'Brand'),
(286217, N'Ridley Scott', N'Director', NULL),
(286217, N'Matt Damon', N'Actor', N'Mark Watney'),
(286217, N'Jessica Chastain', N'Actor', N'Melissa Lewis'),
(354912, N'Lee Unkrich', N'Director', NULL),
(354912, N'Anthony Gonzalez', N'Actor', N'Miguel Rivera (voice)'),
(354912, N'Gael García Bernal', N'Actor', N'Héctor (voice)'),
(150540, N'Pete Docter', N'Director', NULL),
(150540, N'Amy Poehler', N'Actor', N'Joy (voice)'),
(150540, N'Phyllis Smith', N'Actor', N'Sadness (voice)'),
(155, N'Christopher Nolan', N'Director', NULL),
(155, N'Christian Bale', N'Actor', N'Bruce Wayne / Batman'),
(155, N'Heath Ledger', N'Actor', N'Joker'),
(361743, N'Joseph Kosinski', N'Director', NULL),
(361743, N'Tom Cruise', N'Actor', N'Pete ''Maverick'' Mitchell'),
(361743, N'Miles Teller', N'Actor', N'Bradley ''Rooster'' Bradshaw'),
(546554, N'Rian Johnson', N'Director', NULL),
(546554, N'Daniel Craig', N'Actor', N'Benoit Blanc'),
(546554, N'Ana de Armas', N'Actor', N'Marta Cabrera'),
(313369, N'Damien Chazelle', N'Director', NULL),
(313369, N'Ryan Gosling', N'Actor', N'Sebastian'),
(313369, N'Emma Stone', N'Actor', N'Mia'),
(138843, N'James Wan', N'Director', NULL),
(138843, N'Patrick Wilson', N'Actor', N'Ed Warren'),
(138843, N'Vera Farmiga', N'Actor', N'Lorraine Warren'),
(120, N'Peter Jackson', N'Director', NULL),
(120, N'Elijah Wood', N'Actor', N'Frodo'),
(120, N'Ian McKellen', N'Actor', N'Gandalf');

INSERT INTO @AdditionalCredits VALUES
(27205, N'Christopher Nolan', N'Director', NULL),
(27205, N'Leonardo DiCaprio', N'Actor', N'Dom Cobb'),
(27205, N'Joseph Gordon-Levitt', N'Actor', N'Arthur'),
(329865, N'Denis Villeneuve', N'Director', NULL),
(329865, N'Amy Adams', N'Actor', N'Louise Banks'),
(329865, N'Jeremy Renner', N'Actor', N'Ian Donnelly'),
(269149, N'Byron Howard', N'Director', NULL),
(269149, N'Rich Moore', N'Director', NULL),
(269149, N'Ginnifer Goodwin', N'Actor', N'Judy Hopps (voice)'),
(269149, N'Jason Bateman', N'Actor', N'Nick Wilde (voice)'),
(1184918, N'Chris Sanders', N'Director', NULL),
(1184918, N'Lupita Nyong''o', N'Actor', N'Roz (voice)'),
(1184918, N'Pedro Pascal', N'Actor', N'Fink (voice)'),
(496243, N'Bong Joon-ho', N'Director', NULL),
(496243, N'Song Kang-ho', N'Actor', N'Kim Ki-taek'),
(496243, N'Choi Woo-shik', N'Actor', N'Kim Ki-woo'),
(244786, N'Damien Chazelle', N'Director', NULL),
(244786, N'Miles Teller', N'Actor', N'Andrew Neiman'),
(244786, N'J.K. Simmons', N'Actor', N'Terence Fletcher'),
(76341, N'George Miller', N'Director', NULL),
(76341, N'Tom Hardy', N'Actor', N'Max Rockatansky'),
(76341, N'Charlize Theron', N'Actor', N'Imperator Furiosa'),
(120467, N'Wes Anderson', N'Director', NULL),
(120467, N'Ralph Fiennes', N'Actor', N'M. Gustave'),
(120467, N'Tony Revolori', N'Actor', N'Zero'),
(447332, N'John Krasinski', N'Director', NULL),
(447332, N'Emily Blunt', N'Actor', N'Evelyn Abbott'),
(447332, N'John Krasinski', N'Actor', N'Lee Abbott'),
(372058, N'Makoto Shinkai', N'Director', NULL),
(372058, N'Ryunosuke Kamiki', N'Actor', N'Taki Tachibana (voice)'),
(372058, N'Mone Kamishiraishi', N'Actor', N'Mitsuha Miyamizu (voice)');

IF EXISTS (
    SELECT p.FullName FROM dbo.People p
    WHERE EXISTS (SELECT 1 FROM @AdditionalCredits s WHERE s.FullName = p.FullName)
    GROUP BY p.FullName HAVING COUNT(*) > 1
)
    THROW 51007, 'Ambiguous additional credit person. Resolve duplicate names before seeding.', 1;

INSERT INTO dbo.People (FullName, PhotoUrl, BirthDate, CreatedAt, UpdatedAt)
SELECT s.FullName, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM (SELECT DISTINCT FullName FROM @AdditionalCredits) s
WHERE NOT EXISTS (SELECT 1 FROM dbo.People p WHERE p.FullName = s.FullName);

INSERT INTO dbo.MovieCredits (MovieId, PersonId, CreditType, CharacterName, CreatedAt)
SELECT m.MovieId, p.Id, s.CreditType, s.CharacterName, SYSUTCDATETIME()
FROM @AdditionalCredits s
JOIN @AdditionalMovies m ON m.TmdbId = s.TmdbId
JOIN dbo.People p ON p.FullName = s.FullName
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.MovieCredits c
    WHERE c.MovieId = m.MovieId AND c.PersonId = p.Id AND c.CreditType = s.CreditType
      AND (c.CharacterName = s.CharacterName OR (c.CharacterName IS NULL AND s.CharacterName IS NULL))
);


COMMIT TRANSACTION;
SELECT m.Id, m.Title, m.IsDeleted, m.ThumbnailUrl, m.BackdropUrl,
    (SELECT COUNT(*) FROM dbo.MovieGenres mg WHERE mg.MovieId = m.Id) AS GenreCount,
    (SELECT COUNT(*) FROM dbo.MovieCredits mc WHERE mc.MovieId = m.Id) AS CreditCount
FROM dbo.Movies m
WHERE m.Id IN (@DuneId, @SpiderVerseId, @OppenheimerId, @EncantoId, @Guardians3Id)
   OR m.Id IN (SELECT MovieId FROM @AdditionalMovies)
ORDER BY m.Id;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
