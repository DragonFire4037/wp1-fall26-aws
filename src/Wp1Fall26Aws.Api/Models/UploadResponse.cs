namespace Wp1Fall26Aws.Api.Models;

/// <summary>
/// Response model for successful document upload.
/// </summary>
public class UploadResponse
{
    /// <summary>
    /// The object key assigned to the uploaded document in storage.
    /// </summary>
    public required string ObjectKey { get; set; }

    /// <summary>
    /// The original file name as provided by the client.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// The MIME type of the uploaded file.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// The size of the uploaded file in bytes.
    /// </summary>
    public long Size { get; set; }
}
