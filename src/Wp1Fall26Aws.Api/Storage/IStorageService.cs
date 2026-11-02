using Wp1Fall26Aws.Api.Models;

namespace Wp1Fall26Aws.Api.Storage;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file to storage and returns the object key.
    /// </summary>
    Task<string> UploadAsync(Stream file, string objectKey, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all documents in storage with their metadata.
    /// </summary>
    /// <returns>A list of DocumentMetadata objects. Returns empty list if no documents exist.</returns>
    Task<IEnumerable<DocumentMetadata>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific document by key.
    /// </summary>
    /// <param name="key">The document key/path</param>
    /// <returns>The document metadata, or null if not found</returns>
    Task<DocumentMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default);
}
