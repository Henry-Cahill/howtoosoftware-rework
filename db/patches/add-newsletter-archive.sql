-- ============================================================================
-- Migration: AddNewsletterArchiveToggle (NEWS.6)
-- Adds archive_enabled BIT column to newsletters table. When true, the public
-- site exposes /newsletter/{slug}/archive/ listing all posts published through
-- that newsletter. Existing newsletters default to disabled.
-- Idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('newsletters') AND name = 'archive_enabled')
ALTER TABLE [newsletters] ADD [archive_enabled] BIT NOT NULL
    CONSTRAINT [DF_newsletters_archive_enabled] DEFAULT 0;
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
