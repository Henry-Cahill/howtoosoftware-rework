-- ============================================================================
-- Migration: AddMemberSegments
-- Adds member_segments table for saving named filter combinations (MEM.5).
-- Segments are shared across all admins and displayed as quick-access chips
-- above the Members admin table.
-- All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'member_segments')
BEGIN
    CREATE TABLE [member_segments] (
        [id]                NVARCHAR(24)  NOT NULL,
        [name]              NVARCHAR(191) NOT NULL,
        [status_filter]     NVARCHAR(20)  NULL,
        [label_id]          NVARCHAR(24)  NULL,
        [engagement_filter] NVARCHAR(40)  NULL,
        [search_query]      NVARCHAR(191) NULL,
        [sort_order]        INT           NOT NULL CONSTRAINT [DF_member_segments_sort_order] DEFAULT 0,
        [created_at]        DATETIME2     NOT NULL CONSTRAINT [DF_member_segments_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at]        DATETIME2     NULL,
        CONSTRAINT [pk_member_segments] PRIMARY KEY ([id])
    );
END;
GO

-- Unique index on name
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_member_segments_name'
      AND object_id = OBJECT_ID('member_segments'))
CREATE UNIQUE INDEX [ix_member_segments_name]
    ON [member_segments] ([name]);
GO

-- Index for ordering chips
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_member_segments_sort_order'
      AND object_id = OBJECT_ID('member_segments'))
CREATE INDEX [ix_member_segments_sort_order]
    ON [member_segments] ([sort_order]);
GO

-- FK to labels (SET NULL on delete so segments survive label deletions)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'fk_member_segments_labels_label_id')
ALTER TABLE [member_segments]
    ADD CONSTRAINT [fk_member_segments_labels_label_id]
    FOREIGN KEY ([label_id]) REFERENCES [labels] ([id])
    ON DELETE SET NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_member_segments_label_id'
      AND object_id = OBJECT_ID('member_segments'))
CREATE INDEX [ix_member_segments_label_id]
    ON [member_segments] ([label_id]);
GO

PRINT 'AddMemberSegments migration applied successfully.';
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
