-- ============================================================================
-- Migration: AddMemberNotes
-- Adds member_notes table — append-only thread of admin notes per member
-- (MEM.7). Each row captures author + timestamp so the admin UI can render a
-- conversation-style history instead of a single overwrite field.
-- Backfills one row per Member that currently has a non-empty Note value so
-- no historical context is lost when the UI switches to the thread view.
-- All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'member_notes')
BEGIN
    CREATE TABLE [member_notes] (
        [id]          NVARCHAR(24)   NOT NULL,
        [member_id]   NVARCHAR(24)   NOT NULL,
        [author_id]   NVARCHAR(24)   NULL,
        [author_name] NVARCHAR(191)  NULL,
        [body]        NVARCHAR(MAX)  NOT NULL,
        [created_at]  DATETIME2      NOT NULL CONSTRAINT [DF_member_notes_created_at] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [pk_member_notes] PRIMARY KEY ([id])
    );
END;
GO

-- Index for thread loading: latest-per-member queries
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_member_notes_member_id_created_at'
      AND object_id = OBJECT_ID('member_notes'))
CREATE INDEX [ix_member_notes_member_id_created_at]
    ON [member_notes] ([member_id], [created_at]);
GO

-- FK to members (cascade delete so notes go away with their member)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'fk_member_notes_members_member_id')
ALTER TABLE [member_notes]
    ADD CONSTRAINT [fk_member_notes_members_member_id]
    FOREIGN KEY ([member_id]) REFERENCES [members] ([id])
    ON DELETE CASCADE;
GO

-- FK to users (set null so notes survive admin user deletions)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'fk_member_notes_users_author_id')
ALTER TABLE [member_notes]
    ADD CONSTRAINT [fk_member_notes_users_author_id]
    FOREIGN KEY ([author_id]) REFERENCES [users] ([id])
    ON DELETE SET NULL;
GO

-- Backfill: one append-only entry per Member with an existing single-field
-- note. Skip if any notes already exist for the member (idempotent re-run).
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'members')
BEGIN
    INSERT INTO [member_notes] ([id], [member_id], [author_id], [author_name], [body], [created_at])
    SELECT
        LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', ''), 24) AS [id],
        m.[id],
        NULL AS [author_id],
        N'(legacy)' AS [author_name],
        m.[note],
        COALESCE(m.[updated_at], m.[created_at], SYSUTCDATETIME())
    FROM [members] m
    WHERE m.[note] IS NOT NULL
      AND LTRIM(RTRIM(m.[note])) <> ''
      AND NOT EXISTS (
          SELECT 1 FROM [member_notes] n WHERE n.[member_id] = m.[id]
      );
END;
GO

PRINT 'AddMemberNotes migration applied successfully.';
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
