-- ============================================================================
-- Migration: AddMemberAvatar
-- Adds avatar_image column to members table (MEM.8). Stores a relative URL
-- (e.g. /content/images/2026/03/foo.webp) to a member avatar uploaded by an
-- admin via the member detail page. Falls back to the existing initial-letter
-- placeholder when null.
-- All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'members' AND COLUMN_NAME = 'avatar_image')
BEGIN
    ALTER TABLE [members]
        ADD [avatar_image] NVARCHAR(2000) NULL;
END;
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
