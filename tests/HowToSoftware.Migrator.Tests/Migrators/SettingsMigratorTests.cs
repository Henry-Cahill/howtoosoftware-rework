using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class SettingsMigratorTests
{
    #region IsSettingsTable

    [Theory]
    [InlineData("settings", true)]
    [InlineData("custom_theme_settings", true)]
    [InlineData("posts", false)]
    [InlineData("members", false)]
    [InlineData("users", false)]
    [InlineData("newsletters", false)]
    public void IsSettingsTable_ReturnsCorrectResult(string tableName, bool expected)
    {
        Assert.Equal(expected, SettingsMigrator.IsSettingsTable(tableName));
    }

    [Fact]
    public void IsSettingsTable_CaseInsensitive()
    {
        Assert.True(SettingsMigrator.IsSettingsTable("SETTINGS"));
        Assert.True(SettingsMigrator.IsSettingsTable("Custom_Theme_Settings"));
    }

    #endregion

    #region ProcessSettings — Empty & Non-Settings

    [Fact]
    public void ProcessSettings_EmptyInserts_ReturnsZeroStats()
    {
        var result = SettingsMigrator.ProcessSettings([]);

        Assert.Equal(0, result.Stats.SettingsCount);
        Assert.Equal(0, result.Stats.CustomThemeSettingsCount);
        Assert.Equal(0, result.Stats.TotalCount);
    }

    [Fact]
    public void ProcessSettings_NonSettingsInserts_PassedThrough()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "title"],
                [["1", "Hello World"]])
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        Assert.Equal(0, result.Stats.SettingsCount);
        Assert.Single(result.TransformedInserts);
        Assert.Equal("posts", result.TransformedInserts[0].TableName);
    }

    #endregion

    #region ProcessSettings — Settings Table Counting

    [Fact]
    public void ProcessSettings_SettingsTable_CountsTotal()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "title", "howtosoftware", "string"],
                    ["2", "core", "description", "Thoughts, stories and ideas.", "string"],
                    ["3", "core", "accent_color", "#FF1A75", "string"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        Assert.Equal(3, result.Stats.SettingsCount);
    }

    [Fact]
    public void ProcessSettings_SettingsTable_BreaksDownByGroup()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "title", "howtosoftware", "string"],
                    ["2", "core", "description", "Thoughts.", "string"],
                    ["3", "email", "mailgun_domain", "mg.howtoosoftware.com", "string"],
                    ["4", "members", "members_signup_access", "all", "string"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        Assert.Equal(4, result.Stats.SettingsCount);
        Assert.Equal(2, result.Stats.CountByGroup["core"]);
        Assert.Equal(1, result.Stats.CountByGroup["email"]);
        Assert.Equal(1, result.Stats.CountByGroup["members"]);
    }

    [Fact]
    public void ProcessSettings_SettingsTable_BreaksDownByType()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "title", "howtosoftware", "string"],
                    ["2", "core", "active_theme", "howtoosoftware-custom", "string"],
                    ["3", "core", "portal_button", "false", "boolean"],
                    ["4", "core", "portal_plans", "[\"free\"]", "array"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        Assert.Equal(4, result.Stats.SettingsCount);
        Assert.Equal(2, result.Stats.CountByType["string"]);
        Assert.Equal(1, result.Stats.CountByType["boolean"]);
        Assert.Equal(1, result.Stats.CountByType["array"]);
    }

    [Fact]
    public void ProcessSettings_SettingsTable_CollectsKeys()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "title", "howtosoftware", "string"],
                    ["2", "core", "description", "Thoughts.", "string"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        Assert.Equal(2, result.Stats.Keys.Count);
        Assert.Contains("title", result.Stats.Keys);
        Assert.Contains("description", result.Stats.Keys);
    }

    #endregion

    #region ProcessSettings — Custom Theme Settings

    [Fact]
    public void ProcessSettings_CustomThemeSettings_CountsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("custom_theme_settings",
                ["id", "theme", "key", "type", "value"],
                [
                    ["1", "howtoosoftware-custom", "navigation_layout", "select", "Logo on cover"],
                    ["2", "howtoosoftware-custom", "color_scheme", "select", "Light"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        Assert.Equal(0, result.Stats.SettingsCount);
        Assert.Equal(2, result.Stats.CustomThemeSettingsCount);
        Assert.Equal(2, result.Stats.TotalCount);
    }

    #endregion

    #region ProcessSettings — URL Rewriting

    [Fact]
    public void ProcessSettings_RewritesGhostUrlInValues()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "icon", "__GHOST_URL__/content/images/2025/12/H2S_Thumbnail_White.png", "string"],
                    ["2", "core", "title", "howtosoftware", "string"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts, "https://howtoosoftware.com");

        var settingsInsert = result.TransformedInserts.First(i => i.TableName == "settings");
        var valueColIdx = Array.IndexOf(settingsInsert.Columns, "value");

        Assert.Equal("https://howtoosoftware.com/content/images/2025/12/H2S_Thumbnail_White.png",
            settingsInsert.Rows[0][valueColIdx]);
        Assert.Equal("howtosoftware", settingsInsert.Rows[1][valueColIdx]);
        Assert.Equal(1, result.Stats.UrlRewriteCount);
    }

    [Fact]
    public void ProcessSettings_NoSiteUrl_DoesNotRewrite()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "icon", "__GHOST_URL__/content/images/icon.png", "string"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        var settingsInsert = result.TransformedInserts.First(i => i.TableName == "settings");
        var valueColIdx = Array.IndexOf(settingsInsert.Columns, "value");

        Assert.Equal("__GHOST_URL__/content/images/icon.png", settingsInsert.Rows[0][valueColIdx]);
        Assert.Equal(0, result.Stats.UrlRewriteCount);
    }

    [Fact]
    public void ProcessSettings_SiteUrlTrailingSlash_TrimmedBeforeRewrite()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "icon", "__GHOST_URL__/content/images/icon.png", "string"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts, "https://howtoosoftware.com/");

        var settingsInsert = result.TransformedInserts.First(i => i.TableName == "settings");
        var valueColIdx = Array.IndexOf(settingsInsert.Columns, "value");

        Assert.Equal("https://howtoosoftware.com/content/images/icon.png",
            settingsInsert.Rows[0][valueColIdx]);
    }

    [Fact]
    public void ProcessSettings_NullValue_HandledGracefully()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "twitter", null, "string"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts, "https://howtoosoftware.com");

        var settingsInsert = result.TransformedInserts.First(i => i.TableName == "settings");
        var valueColIdx = Array.IndexOf(settingsInsert.Columns, "value");

        Assert.Null(settingsInsert.Rows[0][valueColIdx]);
        Assert.Equal(0, result.Stats.UrlRewriteCount);
    }

    #endregion

    #region ProcessSettings — Mixed Tables

    [Fact]
    public void ProcessSettings_MixedTables_ProcessesOnlySettingsTables()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "title"],
                [["1", "Hello"]]),
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "title", "howtosoftware", "string"],
                ]),
            new ParsedInsert("custom_theme_settings",
                ["id", "theme", "key", "type", "value"],
                [
                    ["1", "mytheme", "color_scheme", "select", "Light"],
                ]),
            new ParsedInsert("members",
                ["id", "email"],
                [["1", "test@example.com"]]),
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        Assert.Equal(1, result.Stats.SettingsCount);
        Assert.Equal(1, result.Stats.CustomThemeSettingsCount);
        Assert.Equal(4, result.TransformedInserts.Count);
    }

    #endregion

    #region ProcessSettings — WithoutGroupColumn

    [Fact]
    public void ProcessSettings_WithoutGroupColumn_CountsWithoutGroupBreakdown()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "key", "value", "type"],
                [
                    ["1", "title", "howtosoftware", "string"],
                    ["2", "description", "Thoughts.", "string"],
                ])
        };

        var result = SettingsMigrator.ProcessSettings(inserts);

        Assert.Equal(2, result.Stats.SettingsCount);
        Assert.Empty(result.Stats.CountByGroup);
    }

    #endregion

    #region ToString

    [Fact]
    public void Stats_ToString_FormatsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("settings",
                ["id", "group", "key", "value", "type"],
                [
                    ["1", "core", "title", "howtosoftware", "string"],
                    ["2", "core", "description", "Thoughts.", "string"],
                ]),
            new ParsedInsert("custom_theme_settings",
                ["id", "theme", "key", "type", "value"],
                [
                    ["1", "mytheme", "color", "select", "Light"],
                ]),
        };

        var result = SettingsMigrator.ProcessSettings(inserts);
        var str = result.Stats.ToString();

        Assert.Contains("Settings: 2", str);
        Assert.Contains("core: 2", str);
        Assert.Contains("Theme settings: 1", str);
    }

    #endregion
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
