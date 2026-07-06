using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create full-text catalog
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'ft_catalog')
                    CREATE FULLTEXT CATALOG ft_catalog AS DEFAULT;
                """);

            // Create full-text index on posts table
            // Requires a unique index — use the PK (which is on posts.id)
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.fulltext_indexes
                    WHERE object_id = OBJECT_ID('posts')
                )
                BEGIN
                    CREATE FULLTEXT INDEX ON posts (
                        title LANGUAGE 1033,
                        plaintext LANGUAGE 1033,
                        custom_excerpt LANGUAGE 1033
                    )
                    KEY INDEX pk_posts
                    ON ft_catalog
                    WITH (CHANGE_TRACKING = AUTO);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.fulltext_indexes
                    WHERE object_id = OBJECT_ID('posts')
                )
                    DROP FULLTEXT INDEX ON posts;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'ft_catalog')
                    DROP FULLTEXT CATALOG ft_catalog;
                """);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
