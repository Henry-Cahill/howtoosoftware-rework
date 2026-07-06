using System.Text;

namespace HowToSoftware.Migrator;

/// <summary>
/// Represents a single parsed INSERT statement from a MySQL dump file.
/// </summary>
public sealed record ParsedInsert(string TableName, string[] Columns, List<string?[]> Rows);

/// <summary>
/// Parses MySQL dump files produced by mysqldump, extracting INSERT statements
/// into structured data (table name, columns, row values).
/// </summary>
public static class MySqlDumpParser
{
    /// <summary>
    /// Reads a MySQL dump file and yields one <see cref="ParsedInsert"/> per INSERT statement.
    /// Handles multi-row INSERTs, MySQL-escaped strings, and NULL values.
    /// </summary>
    public static IEnumerable<ParsedInsert> Parse(string filePath)
    {
        foreach (var line in File.ReadLines(filePath))
        {
            if (!line.StartsWith("INSERT INTO ", StringComparison.OrdinalIgnoreCase))
                continue;

            var parsed = ParseInsertLine(line);
            if (parsed is not null)
                yield return parsed;
        }
    }

    /// <summary>
    /// Parses a single INSERT INTO line from a MySQL dump.
    /// Format: INSERT INTO `table` (`col1`, `col2`, ...) VALUES (v1, v2, ...),(v1, v2, ...);
    /// </summary>
    internal static ParsedInsert? ParseInsertLine(string line)
    {
        var pos = 0;

        // Skip "INSERT INTO "
        pos = SkipLiteral(line, pos, "INSERT INTO ");
        if (pos < 0) return null;

        // Parse table name (backtick-quoted)
        var tableName = ReadBacktickIdentifier(line, ref pos);
        if (tableName is null) return null;

        // Skip " ("
        pos = SkipWhitespace(line, pos);
        if (pos >= line.Length || line[pos] != '(') return null;
        pos++; // skip '('

        // Parse column list
        var columns = new List<string>();
        while (pos < line.Length)
        {
            pos = SkipWhitespace(line, pos);
            if (pos < line.Length && line[pos] == ')')
            {
                pos++;
                break;
            }

            if (columns.Count > 0)
            {
                if (pos < line.Length && line[pos] == ',')
                    pos++;
                pos = SkipWhitespace(line, pos);
            }

            var col = ReadBacktickIdentifier(line, ref pos);
            if (col is null) return null;
            columns.Add(col);
        }

        // Skip " VALUES "
        pos = SkipWhitespace(line, pos);
        pos = SkipLiteral(line, pos, "VALUES ");
        if (pos < 0) return null;

        // Parse value tuples
        var rows = new List<string?[]>();
        while (pos < line.Length)
        {
            pos = SkipWhitespace(line, pos);
            if (pos >= line.Length) break;

            if (line[pos] == '(')
            {
                pos++; // skip '('
                var values = ParseValueTuple(line, ref pos, columns.Count);
                if (values is not null)
                    rows.Add(values);
            }
            else if (line[pos] == ',')
            {
                pos++; // skip comma between tuples
            }
            else if (line[pos] == ';')
            {
                break; // end of statement
            }
            else
            {
                break;
            }
        }

        return new ParsedInsert(tableName, [.. columns], rows);
    }

    /// <summary>
    /// Parses a single value tuple: v1, v2, v3, ...)
    /// Position should be right after the opening '('.
    /// Returns raw MySQL string values (still MySQL-escaped) or null for NULL.
    /// </summary>
    private static string?[]? ParseValueTuple(string line, ref int pos, int expectedCount)
    {
        var values = new List<string?>(expectedCount);

        while (pos < line.Length)
        {
            pos = SkipWhitespace(line, pos);
            if (pos >= line.Length) break;

            if (line[pos] == ')')
            {
                pos++; // skip ')'
                break;
            }

            if (values.Count > 0)
            {
                if (line[pos] == ',')
                    pos++;
                pos = SkipWhitespace(line, pos);
            }

            var value = ReadValue(line, ref pos);
            values.Add(value);
        }

        return values.Count == expectedCount ? [.. values] : null;
    }

    /// <summary>
    /// Reads a single value from the INSERT statement.
    /// Handles: NULL, numeric literals, and single-quoted strings with MySQL escaping.
    /// </summary>
    private static string? ReadValue(string line, ref int pos)
    {
        if (pos >= line.Length) return null;

        // NULL
        if (pos + 4 <= line.Length && line.AsSpan(pos, 4).Equals("NULL", StringComparison.OrdinalIgnoreCase))
        {
            // Make sure it's actually NULL and not a string starting with NULL
            if (pos + 4 >= line.Length || line[pos + 4] == ',' || line[pos + 4] == ')' || line[pos + 4] == ' ')
            {
                pos += 4;
                return null;
            }
        }

        // Single-quoted string
        if (line[pos] == '\'')
        {
            return ReadQuotedString(line, ref pos);
        }

        // Numeric or other unquoted value
        return ReadUnquotedValue(line, ref pos);
    }

    /// <summary>
    /// Reads a MySQL single-quoted string, handling backslash escapes.
    /// Returns the raw content between quotes (with MySQL escapes still present).
    /// </summary>
    private static string ReadQuotedString(string line, ref int pos)
    {
        pos++; // skip opening quote
        var sb = new StringBuilder();

        while (pos < line.Length)
        {
            var ch = line[pos];

            if (ch == '\\')
            {
                // MySQL backslash escape — keep the escape sequence as-is
                sb.Append(ch);
                pos++;
                if (pos < line.Length)
                {
                    sb.Append(line[pos]);
                    pos++;
                }
                continue;
            }

            if (ch == '\'')
            {
                // Check for '' (MySQL alternate escape for single quote)
                if (pos + 1 < line.Length && line[pos + 1] == '\'')
                {
                    sb.Append("''");
                    pos += 2;
                    continue;
                }

                // End of string
                pos++;
                return sb.ToString();
            }

            sb.Append(ch);
            pos++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Reads an unquoted value (numbers, etc.) until comma or closing paren.
    /// </summary>
    private static string ReadUnquotedValue(string line, ref int pos)
    {
        var start = pos;
        while (pos < line.Length && line[pos] != ',' && line[pos] != ')' && line[pos] != ' ')
            pos++;
        return line[start..pos];
    }

    /// <summary>
    /// Reads a backtick-quoted identifier like `table_name`.
    /// </summary>
    private static string? ReadBacktickIdentifier(string line, ref int pos)
    {
        pos = SkipWhitespace(line, pos);
        if (pos >= line.Length || line[pos] != '`') return null;
        pos++; // skip opening backtick

        var start = pos;
        while (pos < line.Length && line[pos] != '`')
            pos++;

        if (pos >= line.Length) return null;
        var name = line[start..pos];
        pos++; // skip closing backtick
        return name;
    }

    private static int SkipWhitespace(string line, int pos)
    {
        while (pos < line.Length && char.IsWhiteSpace(line[pos]))
            pos++;
        return pos;
    }

    private static int SkipLiteral(string line, int pos, string literal)
    {
        if (pos + literal.Length > line.Length)
            return -1;
        if (!line.AsSpan(pos, literal.Length).Equals(literal, StringComparison.OrdinalIgnoreCase))
            return -1;
        return pos + literal.Length;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
