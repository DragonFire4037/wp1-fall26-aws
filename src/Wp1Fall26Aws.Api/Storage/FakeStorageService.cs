using Wp1Fall26Aws.Api.Models;

namespace Wp1Fall26Aws.Api.Storage;

/// <summary>
/// In-memory storage service for testing. Stores files in a dictionary with metadata.
/// </summary>
public class FakeStorageService : IStorageService
{
    private class StoredDocument
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    private readonly Dictionary<string, StoredDocument> _files = new();

    public async Task<string> UploadAsync(Stream file, string objectKey, string contentType, CancellationToken cancellationToken = default)
    {
        using MemoryStream memoryStream = new MemoryStream();

        await file.CopyToAsync(memoryStream, cancellationToken);
        _files[objectKey] = new StoredDocument
        {
            Content = memoryStream.ToArray(),
            ContentType = contentType,
            UploadedAt = DateTime.UtcNow
        };
        return objectKey;
    }

    public Task<IEnumerable<DocumentMetadata>> ListAsync(CancellationToken cancellationToken = default)
    {
        var documents = _files
            .Select(kvp => new DocumentMetadata
            {
                Key = kvp.Key,
                FileName = ExtractFileNameFromKey(kvp.Key),
                Size = kvp.Value.Content.Length,
                ContentType = kvp.Value.ContentType,
                LastModified = kvp.Value.UploadedAt
            })
            .OrderBy(d => d.FileName)
            .AsEnumerable();

        return Task.FromResult(documents);
    }

    public Task<DocumentMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(key, out var document))
        {
            return Task.FromResult<DocumentMetadata?>(null);
        }

        var metadata = new DocumentMetadata
        {
            Key = key,
            FileName = ExtractFileNameFromKey(key),
            Size = document.Content.Length,
            ContentType = document.ContentType,
            LastModified = document.UploadedAt
        };

        return Task.FromResult<DocumentMetadata?>(metadata);
    }

    public async Task<string?> UpdateAsync(string key, Stream file, string contentType, CancellationToken cancellationToken = default)
    {
        // Check if document exists
        if (!_files.ContainsKey(key))
        {
            return null;
        }

        // Replace with new content
        using MemoryStream memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        _files[key] = new StoredDocument
        {
            Content = memoryStream.ToArray(),
            ContentType = contentType,
            UploadedAt = DateTime.UtcNow
        };

        return key;
    }

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_files.Remove(key));
    }

    /// <summary>
    /// Extracts the file name from a key in the format "documents/{guid}-{fileName}".
    /// Uses LastIndexOf to find the separator dash (after the UUID which contains dashes).
    /// </summary>
    private static string ExtractFileNameFromKey(string key)
    {
        var parts = key.Split('/');
        if (parts.Length > 1)
        {
            var fileName = parts[^1]; // Get last part after last /
            var lastDashIndex = fileName.LastIndexOf('-');
            if (lastDashIndex > 0 && lastDashIndex < fileName.Length - 1)
            {
                return fileName[(lastDashIndex + 1)..];
            }
        }

        return key;
    }
}

