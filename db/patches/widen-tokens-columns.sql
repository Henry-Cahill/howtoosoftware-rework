-- Widen tokens.id and tokens.token to fit the values produced by
-- MagicLinkService:
--   id    : Guid.NewGuid().ToString("D")        -> 36 chars
--   token : SHA-256 hex (Convert.ToHexStringLower) -> 64 chars
--
-- Ghost's original schema used nvarchar(24)/nvarchar(32) which is too
-- small for our GUID + SHA-256 hash convention, causing
-- "String or binary data would be truncated" (SqlException 2628).
--
-- Safe to re-run: each ALTER is idempotent via the type/length check.

SET NOCOUNT ON;
GO

-- 1) tokens.id : nvarchar(24) -> nvarchar(36)
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.tables  t ON t.object_id = c.object_id
    WHERE t.name = 'tokens' AND c.name = 'id'
      AND (c.max_length / 2) < 36   -- nvarchar stores 2 bytes per char
)
BEGIN
    PRINT 'Altering tokens.id -> nvarchar(36) NOT NULL';

    -- Drop the PK constraint so we can change the underlying type
    DECLARE @pk sysname = (
        SELECT name FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.tokens') AND type = 'PK'
    );
    IF @pk IS NOT NULL
        EXEC('ALTER TABLE [dbo].[tokens] DROP CONSTRAINT [' + @pk + ']');

    ALTER TABLE [dbo].[tokens] ALTER COLUMN [id] NVARCHAR(36) NOT NULL;

    ALTER TABLE [dbo].[tokens]
        ADD CONSTRAINT [pk_tokens] PRIMARY KEY ([id]);
END
GO

-- 2) tokens.token : nvarchar(32) -> nvarchar(64)
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.tables  t ON t.object_id = c.object_id
    WHERE t.name = 'tokens' AND c.name = 'token'
      AND (c.max_length / 2) < 64
)
BEGIN
    PRINT 'Altering tokens.token -> nvarchar(64) NOT NULL';

    -- Drop the index so the column can be altered
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_tokens_token' AND object_id = OBJECT_ID('dbo.tokens'))
        DROP INDEX [ix_tokens_token] ON [dbo].[tokens];

    ALTER TABLE [dbo].[tokens] ALTER COLUMN [token] NVARCHAR(64) NOT NULL;

    CREATE INDEX [ix_tokens_token] ON [dbo].[tokens] ([token]);
END
GO

PRINT 'tokens column widening complete.';
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
