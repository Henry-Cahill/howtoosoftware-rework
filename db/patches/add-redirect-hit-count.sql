-- ============================================================================
-- Migration: AddRedirectHitCount
-- Adds hit_count column to redirects table for redirect analytics (REDIR.2).
-- All statements are idempotent (safe to re-run).
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- ============================================================================

USE [Website_HowTooSoftwareDb];
GO

-- Add hit_count column with default 0 (BIGINT to support high-traffic redirects)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('redirects') AND name = 'hit_count')
ALTER TABLE [redirects] ADD [hit_count] BIGINT NOT NULL CONSTRAINT [DF_redirects_hit_count] DEFAULT 0;
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
