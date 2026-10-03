-- Apply to an existing database before running the ViewingSessions feature.
-- No generated EF files need editing: the new columns use custom shadow mapping.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.ViewingSessions', 'ClientSessionId') IS NULL
BEGIN
    ALTER TABLE dbo.ViewingSessions ADD
        ClientSessionId uniqueidentifier NULL,
        CheckpointSequence bigint NOT NULL CONSTRAINT DF_ViewingSessions_CheckpointSequence DEFAULT (0),
        WatchedMilliseconds bigint NOT NULL CONSTRAINT DF_ViewingSessions_WatchedMilliseconds DEFAULT (0),
        LastCheckpointAtUtc datetime2 NOT NULL CONSTRAINT DF_ViewingSessions_LastCheckpointAtUtc DEFAULT (SYSUTCDATETIME());
    -- Dynamic SQL compiles after the new columns exist.
    EXEC(N'UPDATE dbo.ViewingSessions SET WatchedMilliseconds = CONVERT(bigint, WatchedSeconds) * 1000,
        LastCheckpointAtUtc = COALESCE(EndedAt, StartedAt);
        CREATE UNIQUE INDEX UX_ViewingSessions_Device_ClientSession ON dbo.ViewingSessions(DeviceId, ClientSessionId)
        WHERE ClientSessionId IS NOT NULL;
        ALTER TABLE dbo.ViewingSessions ADD CONSTRAINT CK_ViewingSessions_Checkpoint
        CHECK (CheckpointSequence >= 0 AND WatchedMilliseconds >= 0 AND WatchedSeconds = WatchedMilliseconds / 1000);');
END;
COMMIT;
