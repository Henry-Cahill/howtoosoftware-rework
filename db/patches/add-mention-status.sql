-- ============================================================================
-- Migration: AddMentionApprovalStatus
-- Adds status column to mentions table for approval workflow (MENT.2).
-- New mentions start as "pending"; admin reviews and approves/rejects.
-- Existing rows are backfilled to "approved" so they keep rendering.
-- All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

-- Add status column with default 'approved' (backfills existing rows).
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('mentions') AND name = 'status')
ALTER TABLE [mentions] ADD [status] NVARCHAR(20) NOT NULL
    CONSTRAINT [DF_mentions_status] DEFAULT N'approved';
GO

-- Index for filtering by status in admin list / public queries.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('mentions') AND name = 'IX_mentions_status')
CREATE INDEX [IX_mentions_status] ON [mentions] ([status]);
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
