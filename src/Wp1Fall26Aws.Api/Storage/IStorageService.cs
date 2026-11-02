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

    /// <summary>
    /// Updates (replaces) an existing document with a new file.
    /// </summary>
    /// <param name="key">The document key to update</param>
    /// <param name="file">The new file content</param>
    /// <param name="contentType">The MIME type of the file</param>
    /// <returns>The updated document key, or null if document not found</returns>
    Task<string?> UpdateAsync(string key, Stream file, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing document from storage.
    /// </summary>
    /// <param name="key">The document key to delete</param>
    /// <returns>True if deletion succeeded, false if document not found</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
}
