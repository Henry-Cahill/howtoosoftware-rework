using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPageHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add parent_id column (nullable FK to self)
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('posts') AND name = 'parent_id')
                ALTER TABLE [posts] ADD [parent_id] NVARCHAR(24) NULL;
                """);

            // Add sort_order column with default 0
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('posts') AND name = 'sort_order')
                ALTER TABLE [posts] ADD [sort_order] INT NOT NULL DEFAULT 0;
                """);

            // Add self-referencing FK: posts.parent_id → posts.id (NO ACTION on delete)
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'fk_posts_posts_parent_id')
                ALTER TABLE [posts]
                    ADD CONSTRAINT [fk_posts_posts_parent_id]
                    FOREIGN KEY ([parent_id]) REFERENCES [posts] ([id])
                    ON DELETE NO ACTION;
                """);

            // Index on parent_id for FK lookups
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'ix_posts_parent_id'
                      AND object_id = OBJECT_ID('posts'))
                CREATE INDEX [ix_posts_parent_id]
                    ON [posts] ([parent_id]);
                """);

            // Composite index for page tree queries: type + parent_id + sort_order
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'ix_posts_type_parent_id_sort_order'
                      AND object_id = OBJECT_ID('posts'))
                CREATE INDEX [ix_posts_type_parent_id_sort_order]
                    ON [posts] ([type], [parent_id], [sort_order]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_posts_type_parent_id_sort_order' AND object_id = OBJECT_ID('posts'))
                DROP INDEX [ix_posts_type_parent_id_sort_order] ON [posts];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_posts_parent_id' AND object_id = OBJECT_ID('posts'))
                DROP INDEX [ix_posts_parent_id] ON [posts];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_posts_posts_parent_id')
                ALTER TABLE [posts] DROP CONSTRAINT [fk_posts_posts_parent_id];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('posts') AND name = 'sort_order')
                ALTER TABLE [posts] DROP COLUMN [sort_order];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('posts') AND name = 'parent_id')
                ALTER TABLE [posts] DROP COLUMN [parent_id];
                """);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
