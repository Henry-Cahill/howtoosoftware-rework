-- ============================================================================
-- Migration: AddRedirectRegexSupport
-- Adds is_regex column to redirects table for regex pattern matching (REDIR.3).
-- All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

-- Add is_regex column with default 0 (BIT, not nullable)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('redirects') AND name = 'is_regex')
ALTER TABLE [redirects] ADD [is_regex] BIT NOT NULL CONSTRAINT [DF_redirects_is_regex] DEFAULT 0;
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
