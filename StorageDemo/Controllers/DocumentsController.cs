using Microsoft.AspNetCore.Mvc;
using StorageDemo.Storage;

namespace StorageDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IStorageService _storageService;

    public DocumentsController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost]
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
        {objectKey = storedKey, fileName = file.FileName, contentType = file.ContentType, size = file.Length}
        );
    }
}