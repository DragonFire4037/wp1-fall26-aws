namespace Wp1Fall26Aws.Api.Models;

/// <summary>
/// Represents metadata for a stored document.
/// </summary>
public class DocumentMetadata
{
    /// <summary>
    /// The unique key/path of the document in storage.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The original file name provided during upload.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// The size of the document in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The MIME type of the document.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// The date and time when the document was last modified (uploaded).
    /// </summary>
    public DateTime LastModified { get; set; }
}
