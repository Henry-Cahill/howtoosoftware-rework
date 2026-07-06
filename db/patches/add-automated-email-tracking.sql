-- ============================================================================
-- Migration: AddAutomatedEmailDeliveryTracking
-- Adds delivered/opened/clicked/failed/bounced tracking columns to
-- automated_email_recipients for AUTO.3 (per-send delivery statistics).
-- Adds an index on member_email so the Mailgun webhook can resolve the
-- most-recent recipient row for a given address in O(log N) instead of a
-- table scan. All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('automated_email_recipients') AND name = 'delivered_at')
ALTER TABLE [automated_email_recipients] ADD [delivered_at] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('automated_email_recipients') AND name = 'opened_at')
ALTER TABLE [automated_email_recipients] ADD [opened_at] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('automated_email_recipients') AND name = 'clicked_at')
ALTER TABLE [automated_email_recipients] ADD [clicked_at] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('automated_email_recipients') AND name = 'failed_at')
ALTER TABLE [automated_email_recipients] ADD [failed_at] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('automated_email_recipients') AND name = 'bounced_at')
ALTER TABLE [automated_email_recipients] ADD [bounced_at] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('automated_email_recipients') AND name = 'failure_reason')
ALTER TABLE [automated_email_recipients] ADD [failure_reason] NVARCHAR(2000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('automated_email_recipients')
      AND name = 'ix_automated_email_recipients_member_email')
CREATE INDEX [ix_automated_email_recipients_member_email]
    ON [automated_email_recipients] ([member_email]);
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
