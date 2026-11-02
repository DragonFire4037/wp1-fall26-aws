using Microsoft.AspNetCore.Mvc;
using Wp1Fall26Aws.Api.Models;
using Wp1Fall26Aws.Api.Storage;

namespace Wp1Fall26Aws.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IStorageService _storageService;

    public DocumentsController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// Gets a list of all documents with their metadata.
    /// </summary>
    /// <returns>A list of documents, or an empty list if no documents exist.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentMetadata>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments()
    {
        var documents = await _storageService.ListAsync();
        return Ok(documents);
    }

    /// <summary>
    /// Gets metadata for a specific document by key.
    /// </summary>
    /// <param name="key">The document key (URL encoded)</param>
    /// <returns>The document metadata if found, 404 if not found.</returns>
    [HttpGet("{**key}")]
    [ProducesResponseType(typeof(DocumentMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest("Document key is required.");

        var metadata = await _storageService.GetMetadataAsync(key);

        if (metadata == null)
            return NotFound(new { message = "Document not found.", key });

        return Ok(metadata);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        {
            if (file == null || file.Length == 0) // Check if file was provided
                return BadRequest("A file is required.");
        }

        const long maxFileSize = 5 * 1024 * 1024; // 5 MB

        if (file.Length > maxFileSize) // Check if file exceeds size limit
            return BadRequest("File exceeds the 5 MB size limit.");

        var allowedTypes = new[]
{
            "text/plain", "application/pdf"
        };

        if (!allowedTypes.Contains(file.ContentType)) // Check if file type is allowed
            return BadRequest("File type is not allowed.");

        var objectKey = $"documents/{Guid.NewGuid()}-{file.FileName}";

        await using var stream = file.OpenReadStream();

        var storedKey = await _storageService.UploadAsync(stream, objectKey, file.ContentType);

        return Ok(new
        { objectKey = storedKey, fileName = file.FileName, contentType = file.ContentType, size = file.Length }
        );
    }
}
