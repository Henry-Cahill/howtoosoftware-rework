-- TIER.1 ─ Add SortOrder column to products (tiers) for display ordering
-- on the public pricing page and the admin Tiers grid.
-- Idempotent: safe to re-run.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.products')
      AND name = N'sort_order'
)
BEGIN
    ALTER TABLE dbo.products
        ADD sort_order INT NOT NULL
            CONSTRAINT DF_products_sort_order DEFAULT (0);
END
GO

-- Backfill an explicit order for existing rows so drag-drop starts from
-- a deterministic baseline (oldest tier first). Only runs when every row
-- still has the default sort_order = 0 (i.e. column was just added).
IF NOT EXISTS (
    SELECT 1 FROM dbo.products WHERE sort_order <> 0
)
BEGIN
    ;WITH numbered AS (
        SELECT id,
               ROW_NUMBER() OVER (ORDER BY created_at, id) - 1 AS new_order
        FROM dbo.products
    )
    UPDATE p
        SET p.sort_order = n.new_order
    FROM dbo.products p
    INNER JOIN numbered n ON n.id = p.id;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_products_sort_order'
      AND object_id = OBJECT_ID(N'dbo.products')
)
BEGIN
    CREATE INDEX IX_products_sort_order
        ON dbo.products (sort_order);
END
GO

-- =============================================================
-- © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-- Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
-- =============================================================
