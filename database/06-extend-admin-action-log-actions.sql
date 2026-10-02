USE [NetflixCloneDb]
GO
-- Existing columns and EF entities stay unchanged. Run once before role-management requests.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.AdminActionLogs', N'U') IS NULL
        THROW 50001, 'AdminActionLogs must exist.', 1;
    ALTER TABLE [dbo].[AdminActionLogs] DROP CONSTRAINT [CK_AdminActionLogs_Action];
    ALTER TABLE [dbo].[AdminActionLogs] WITH CHECK ADD CONSTRAINT [CK_AdminActionLogs_Action]
        CHECK ([Action] IN ('AccountLocked', 'AccountUnlocked', 'RoleAssigned', 'RoleRemoved'));
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
