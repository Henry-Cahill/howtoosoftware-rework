using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class MySqlDumpParserTests
{
    [Fact]
    public void ParseInsertLine_SimpleInsert_ExtractsTableColumnsAndValues()
    {
        var line = "INSERT INTO `posts` (`id`, `title`) VALUES ('abc123','Hello World');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Equal("posts", result.TableName);
        Assert.Equal(["id", "title"], result.Columns);
        Assert.Single(result.Rows);
        Assert.Equal("abc123", result.Rows[0][0]);
        Assert.Equal("Hello World", result.Rows[0][1]);
    }

    [Fact]
    public void ParseInsertLine_MultipleRows_ParsesAll()
    {
        var line = "INSERT INTO `tags` (`id`, `name`) VALUES ('1','Tag A'),('2','Tag B'),('3','Tag C');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Equal("tags", result.TableName);
        Assert.Equal(3, result.Rows.Count);
        Assert.Equal("1", result.Rows[0][0]);
        Assert.Equal("Tag A", result.Rows[0][1]);
        Assert.Equal("3", result.Rows[2][0]);
        Assert.Equal("Tag C", result.Rows[2][1]);
    }

    [Fact]
    public void ParseInsertLine_NullValues_ParsedAsNull()
    {
        var line = "INSERT INTO `posts` (`id`, `title`, `slug`) VALUES ('abc',NULL,'my-slug');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Single(result.Rows);
        Assert.Equal("abc", result.Rows[0][0]);
        Assert.Null(result.Rows[0][1]);
        Assert.Equal("my-slug", result.Rows[0][2]);
    }

    [Fact]
    public void ParseInsertLine_EscapedSingleQuote_PreservedCorrectly()
    {
        var line = @"INSERT INTO `posts` (`id`, `title`) VALUES ('1','Henry\'s Blog');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Single(result.Rows);
        Assert.Equal(@"Henry\'s Blog", result.Rows[0][1]);
    }

    [Fact]
    public void ParseInsertLine_EscapedBackslash_PreservedCorrectly()
    {
        var line = @"INSERT INTO `posts` (`id`, `html`) VALUES ('1','path\\to\\file');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Equal(@"path\\to\\file", result.Rows[0][1]);
    }

    [Fact]
    public void ParseInsertLine_NumericValues_ParsedAsStrings()
    {
        var line = "INSERT INTO `posts` (`id`, `featured`) VALUES ('abc',0);";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Equal("0", result.Rows[0][1]);
    }

    [Fact]
    public void ParseInsertLine_DateTimeValue_ParsedAsString()
    {
        var line = "INSERT INTO `posts` (`id`, `created_at`) VALUES ('abc','2025-12-04 03:01:17');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Equal("2025-12-04 03:01:17", result.Rows[0][1]);
    }

    [Fact]
    public void ParseInsertLine_JsonValue_ParsedCorrectly()
    {
        var line = @"INSERT INTO `posts` (`id`, `lexical`) VALUES ('abc','{""key"":""value""}');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Equal(@"{""key"":""value""}", result.Rows[0][1]);
    }

    [Fact]
    public void ParseInsertLine_ComplexJson_WithEscapes()
    {
        var line = @"INSERT INTO `posts` (`id`, `mobiledoc`) VALUES ('1','{""sections"":[[1,""p"",[[0,[],0,""That\'s great""]]]]}');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Contains(@"That\'s great", result.Rows[0][1]);
    }

    [Fact]
    public void ParseInsertLine_NonInsertLine_ReturnsNull()
    {
        var line = "-- This is a comment";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.Null(result);
    }

    [Fact]
    public void ParseInsertLine_EmptyValues_ParsedCorrectly()
    {
        var line = "INSERT INTO `posts` (`id`, `locale`) VALUES ('abc','');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Equal("", result.Rows[0][1]);
    }

    [Fact]
    public void Parse_FileWithMySqlHeader_SkipsNonInsertLines()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
                -- MySQL dump 10.13
                /*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
                LOCK TABLES `posts` WRITE;
                INSERT INTO `posts` (`id`, `title`) VALUES ('1','Test Post');
                UNLOCK TABLES;
                """);

            var results = MySqlDumpParser.Parse(tempFile).ToList();

            Assert.Single(results);
            Assert.Equal("posts", results[0].TableName);
            Assert.Single(results[0].Rows);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseInsertLine_GhostIdFormat_24CharHex()
    {
        var line = "INSERT INTO `posts` (`id`) VALUES ('6930f97d04b3d10001c01beb');";

        var result = MySqlDumpParser.ParseInsertLine(line);

        Assert.NotNull(result);
        Assert.Equal("6930f97d04b3d10001c01beb", result.Rows[0][0]);
        Assert.Equal(24, result.Rows[0][0]!.Length);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
