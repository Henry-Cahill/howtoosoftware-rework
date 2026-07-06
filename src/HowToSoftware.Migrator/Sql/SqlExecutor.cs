using Microsoft.Data.SqlClient;

namespace HowToSoftware.Migrator;

/// <summary>
/// Executes a T-SQL script file against SQL Server, splitting on GO batch separators.
/// </summary>
public static class SqlExecutor
{
    /// <summary>
    /// Reads a T-SQL script file and executes each batch (separated by GO) against the database.
    /// </summary>
    public static async Task ExecuteScriptAsync(string scriptPath, string connectionString)
    {
        var scriptText = await File.ReadAllTextAsync(scriptPath);
        var batches = SplitIntoBatches(scriptText);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        Console.WriteLine($"Connected to: {connection.DataSource}/{connection.Database}");

        var batchNumber = 0;
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            batchNumber++;
            try
            {
                await using var command = new SqlCommand(batch, connection);
                command.CommandTimeout = 120;
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"Error in batch {batchNumber}:");
                Console.Error.WriteLine($"  {ex.Message}");
                // Print first 200 chars of the failing batch for context
                var preview = batch.Length > 200 ? batch[..200] + "..." : batch;
                Console.Error.WriteLine($"  Batch preview: {preview}");
                throw;
            }
        }

        Console.WriteLine($"Executed {batchNumber} batches successfully.");
    }

    /// <summary>
    /// Splits a T-SQL script on GO batch separators (case-insensitive, must be on its own line).
    /// </summary>
    internal static List<string> SplitIntoBatches(string script)
    {
        var batches = new List<string>();
        var currentBatch = new System.Text.StringBuilder();

        foreach (var line in script.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                if (currentBatch.Length > 0)
                {
                    batches.Add(currentBatch.ToString());
                    currentBatch.Clear();
                }
            }
            else
            {
                currentBatch.AppendLine(line);
            }
        }

        // Add any remaining batch after the last GO
        if (currentBatch.Length > 0)
            batches.Add(currentBatch.ToString());

        return batches;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
