using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class TSqlGeneratorTests
{
    [Fact]
    public void ConvertString_SimpleString_WrapsWithNPrefix()
    {
        var result = TSqlGenerator.ConvertString("Hello World");
        Assert.Equal("N'Hello World'", result);
    }

    [Fact]
    public void ConvertString_MySqlEscapedQuote_ConvertedToTSqlDoubleQuote()
    {
        // MySQL stores: Henry\'s → parser keeps the backslash escape
        var result = TSqlGenerator.ConvertString(@"Henry\'s Blog");
        Assert.Equal("N'Henry''s Blog'", result);
    }

    [Fact]
    public void ConvertString_EscapedBackslash_ConvertedToSingleBackslash()
    {
        var result = TSqlGenerator.ConvertString(@"path\\to\\file");
        Assert.Equal(@"N'path\to\file'", result);
    }

    [Fact]
    public void ConvertString_EscapedNewline_ConvertedToActualNewline()
    {
        var result = TSqlGenerator.ConvertString(@"line1\nline2");
        Assert.Equal("N'line1\nline2'", result);
    }

    [Fact]
    public void ConvertString_EmptyString_ReturnsEmptyNVarchar()
    {
        var result = TSqlGenerator.ConvertString("");
        Assert.Equal("N''", result);
    }

    [Fact]
    public void ConvertString_JsonContent_PreservesStructure()
    {
        var json = @"{""key"":""value"",""nested"":{""a"":1}}";
        var result = TSqlGenerator.ConvertString(json);
        Assert.Equal(@"N'{""key"":""value"",""nested"":{""a"":1}}'", result);
    }

    [Fact]
    public void ConvertDateTime_MySqlFormat_ConvertsToISO8601()
    {
        var result = TSqlGenerator.ConvertDateTime("2025-12-04 03:01:17");
        Assert.Equal("'2025-12-04T03:01:17.0000000'", result);
    }

    [Fact]
    public void ConvertDateTime_DifferentDateTime_ConvertsCorrectly()
    {
        var result = TSqlGenerator.ConvertDateTime("2026-01-15 14:30:00");
        Assert.Equal("'2026-01-15T14:30:00.0000000'", result);
    }

    [Fact]
    public void ConvertDateTime_InvalidFormat_FallsBackToStringWrapping()
    {
        var result = TSqlGenerator.ConvertDateTime("not-a-date");
        Assert.Equal("N'not-a-date'", result);
    }

    [Fact]
    public void ConvertValue_NullValue_ReturnsNULL()
    {
        var result = TSqlGenerator.ConvertValue(null, TSqlGenerator.ColType.String);
        Assert.Equal("NULL", result);
    }

    [Fact]
    public void ConvertValue_BooleanZero_Returns0()
    {
        var result = TSqlGenerator.ConvertValue("0", TSqlGenerator.ColType.Boolean);
        Assert.Equal("0", result);
    }

    [Fact]
    public void ConvertValue_BooleanOne_Returns1()
    {
        var result = TSqlGenerator.ConvertValue("1", TSqlGenerator.ColType.Boolean);
        Assert.Equal("1", result);
    }

    [Fact]
    public void ConvertValue_Integer_PassesThrough()
    {
        var result = TSqlGenerator.ConvertValue("42", TSqlGenerator.ColType.Integer);
        Assert.Equal("42", result);
    }

    [Fact]
    public void Generate_SingleTableInsert_ProducesValidTSql()
    {
        var insert = new ParsedInsert(
            "posts",
            ["id", "title", "featured", "created_at"],
            [
                ["abc123", "Test Post", "0", "2025-12-04 03:01:17"]
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("INSERT INTO [dbo].[posts]", result);
        Assert.Contains("N'abc123'", result);
        Assert.Contains("N'Test Post'", result);
        Assert.Contains("'2025-12-04T03:01:17.0000000'", result);
        Assert.Contains("SET NOCOUNT ON;", result);
        Assert.Contains("BEGIN TRANSACTION;", result);
        Assert.Contains("COMMIT TRANSACTION;", result);
        Assert.Contains("NOCHECK CONSTRAINT ALL", result);
        Assert.Contains("CHECK CONSTRAINT ALL", result);
    }

    [Fact]
    public void Generate_NullValues_OutputNULL()
    {
        var insert = new ParsedInsert(
            "posts",
            ["id", "title", "published_at"],
            [
                ["abc123", "Draft", null]
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("NULL", result);
        Assert.Contains("N'Draft'", result);
    }

    [Fact]
    public void Generate_MultipleTables_IncludesBoth()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts", ["id"], [["1"]]),
            new ParsedInsert("tags", ["id"], [["2"]]),
        };

        var result = TSqlGenerator.Generate(inserts);

        Assert.Contains("Table: posts", result);
        Assert.Contains("Table: tags", result);
        Assert.Contains("[dbo].[posts]", result);
        Assert.Contains("[dbo].[tags]", result);
    }

    [Fact]
    public void Generate_GhostId24Char_PreservesAsNVarchar()
    {
        var insert = new ParsedInsert(
            "posts",
            ["id"],
            [
                ["6930f97d04b3d10001c01beb"]
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("N'6930f97d04b3d10001c01beb'", result);
    }

    [Fact]
    public void InferColumnType_CreatedAt_ReturnsDateTime()
    {
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.InferColumnType("created_at"));
    }

    [Fact]
    public void InferColumnType_UpdatedAt_ReturnsDateTime()
    {
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.InferColumnType("updated_at"));
    }

    [Fact]
    public void InferColumnType_SortOrder_ReturnsInteger()
    {
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.InferColumnType("sort_order"));
    }

    [Fact]
    public void InferColumnType_UnknownColumn_DefaultsToString()
    {
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.InferColumnType("some_column"));
    }

    #region Post Content Table Column Types

    [Fact]
    public void Generate_PostsTagsTable_HandlesJoinColumnsCorrectly()
    {
        var insert = new ParsedInsert(
            "posts_tags",
            ["id", "post_id", "tag_id", "sort_order"],
            [
                ["pt1", "post1", "tag1", "0"],
                ["pt2", "post1", "tag2", "1"],
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("[dbo].[posts_tags]", result);
        Assert.Contains("N'pt1'", result);
        Assert.Contains("N'post1'", result);
        Assert.Contains("N'tag1'", result);
        // sort_order should be integer (no N'' prefix)
        Assert.DoesNotContain("N'0'", result);
        Assert.DoesNotContain("N'1'", result);
    }

    [Fact]
    public void Generate_PostsAuthorsTable_HandlesJoinColumnsCorrectly()
    {
        var insert = new ParsedInsert(
            "posts_authors",
            ["id", "post_id", "author_id", "sort_order"],
            [
                ["pa1", "post1", "author1", "0"],
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("[dbo].[posts_authors]", result);
        Assert.Contains("N'pa1'", result);
        Assert.Contains("N'author1'", result);
    }

    [Fact]
    public void Generate_PostsMetaTable_HandlesAllColumnTypes()
    {
        var insert = new ParsedInsert(
            "posts_meta",
            ["id", "post_id", "og_title", "meta_description", "email_only"],
            [
                ["m1", "post1", "OG Title", "Description", "0"],
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("N'OG Title'", result);
        Assert.Contains("N'Description'", result);
        // email_only is boolean: should be 0, not N'0'
        Assert.Contains(", 0);", result);
    }

    [Fact]
    public void Generate_PostRevisionsTable_HandlesTimestampAndContent()
    {
        var insert = new ParsedInsert(
            "post_revisions",
            ["id", "post_id", "lexical", "created_at_ts", "created_at", "author_id", "title"],
            [
                ["r1", "post1", "{\"root\":{}}", "1733277677000", "2025-12-04 03:01:17", "author1", "Rev Title"],
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("[dbo].[post_revisions]", result);
        Assert.Contains("N'{\"root\":{}}'" , result);
        Assert.Contains("1733277677000", result); // created_at_ts as integer
        Assert.Contains("'2025-12-04T03:01:17.0000000'", result); // created_at as datetime
    }

    [Fact]
    public void Generate_MobiledocRevisionsTable_HandlesCorrectly()
    {
        var insert = new ParsedInsert(
            "mobiledoc_revisions",
            ["id", "post_id", "mobiledoc", "created_at_ts", "created_at"],
            [
                ["mr1", "post1", "{\"version\":\"0.3.1\"}", "1733277677000", "2025-12-04 03:01:17"],
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("[dbo].[mobiledoc_revisions]", result);
        Assert.Contains("N'{\"version\":\"0.3.1\"}'", result);
    }

    [Fact]
    public void ResolveColumnType_KnownTable_ReturnsExplicitType()
    {
        Assert.Equal(TSqlGenerator.ColType.Boolean, TSqlGenerator.ResolveColumnType("posts", "featured"));
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("posts", "created_at"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("posts", "title"));
    }

    [Fact]
    public void ResolveColumnType_JoinTable_ReturnsExplicitType()
    {
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("posts_tags", "sort_order"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("posts_tags", "post_id"));
    }

    [Fact]
    public void ResolveColumnType_UnknownTable_FallsBackToInference()
    {
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("unknown_table", "created_at"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("unknown_table", "sort_order"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("unknown_table", "name"));
    }

    [Fact]
    public void Generate_UnknownTable_InfersColumnTypes()
    {
        // An unmapped table should still get sort_order as integer and *_at as datetime
        var insert = new ParsedInsert(
            "some_future_table",
            ["id", "name", "sort_order", "created_at"],
            [
                ["1", "Item", "5", "2025-12-04 03:01:17"],
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("N'1'", result);      // id: string (inferred)
        Assert.Contains("N'Item'", result);    // name: string (inferred)
        Assert.DoesNotContain("N'5'", result); // sort_order: integer (inferred, not N'5')
        Assert.Contains("'2025-12-04T03:01:17.0000000'", result); // created_at: datetime (inferred)
    }

    [Fact]
    public void Generate_TableNameWithBracket_EscapesAsDoubleBracket()
    {
        // A malicious table name containing ] should be escaped to ]] inside brackets
        var insert = new ParsedInsert(
            "evil]table",
            ["id", "col]name"],
            [
                ["1", "value"],
            ]);

        var result = TSqlGenerator.Generate([insert]);

        Assert.Contains("[dbo].[evil]]table]", result);
        Assert.Contains("[col]]name]", result);
        Assert.DoesNotContain("[dbo].[evil]table]", result.Replace("evil]]table", ""));
    }

    #endregion
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
