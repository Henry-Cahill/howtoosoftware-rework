namespace HowToSoftware.Core.Interfaces;

public interface IImageStorageService
{
    /// <summary>
    /// Stores an uploaded image, resizing/optimizing it, and returns the relative URL path.
    /// Images are stored under /content/images/{year}/{month}/.
    /// </summary>
    Task<ImageUploadResult> UploadAsync(
        Stream imageStream,
        string fileName,
        string? contentType = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an image by its relative URL path (e.g. /content/images/2026/03/photo.webp).
    /// </summary>
    Task<bool> DeleteAsync(string relativePath, CancellationToken ct = default);

    /// <summary>
    /// Checks whether an image exists at the given relative path.
    /// </summary>
    Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default);
}

public record ImageUploadResult
{
    /// <summary>Relative URL path usable in img src (e.g. /content/images/2026/03/photo.webp).</summary>
    public required string Url { get; init; }

    /// <summary>Width in pixels after processing.</summary>
    public int Width { get; init; }

    /// <summary>Height in pixels after processing.</summary>
    public int Height { get; init; }

    /// <summary>File size in bytes after processing.</summary>
    public long FileSize { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
