/* Development media fixtures. Run AFTER 03-seed-catalog-dev.sql.
   Trailer source: docs/link-youtube-trailer.txt (user-selected links).
   Canonical watch URLs retain the video ID, without share/tracking parameters.
   Playback/embedding availability must be checked in a browser.
   ALL 25 movies use the SAME Blender sample, NOT their actual movie footage.
   The player must label this as demo content and use the video's actual duration;
   Movies.DurationSeconds remains the catalog movie runtime.
   VideoUrl is relative to the WEB origin, not the API origin.
   Required local file: Web/wwwroot/videos/bbb_sunflower_1080p_60fps_normal.mp4.
   This script does not download files or establish media licensing/attribution.
   Static wwwroot video is public, not protected subscription content.
   Sequential runs only. Preserve existing nonblank URLs and soft-delete state.
*/
USE [NetflixCloneDb];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT <> 0 THROW 51100, 'Run this seed outside an existing transaction.', 1;

DECLARE @DemoVideoUrl nvarchar(500) = N'/videos/bbb_sunflower_1080p_60fps_normal.mp4';
DECLARE @Media TABLE
(
    Title nvarchar(255) NOT NULL,
    ReleaseDate date NOT NULL,
    YoutubeVideoId varchar(11) NOT NULL,
    PRIMARY KEY (Title, ReleaseDate)
);
INSERT INTO @Media VALUES
(N'Dune: Part Two', '2024-03-01', 'Way9Dexny3w'),
(N'Spider-Man: Across the Spider-Verse', '2023-06-02', 'cqGjhVJWtEg'),
(N'Oppenheimer', '2023-07-21', 'bK6ldnjE3Y0'),
(N'Encanto', '2021-11-24', 'CaimKeDcudo'),
(N'Guardians of the Galaxy Vol. 3', '2023-05-05', 'u3V5KDHRQvk'),
(N'The Lord of the Rings: The Fellowship of the Ring', '2001-12-19', '_nZdmwHrcnw'),
(N'The Dark Knight', '2008-07-18', 'g8evyE9TuYk'),
(N'The Conjuring', '2013-07-19', 'ejMMn0t58Lc'),
(N'Inside Out', '2015-06-19', 'yRUAzGQ3nSY'),
(N'Interstellar', '2014-11-07', 'zSWdZVtXT7E'),
(N'The Martian', '2015-10-02', 'ej3ioOneTy8'),
(N'La La Land', '2016-12-09', '0pdqf4P9MB8'),
(N'Coco', '2017-11-22', 'Rvr68u6k5sI'),
(N'Top Gun: Maverick', '2022-05-27', 'qSqVVswa420'),
(N'Knives Out', '2019-11-27', 'qGqiHJTsRkQ'),
(N'Inception', '2010-07-16', '8hP9D6kZseM'),
(N'Mad Max: Fury Road', '2015-05-15', 'hEJnMQG9ev8'),
(N'The Grand Budapest Hotel', '2014-03-07', '1Fg5iWmQjwk'),
(N'Whiplash', '2014-10-10', 'Q7kZy3T6vRM'),
(N'Zootopia', '2016-03-04', 'Y0c3nKWhlIA'),
(N'Arrival', '2016-11-11', 'tFMo3UJ4B4g'),
(N'Your Name.', '2017-04-07', 'RuyHIkXdYf8'),
(N'A Quiet Place', '2018-04-06', 'WR7cc5t7tv8'),
(N'Parasite', '2019-10-11', 'SEUXfv87Wpk'),
(N'The Wild Robot', '2024-09-27', '67vbA5ZJdKQ');

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM @Media s
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Movies m WHERE m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate)
    )
        THROW 51101, 'A catalog movie is missing. Run 03-seed-catalog-dev.sql first.', 1;

    IF EXISTS (
        SELECT s.Title, s.ReleaseDate FROM @Media s
        JOIN dbo.Movies m ON m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate
        GROUP BY s.Title, s.ReleaseDate HAVING COUNT(*) > 1
    )
        THROW 51102, 'Ambiguous movie title/date. Resolve duplicates before seeding media.', 1;

    UPDATE m SET
        TrailerUrl = CASE WHEN NULLIF(LTRIM(RTRIM(m.TrailerUrl)), N'') IS NULL
            THEN N'https://www.youtube.com/watch?v=' + s.YoutubeVideoId ELSE m.TrailerUrl END,
        VideoUrl = CASE WHEN NULLIF(LTRIM(RTRIM(m.VideoUrl)), N'') IS NULL
            THEN @DemoVideoUrl ELSE m.VideoUrl END,
        UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Movies m
    JOIN @Media s ON m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate
    WHERE m.IsDeleted = 0 AND (
        NULLIF(LTRIM(RTRIM(m.TrailerUrl)), N'') IS NULL OR
        NULLIF(LTRIM(RTRIM(m.VideoUrl)), N'') IS NULL);

    COMMIT TRANSACTION;

    SELECT m.Id, m.Title, m.IsDeleted, m.TrailerUrl, m.VideoUrl
    FROM dbo.Movies m
    JOIN @Media s ON m.Title = s.Title AND m.ReleaseDate = s.ReleaseDate
    ORDER BY m.Id;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
