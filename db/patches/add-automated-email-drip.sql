-- ============================================================================
-- Migration: AddAutomatedEmailDripSequence
-- Adds delay/drip-sequence support for automated emails (AUTO.4):
--   * automated_emails.delay_minutes      INT NOT NULL DEFAULT 0
--   * automated_emails.trigger_event      NVARCHAR(100) NULL (+ index)
--   * automated_email_schedules           queue table for delayed sends
-- All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

-- ── New columns on automated_emails ──────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('automated_emails') AND name = 'delay_minutes')
ALTER TABLE [automated_emails]
    ADD [delay_minutes] INT NOT NULL CONSTRAINT [DF_automated_emails_delay_minutes] DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('automated_emails') AND name = 'trigger_event')
ALTER TABLE [automated_emails] ADD [trigger_event] NVARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('automated_emails')
      AND name = 'ix_automated_emails_trigger_event')
CREATE INDEX [ix_automated_emails_trigger_event]
    ON [automated_emails] ([trigger_event]);
GO

-- ── Schedule queue table ─────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'automated_email_schedules')
CREATE TABLE [automated_email_schedules] (
    [id]                  NVARCHAR(24)  NOT NULL,
    [automated_email_id]  NVARCHAR(24)  NOT NULL,
    [member_id]           NVARCHAR(24)  NOT NULL,
    [member_uuid]         NVARCHAR(36)  NOT NULL,
    [member_email]        NVARCHAR(191) NOT NULL,
    [member_name]         NVARCHAR(191) NULL,
    [site_url]            NVARCHAR(500) NOT NULL,
    [scheduled_for]       DATETIME2     NOT NULL,
    [created_at]          DATETIME2     NOT NULL CONSTRAINT [DF_automated_email_schedules_created_at] DEFAULT SYSUTCDATETIME(),
    [processed_at]        DATETIME2     NULL,
    [failure_reason]      NVARCHAR(2000) NULL,
    CONSTRAINT [PK_automated_email_schedules] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [FK_automated_email_schedules_automated_emails]
        FOREIGN KEY ([automated_email_id])
        REFERENCES [automated_emails] ([id])
        ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('automated_email_schedules')
      AND name = 'ix_automated_email_schedules_automated_email_id')
CREATE INDEX [ix_automated_email_schedules_automated_email_id]
    ON [automated_email_schedules] ([automated_email_id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('automated_email_schedules')
      AND name = 'ix_automated_email_schedules_member_id')
CREATE INDEX [ix_automated_email_schedules_member_id]
    ON [automated_email_schedules] ([member_id]);
GO

-- Composite pending-work index: dispatcher scans WHERE processed_at IS NULL
-- ORDER BY scheduled_for. Indexing both columns lets SQL Server seek directly
-- to the next due batch.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('automated_email_schedules')
      AND name = 'ix_automated_email_schedules_pending')
CREATE INDEX [ix_automated_email_schedules_pending]
    ON [automated_email_schedules] ([processed_at], [scheduled_for]);
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
