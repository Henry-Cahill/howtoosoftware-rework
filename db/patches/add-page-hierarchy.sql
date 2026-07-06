-- ============================================================================
-- Migration: AddPageHierarchy
-- Adds parent_id and sort_order columns to posts table for page hierarchy
-- All statements are idempotent (safe to re-run)
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

-- 1. Add parent_id column (nullable FK to self)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('posts') AND name = 'parent_id')
ALTER TABLE [posts] ADD [parent_id] NVARCHAR(24) NULL;
GO

-- 2. Add sort_order column with default 0
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('posts') AND name = 'sort_order')
ALTER TABLE [posts] ADD [sort_order] INT NOT NULL DEFAULT 0;
GO

-- 3. Self-referencing FK: posts.parent_id → posts.id (NO ACTION on delete)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'fk_posts_posts_parent_id')
ALTER TABLE [posts]
    ADD CONSTRAINT [fk_posts_posts_parent_id]
    FOREIGN KEY ([parent_id]) REFERENCES [posts] ([id])
    ON DELETE NO ACTION;
GO

-- 4. Index on parent_id for FK lookups
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_posts_parent_id'
      AND object_id = OBJECT_ID('posts'))
CREATE INDEX [ix_posts_parent_id]
    ON [posts] ([parent_id]);
GO

-- 5. Composite index for page tree queries
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_posts_type_parent_id_sort_order'
      AND object_id = OBJECT_ID('posts'))
CREATE INDEX [ix_posts_type_parent_id_sort_order]
    ON [posts] ([type], [parent_id], [sort_order]);
GO

-- 6. Record migration in EF history table (delete stale row first if re-running)
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = '20260418210000_AddPageHierarchy';
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES ('20260418210000_AddPageHierarchy', '10.0.0-preview.2.25163.8');
GO

PRINT 'AddPageHierarchy migration applied successfully.';
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
