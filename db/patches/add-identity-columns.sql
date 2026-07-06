-- =============================================================================
-- Add ASP.NET Identity columns to users/roles tables and create Identity tables
-- Run against: Website_HowTooSoftwareDb on <sql-host>,1433
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Users table: add Identity columns ──

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'user_name')
    ALTER TABLE [users] ADD [user_name] NVARCHAR(191) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'normalized_user_name')
    ALTER TABLE [users] ADD [normalized_user_name] NVARCHAR(191) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'normalized_email')
    ALTER TABLE [users] ADD [normalized_email] NVARCHAR(191) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'email_confirmed')
    ALTER TABLE [users] ADD [email_confirmed] BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'password_hash')
    ALTER TABLE [users] ADD [password_hash] NVARCHAR(256) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'security_stamp')
    ALTER TABLE [users] ADD [security_stamp] NVARCHAR(256) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'concurrency_stamp')
    ALTER TABLE [users] ADD [concurrency_stamp] NVARCHAR(36) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'phone_number')
    ALTER TABLE [users] ADD [phone_number] NVARCHAR(MAX) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'phone_number_confirmed')
    ALTER TABLE [users] ADD [phone_number_confirmed] BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'two_factor_enabled')
    ALTER TABLE [users] ADD [two_factor_enabled] BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'lockout_end')
    ALTER TABLE [users] ADD [lockout_end] DATETIMEOFFSET NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'lockout_enabled')
    ALTER TABLE [users] ADD [lockout_enabled] BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'access_failed_count')
    ALTER TABLE [users] ADD [access_failed_count] INT NOT NULL DEFAULT 0;
GO

-- Populate user_name and normalized columns from existing data
UPDATE [users]
SET [user_name] = [email],
    [normalized_user_name] = UPPER([email]),
    [normalized_email] = UPPER([email]),
    [email_confirmed] = 1
WHERE [user_name] IS NULL;
GO

-- ── Roles table: add Identity columns ──

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('roles') AND name = 'normalized_name')
    ALTER TABLE [roles] ADD [normalized_name] NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('roles') AND name = 'concurrency_stamp')
    ALTER TABLE [roles] ADD [concurrency_stamp] NVARCHAR(36) NULL;
GO

-- Populate normalized_name from existing data
UPDATE [roles]
SET [normalized_name] = UPPER([name])
WHERE [normalized_name] IS NULL;
GO

-- ── Create Identity support tables ──

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'user_claims')
CREATE TABLE [user_claims] (
    [id] INT NOT NULL IDENTITY(1,1),
    [user_id] NVARCHAR(24) NOT NULL,
    [claim_type] NVARCHAR(256) NULL,
    [claim_value] NVARCHAR(1024) NULL,
    CONSTRAINT [pk_user_claims] PRIMARY KEY ([id]),
    CONSTRAINT [fk_user_claims_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'user_logins')
CREATE TABLE [user_logins] (
    [login_provider] NVARCHAR(128) NOT NULL,
    [provider_key] NVARCHAR(128) NOT NULL,
    [provider_display_name] NVARCHAR(256) NULL,
    [user_id] NVARCHAR(24) NOT NULL,
    CONSTRAINT [pk_user_logins] PRIMARY KEY ([login_provider], [provider_key]),
    CONSTRAINT [fk_user_logins_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'user_tokens')
CREATE TABLE [user_tokens] (
    [user_id] NVARCHAR(24) NOT NULL,
    [login_provider] NVARCHAR(128) NOT NULL,
    [name] NVARCHAR(128) NOT NULL,
    [value] NVARCHAR(MAX) NULL,
    CONSTRAINT [pk_user_tokens] PRIMARY KEY ([user_id], [login_provider], [name]),
    CONSTRAINT [fk_user_tokens_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'role_claims')
CREATE TABLE [role_claims] (
    [id] INT NOT NULL IDENTITY(1,1),
    [role_id] NVARCHAR(24) NOT NULL,
    [claim_type] NVARCHAR(256) NULL,
    [claim_value] NVARCHAR(1024) NULL,
    CONSTRAINT [pk_role_claims] PRIMARY KEY ([id]),
    CONSTRAINT [fk_role_claims_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([id]) ON DELETE CASCADE
);
GO

-- ── Create indexes for Identity columns ──

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_users_normalized_email' AND object_id = OBJECT_ID('users'))
    CREATE INDEX [ix_users_normalized_email] ON [users] ([normalized_email]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_users_normalized_user_name' AND object_id = OBJECT_ID('users'))
    CREATE UNIQUE INDEX [ix_users_normalized_user_name] ON [users] ([normalized_user_name]) WHERE [normalized_user_name] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_roles_normalized_name' AND object_id = OBJECT_ID('roles'))
    CREATE UNIQUE INDEX [ix_roles_normalized_name] ON [roles] ([normalized_name]) WHERE [normalized_name] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_user_claims_user_id' AND object_id = OBJECT_ID('user_claims'))
    CREATE INDEX [ix_user_claims_user_id] ON [user_claims] ([user_id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_user_logins_user_id' AND object_id = OBJECT_ID('user_logins'))
    CREATE INDEX [ix_user_logins_user_id] ON [user_logins] ([user_id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_role_claims_role_id' AND object_id = OBJECT_ID('role_claims'))
    CREATE INDEX [ix_role_claims_role_id] ON [role_claims] ([role_id]);
GO

PRINT N'Identity columns and tables added successfully.';
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
