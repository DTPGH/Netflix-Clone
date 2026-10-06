-- Apply to the existing NetflixClone database, then intentionally re-scaffold EF.
-- Existing profiles retain their optional PinHash and start with no failed attempts.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Profiles', 'PinFailedAttempts') IS NULL
BEGIN
    ALTER TABLE dbo.Profiles ADD PinFailedAttempts int NOT NULL
        CONSTRAINT DF_Profiles_PinFailedAttempts DEFAULT (0) WITH VALUES;
END;

IF COL_LENGTH('dbo.Profiles', 'PinLockoutEnd') IS NULL
BEGIN
    ALTER TABLE dbo.Profiles ADD PinLockoutEnd datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.Profiles')
      AND name = 'CK_Profiles_PinFailedAttempts'
)
BEGIN
    -- Compile only after the new column exists, including on the first run.
    EXEC(N'ALTER TABLE dbo.Profiles WITH CHECK ADD CONSTRAINT
        CK_Profiles_PinFailedAttempts CHECK (PinFailedAttempts >= 0);');
END;

COMMIT TRANSACTION;
