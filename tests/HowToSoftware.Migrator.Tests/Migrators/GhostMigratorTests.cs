using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class GhostMigratorTests
{
    [Fact]
    public void Migrate_ValidDumpFile_ProducesOutputFile()
    {
        var dumpFile = Path.GetTempFileName();
        var outputFile = Path.Combine(Path.GetTempPath(), $"migration_test_{Guid.NewGuid()}.sql");

        try
        {
            File.WriteAllText(dumpFile, """
                -- MySQL dump 10.13
                LOCK TABLES `posts` WRITE;
                INSERT INTO `posts` (`id`, `title`, `created_at`) VALUES ('6930f97d04b3d10001c01beb','Test Post','2025-12-04 03:01:17');
                UNLOCK TABLES;
                """);

            var migrator = new GhostMigrator();
            var result = migrator.Migrate([dumpFile], outputFile);

            Assert.Equal(1, result.TableCount);
            Assert.Equal(1, result.RowCount);
            Assert.Contains("posts", result.TableNames);
            Assert.True(File.Exists(outputFile));

            var content = File.ReadAllText(outputFile);
            Assert.Contains("INSERT INTO [dbo].[posts]", content);
            Assert.Contains("N'6930f97d04b3d10001c01beb'", content);
            Assert.Contains("'2025-12-04T03:01:17.0000000'", content);
        }
        finally
        {
            File.Delete(dumpFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }

    [Fact]
    public void Migrate_MissingFile_ThrowsFileNotFoundException()
    {
        var migrator = new GhostMigrator();
        Assert.Throws<FileNotFoundException>(() =>
            migrator.Migrate(["nonexistent_file.sql"], "output.sql"));
    }

    [Fact]
    public void Migrate_EmptyDump_ReturnsZeroCounts()
    {
        var dumpFile = Path.GetTempFileName();
        var outputFile = Path.Combine(Path.GetTempPath(), $"migration_empty_{Guid.NewGuid()}.sql");

        try
        {
            File.WriteAllText(dumpFile, """
                -- MySQL dump 10.13
                -- Empty dump with no INSERT statements
                LOCK TABLES `posts` WRITE;
                UNLOCK TABLES;
                """);

            var migrator = new GhostMigrator();
            var result = migrator.Migrate([dumpFile], outputFile);

            Assert.Equal(0, result.TableCount);
            Assert.Equal(0, result.RowCount);
        }
        finally
        {
            File.Delete(dumpFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }

    [Fact]
    public void Migrate_MultipleDumpFiles_CombinesResults()
    {
        var dump1 = Path.GetTempFileName();
        var dump2 = Path.GetTempFileName();
        var outputFile = Path.Combine(Path.GetTempPath(), $"migration_multi_{Guid.NewGuid()}.sql");

        try
        {
            File.WriteAllText(dump1, "INSERT INTO `posts` (`id`, `title`) VALUES ('1','Post 1');");
            File.WriteAllText(dump2, "INSERT INTO `tags` (`id`, `name`) VALUES ('2','Tag 1');");

            var migrator = new GhostMigrator();
            var result = migrator.Migrate([dump1, dump2], outputFile);

            Assert.Equal(2, result.TableCount);
            Assert.Equal(2, result.RowCount);
            Assert.Contains("posts", result.TableNames);
            Assert.Contains("tags", result.TableNames);
        }
        finally
        {
            File.Delete(dump1);
            File.Delete(dump2);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
