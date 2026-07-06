using HowToSoftware.Core.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace HowToSoftware.Infrastructure.Services;

public sealed class ImageStorageService(
    ImageStorageOptions options,
    ILogger<ImageStorageService> logger) : IImageStorageService
{
    private const int MaxWidth = 2000;
    private const int WebpQuality = 80;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".ico"
    };

    public async Task<ImageUploadResult> UploadAsync(
        Stream imageStream,
        string fileName,
        string? contentType = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException($"File type '{extension}' is not allowed.");

        var now = DateTime.UtcNow;
        var monthDir = Path.Combine(
            options.RootPath,
            "content", "images",
            now.Year.ToString(),
            now.Month.ToString("D2"));

        Directory.CreateDirectory(monthDir);

        // SVG and ICO are passed through without processing
        if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ico", StringComparison.OrdinalIgnoreCase))
        {
            return await SaveRawAsync(imageStream, fileName, monthDir, now, ct);
        }

        return await ProcessAndSaveAsync(imageStream, fileName, monthDir, now, ct);
    }

    public Task<bool> DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(relativePath);
        if (fullPath is null || !File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        logger.LogInformation("Deleted image: {Path}", relativePath);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(relativePath);
        return Task.FromResult(fullPath is not null && File.Exists(fullPath));
    }

    private async Task<ImageUploadResult> ProcessAndSaveAsync(
        Stream imageStream, string fileName, string monthDir, DateTime now, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(imageStream, ct);

        // Resize if wider than max
        if (image.Width > MaxWidth)
        {
            var ratio = (double)MaxWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(MaxWidth, newHeight));
        }

        // Save as WebP for best compression (keep original name stem)
        var stem = SanitizeFileName(Path.GetFileNameWithoutExtension(fileName));
        var outputName = DeduplicateFileName(monthDir, stem, ".webp");
        var outputPath = Path.Combine(monthDir, outputName);

        var encoder = new WebpEncoder { Quality = WebpQuality };
        await using var outStream = File.Create(outputPath);
        await image.SaveAsync(outStream, encoder, ct);
        await outStream.FlushAsync(ct);

        var fileInfo = new FileInfo(outputPath);
        var relativeUrl = ToRelativeUrl(outputPath, now);

        logger.LogInformation("Uploaded image: {Url} ({Width}x{Height}, {Size} bytes)",
            relativeUrl, image.Width, image.Height, fileInfo.Length);

        return new ImageUploadResult
        {
            Url = relativeUrl,
            Width = image.Width,
            Height = image.Height,
            FileSize = fileInfo.Length
        };
    }

    private async Task<ImageUploadResult> SaveRawAsync(
        Stream imageStream, string fileName, string monthDir, DateTime now, CancellationToken ct)
    {
        var extension = Path.GetExtension(fileName);
        var stem = SanitizeFileName(Path.GetFileNameWithoutExtension(fileName));
        var outputName = DeduplicateFileName(monthDir, stem, extension);
        var outputPath = Path.Combine(monthDir, outputName);

        await using var outStream = File.Create(outputPath);
        await imageStream.CopyToAsync(outStream, ct);
        await outStream.FlushAsync(ct);

        var fileInfo = new FileInfo(outputPath);
        var relativeUrl = ToRelativeUrl(outputPath, now);

        logger.LogInformation("Uploaded raw file: {Url} ({Size} bytes)", relativeUrl, fileInfo.Length);

        return new ImageUploadResult
        {
            Url = relativeUrl,
            Width = 0,
            Height = 0,
            FileSize = fileInfo.Length
        };
    }

    private string ToRelativeUrl(string fullPath, DateTime now)
    {
        return $"/content/images/{now.Year}/{now.Month:D2}/{Path.GetFileName(fullPath)}";
    }

    private string? ToFullPath(string relativePath)
    {
        // Ensure the path is within /content/images/ to prevent directory traversal
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("content/images/", StringComparison.OrdinalIgnoreCase))
            return null;

        var fullPath = Path.GetFullPath(Path.Combine(options.RootPath, normalized));
        var rootFull = Path.GetFullPath(Path.Combine(options.RootPath, "content", "images"));

        // Verify resolved path is still under the images root (prevents traversal)
        if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            return null;

        return fullPath;
    }

    private static string SanitizeFileName(string name)
    {
        // Keep only alphanumeric, hyphens, underscores
        var sanitized = new string(name
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray());

        return string.IsNullOrEmpty(sanitized) ? "image" : sanitized;
    }

    private static string DeduplicateFileName(string directory, string stem, string extension)
    {
        var candidate = $"{stem}{extension}";
        if (!File.Exists(Path.Combine(directory, candidate)))
            return candidate;

        for (var i = 2; i < 10000; i++)
        {
            candidate = $"{stem}-{i}{extension}";
            if (!File.Exists(Path.Combine(directory, candidate)))
                return candidate;
        }

        // Extremely unlikely fallback
        return $"{stem}-{Guid.NewGuid():N}{extension}";
    }
}

public sealed class ImageStorageOptions
{
    /// <summary>
    /// Root filesystem path where /content/images/ is stored.
    /// Typically the web app's content root or a Docker volume mount.
    /// </summary>
    public required string RootPath { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
