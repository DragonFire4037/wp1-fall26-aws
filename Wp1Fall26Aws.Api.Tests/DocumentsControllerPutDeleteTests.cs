using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wp1Fall26Aws.Api.Controllers;
using Wp1Fall26Aws.Api.Models;
using Wp1Fall26Aws.Api.Storage;
using Xunit;

namespace Wp1Fall26Aws.Api.Tests;

/// <summary>
/// Tests for DocumentsController PUT and DELETE operations.
/// </summary>
public class DocumentsControllerPutDeleteTests
{
    private static string GetObjectKeyFromUploadResponse(OkObjectResult uploadResult)
    {
        var value = uploadResult!.Value;
        if (value == null)
            throw new NullReferenceException("Upload response value is null");

        var property = value.GetType().GetProperty("objectKey");
        if (property == null)
        {
            // Debug: print all properties
            var props = string.Join(", ", value.GetType().GetProperties().Select(p => p.Name));
            throw new InvalidOperationException($"Property 'objectKey' not found. Available properties: {props}");
        }

        var keyValue = property.GetValue(value);
        if (keyValue == null)
            throw new NullReferenceException("objectKey property is null");

        return (string)keyValue;
    }

    #region PUT Tests

    [Fact]
    public async Task UpdateDocument_WithExistingKey_ReturnsUpdatedMetadata()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // Upload initial document
        var initialFileName = "test.txt";
        var initialContent = "Initial content";
        using var initialStream = new MemoryStream(Encoding.UTF8.GetBytes(initialContent));
        var initialFile = new FormFile(initialStream, 0, initialStream.Length, "file", initialFileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var uploadResult = await controller.Upload(initialFile);
        var uploadOkResult = Assert.IsType<OkObjectResult>(uploadResult);
        string objectKey = GetObjectKeyFromUploadResponse(uploadOkResult);

        // Act: Update the document
        var newContent = "Updated content - longer and different";
        using var updateStream = new MemoryStream(Encoding.UTF8.GetBytes(newContent));
        var updateFile = new FormFile(updateStream, 0, updateStream.Length, "file", "test-updated.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var updateResult = await controller.UpdateDocument(objectKey, updateFile);

        // Assert
        Assert.IsType<OkObjectResult>(updateResult);
        var okResult = (OkObjectResult)updateResult;
        var metadata = Assert.IsType<DocumentMetadata>(okResult.Value);
        Assert.Equal(objectKey, metadata.Key);
        Assert.Equal("text/plain", metadata.ContentType);
        Assert.Equal(newContent.Length, metadata.Size);
    }

    [Fact]
    public async Task UpdateDocument_WithNonexistentKey_Returns404()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);
        var fakeKey = "documents/00000000-0000-0000-0000-000000000000-nonexistent.txt";

        var content = "Some content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", "test.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Act
        var result = await controller.UpdateDocument(fakeKey, file);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDocument_WithInvalidContentType_Returns400()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // Upload initial document
        var fileName = "test.txt";
        var content = "Initial content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var uploadResult = await controller.Upload(file);
        var uploadOkResult = Assert.IsType<OkObjectResult>(uploadResult);
        string objectKey = GetObjectKeyFromUploadResponse(uploadOkResult);

        // Act: Try to update with invalid content type
        var invalidContent = "Invalid file content";
        using var invalidStream = new MemoryStream(Encoding.UTF8.GetBytes(invalidContent));
        var invalidFile = new FormFile(invalidStream, 0, invalidStream.Length, "file", "test.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream" // Not allowed
        };

        var result = await controller.UpdateDocument(objectKey, invalidFile);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDocument_WithOversizedFile_Returns400()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // Upload initial document
        var fileName = "test.txt";
        var content = "Initial content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var uploadResult = await controller.Upload(file);
        var uploadOkResult = Assert.IsType<OkObjectResult>(uploadResult);
        string objectKey = GetObjectKeyFromUploadResponse(uploadOkResult);

        // Act: Try to update with oversized file
        var oversizedContent = new byte[6 * 1024 * 1024]; // 6 MB
        using var oversizedStream = new MemoryStream(oversizedContent);
        var oversizedFile = new FormFile(oversizedStream, 0, oversizedStream.Length, "file", "oversized.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var result = await controller.UpdateDocument(objectKey, oversizedFile);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDocument_WithEmptyKey_Returns400()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        var content = "Some content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", "test.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Act
        var result = await controller.UpdateDocument(string.Empty, file);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDocument_WithNullFile_Returns400()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);
        var objectKey = "documents/some-key.txt";

