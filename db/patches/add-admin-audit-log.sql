-- ============================================================================
-- Migration: AddAdminAuditLog
-- Adds admin_audit_logs table — append-only record of security-sensitive
-- admin actions (member impersonation, etc.) for MEM.6.
-- All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'admin_audit_logs')
BEGIN
    CREATE TABLE [admin_audit_logs] (
        [id]               NVARCHAR(36)  NOT NULL,
        [admin_user_id]    NVARCHAR(24)  NOT NULL,
        [admin_user_email] NVARCHAR(191) NULL,
        [action]           NVARCHAR(64)  NOT NULL,
        [target_type]      NVARCHAR(50)  NULL,
        [target_id]        NVARCHAR(36)  NULL,
        [metadata]         NVARCHAR(2000) NULL,
        [ip_address]       NVARCHAR(64)  NULL,
        [user_agent]       NVARCHAR(512) NULL,
        [created_at]       DATETIME2     NOT NULL CONSTRAINT [DF_admin_audit_logs_created_at] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [pk_admin_audit_logs] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_admin_audit_logs_admin_user_id'
      AND object_id = OBJECT_ID('admin_audit_logs'))
CREATE INDEX [ix_admin_audit_logs_admin_user_id]
    ON [admin_audit_logs] ([admin_user_id]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_admin_audit_logs_action'
      AND object_id = OBJECT_ID('admin_audit_logs'))
CREATE INDEX [ix_admin_audit_logs_action]
    ON [admin_audit_logs] ([action]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_admin_audit_logs_target'
      AND object_id = OBJECT_ID('admin_audit_logs'))
CREATE INDEX [ix_admin_audit_logs_target]
    ON [admin_audit_logs] ([target_type], [target_id]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_admin_audit_logs_created_at'
      AND object_id = OBJECT_ID('admin_audit_logs'))
CREATE INDEX [ix_admin_audit_logs_created_at]
    ON [admin_audit_logs] ([created_at]);
GO

PRINT 'AddAdminAuditLog migration applied successfully.';
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
