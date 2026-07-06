-- A/B subject-line testing for newsletter sends (NEWS.5)
-- Idempotent: re-running is safe. Adds columns + index for variant assignment and winner scheduling.
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

-- ============================================================
-- emails: A/B testing fields
-- ============================================================
IF COL_LENGTH('dbo.emails', 'subject_b') IS NULL
    ALTER TABLE dbo.emails ADD subject_b NVARCHAR(300) NULL;
GO
IF COL_LENGTH('dbo.emails', 'ab_test_split_percent') IS NULL
    ALTER TABLE dbo.emails ADD ab_test_split_percent INT NOT NULL CONSTRAINT df_emails_ab_test_split_percent DEFAULT(0);
GO
IF COL_LENGTH('dbo.emails', 'ab_test_wait_minutes') IS NULL
    ALTER TABLE dbo.emails ADD ab_test_wait_minutes INT NOT NULL CONSTRAINT df_emails_ab_test_wait_minutes DEFAULT(0);
GO
IF COL_LENGTH('dbo.emails', 'ab_test_phase') IS NULL
    ALTER TABLE dbo.emails ADD ab_test_phase NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.emails', 'ab_test_started_at') IS NULL
    ALTER TABLE dbo.emails ADD ab_test_started_at DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.emails', 'ab_test_winner_variant') IS NULL
    ALTER TABLE dbo.emails ADD ab_test_winner_variant NVARCHAR(1) NULL;
GO
IF COL_LENGTH('dbo.emails', 'ab_test_opens_a') IS NULL
    ALTER TABLE dbo.emails ADD ab_test_opens_a INT NULL;
GO
IF COL_LENGTH('dbo.emails', 'ab_test_opens_b') IS NULL
    ALTER TABLE dbo.emails ADD ab_test_opens_b INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_emails_ab_test_phase_ab_test_started_at' AND object_id = OBJECT_ID('dbo.emails'))
    CREATE INDEX ix_emails_ab_test_phase_ab_test_started_at ON dbo.emails(ab_test_phase, ab_test_started_at);
GO

-- ============================================================
-- email_recipients: per-recipient variant assignment
-- ============================================================
IF COL_LENGTH('dbo.email_recipients', 'ab_variant') IS NULL
    ALTER TABLE dbo.email_recipients ADD ab_variant NVARCHAR(10) NULL;
GO

-- ============================================================
-- posts_meta: optional alternate subject line
-- ============================================================
IF COL_LENGTH('dbo.posts_meta', 'email_subject_b') IS NULL
    ALTER TABLE dbo.posts_meta ADD email_subject_b NVARCHAR(300) NULL;
GO

PRINT 'add-email-ab-test.sql: completed';
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