        // Act
        var result = await controller.UpdateDocument(objectKey, null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task DeleteDocument_WithExistingKey_Returns204()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // Upload a document first
        var fileName = "test.txt";
        var content = "Content to delete";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var uploadResult = await controller.Upload(file);
        var uploadOkResult = Assert.IsType<OkObjectResult>(uploadResult);
        string objectKey = GetObjectKeyFromUploadResponse(uploadOkResult);

        // Verify document exists
        var getResult = await controller.GetDocument(objectKey);
        Assert.IsType<OkObjectResult>(getResult);

        // Act: Delete the document
        var deleteResult = await controller.DeleteDocument(objectKey);

        // Assert
        Assert.IsType<NoContentResult>(deleteResult);

        // Verify document is deleted
        var getAfterDeleteResult = await controller.GetDocument(objectKey);
        Assert.IsType<NotFoundObjectResult>(getAfterDeleteResult);
    }

    [Fact]
    public async Task DeleteDocument_WithNonexistentKey_Returns404()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);
        var fakeKey = "documents/00000000-0000-0000-0000-000000000000-nonexistent.txt";

        // Act
        var result = await controller.DeleteDocument(fakeKey);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDocument_WithEmptyKey_Returns400()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // Act
        var result = await controller.DeleteDocument(string.Empty);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Complete CRUD Workflow Tests

    [Fact]
    public async Task CompleteCrudWorkflow_PostGetPutDeleteSequence_SucceedsIndependently()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // 1. POST: Create a document
        var fileName = "crud-test.txt";
        var initialContent = "Initial CRUD test content";
        using var createStream = new MemoryStream(Encoding.UTF8.GetBytes(initialContent));
        var createFile = new FormFile(createStream, 0, createStream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var createResult = await controller.Upload(createFile);
        Assert.IsType<OkObjectResult>(createResult);
        var createOkResult = (OkObjectResult)createResult;
        string objectKey = GetObjectKeyFromUploadResponse(createOkResult);

        // 2. GET: Verify document exists
        var getResult = await controller.GetDocument(objectKey);
        Assert.IsType<OkObjectResult>(getResult);
        var getOkResult = (OkObjectResult)getResult;
        var metadata = Assert.IsType<DocumentMetadata>(getOkResult.Value);
        Assert.Equal(objectKey, metadata.Key);
        Assert.Equal(initialContent.Length, metadata.Size);

        // 3. PUT: Update the document
        var updatedContent = "Updated CRUD test content with more data";
        using var updateStream = new MemoryStream(Encoding.UTF8.GetBytes(updatedContent));
        var updateFile = new FormFile(updateStream, 0, updateStream.Length, "file", "crud-test-updated.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var updateResult = await controller.UpdateDocument(objectKey, updateFile);
        Assert.IsType<OkObjectResult>(updateResult);
        var updateOkResult = (OkObjectResult)updateResult;
        var updatedMetadata = Assert.IsType<DocumentMetadata>(updateOkResult.Value);
        Assert.Equal(updatedContent.Length, updatedMetadata.Size);

        // 4. GET after PUT: Verify update
        var getAfterUpdateResult = await controller.GetDocument(objectKey);
        Assert.IsType<OkObjectResult>(getAfterUpdateResult);

        // 5. DELETE: Remove the document
        var deleteResult = await controller.DeleteDocument(objectKey);
        Assert.IsType<NoContentResult>(deleteResult);

        // 6. GET after DELETE: Verify deletion
        var getAfterDeleteResult = await controller.GetDocument(objectKey);
        Assert.IsType<NotFoundObjectResult>(getAfterDeleteResult);
    }

    [Fact]
    public async Task MultipleDocuments_PostUpdateDeleteIndependently_AllOperationsSucceed()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // Upload multiple documents
        var keys = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var fileName = $"doc{i}.txt";
            var content = $"Document {i} content";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var file = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var uploadResult = await controller.Upload(file);
            var uploadOkResult = Assert.IsType<OkObjectResult>(uploadResult);
            keys.Add(GetObjectKeyFromUploadResponse(uploadOkResult));
        }

        // Update the first document
        var updateContent = "Updated document 0";
        using var updateStream = new MemoryStream(Encoding.UTF8.GetBytes(updateContent));
        var updateFile = new FormFile(updateStream, 0, updateStream.Length, "file", "doc0-updated.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var putResult = await controller.UpdateDocument(keys[0], updateFile);
        Assert.IsType<OkObjectResult>(putResult);

        // Delete the second document
        var deleteResult = await controller.DeleteDocument(keys[1]);
        Assert.IsType<NoContentResult>(deleteResult);

        // Verify: First should exist with updated metadata
        var getFirst = await controller.GetDocument(keys[0]);
        Assert.IsType<OkObjectResult>(getFirst);

        // Verify: Second should be gone
        var getSecond = await controller.GetDocument(keys[1]);
        Assert.IsType<NotFoundObjectResult>(getSecond);

        // Verify: Third should still exist
        var getThird = await controller.GetDocument(keys[2]);
        Assert.IsType<OkObjectResult>(getThird);
    }

    #endregion
}
