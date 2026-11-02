using Microsoft.AspNetCore.Mvc;
using Wp1Fall26Aws.Api.Controllers;
using Wp1Fall26Aws.Api.Models;
using Wp1Fall26Aws.Api.Storage;

namespace Wp1Fall26Aws.Api.Tests;

/// <summary>
/// Tests for DocumentsController GET operations (List and GetMetadata).
/// </summary>
public class DocumentsControllerGetTests
{
    /// <summary>
    /// Test that GetDocuments returns an empty list when no documents exist.
    /// </summary>
    [Fact]
    public async Task GetDocuments_WithNoDocuments_ReturnsEmptyList()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // Act
        var result = await controller.GetDocuments();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var documents = Assert.IsAssignableFrom<IEnumerable<DocumentMetadata>>(okResult.Value);
        Assert.Empty(documents);
    }

    /// <summary>
    /// Test that GetDocuments returns documents with correct metadata after upload.
    /// </summary>
    [Fact]
    public async Task GetDocuments_AfterUpload_ReturnsDocumentList()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);
        var fileName = "test.txt";
        var fileContent = "This is a test document";
        var contentType = "text/plain";

        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var objectKey = $"documents/{Guid.NewGuid()}-{fileName}";
        await storage.UploadAsync(stream, objectKey, contentType);

        // Act
        var result = await controller.GetDocuments();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var documents = Assert.IsAssignableFrom<IEnumerable<DocumentMetadata>>(okResult.Value);
        var documentList = documents.ToList();

        Assert.Single(documentList);
        Assert.Equal(fileName, documentList[0].FileName);
        Assert.Equal(objectKey, documentList[0].Key);
        Assert.Equal(contentType, documentList[0].ContentType);
        Assert.Equal(fileContent.Length, documentList[0].Size);
    }

    /// <summary>
    /// Test that GetDocuments returns multiple documents sorted by filename.
    /// </summary>
    [Fact]
    public async Task GetDocuments_WithMultipleDocuments_ReturnsSortedList()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);
        var files = new[] { "zebra.txt", "apple.txt", "monkey.pdf" };

        foreach (var fileName in files)
        {
            var contentType = fileName.EndsWith(".pdf") ? "application/pdf" : "text/plain";
            await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Content of {fileName}"));
            var objectKey = $"documents/{Guid.NewGuid()}-{fileName}";
            await storage.UploadAsync(stream, objectKey, contentType);
        }

        // Act
        var result = await controller.GetDocuments();

        // Assert
        var okResult = (OkObjectResult)result;
        var documents = Assert.IsAssignableFrom<IEnumerable<DocumentMetadata>>(okResult.Value).ToList();

        Assert.Equal(3, documents.Count);
        // Verify sorted by filename
        Assert.Equal("apple.txt", documents[0].FileName);
        Assert.Equal("monkey.pdf", documents[1].FileName);
        Assert.Equal("zebra.txt", documents[2].FileName);
    }

    /// <summary>
    /// Test that GetDocument returns metadata for an existing document.
    /// </summary>
    [Fact]
    public async Task GetDocument_WithExistingKey_ReturnsMetadata()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);
        var fileName = "report.pdf";
        var contentType = "application/pdf";
        var fileContent = "PDF report content";

        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var objectKey = $"documents/{Guid.NewGuid()}-{fileName}";
        await storage.UploadAsync(stream, objectKey, contentType);

        // Act
        var result = await controller.GetDocument(objectKey);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var metadata = Assert.IsType<DocumentMetadata>(okResult.Value);

        Assert.Equal(objectKey, metadata.Key);
        Assert.Equal(fileName, metadata.FileName);
        Assert.Equal(contentType, metadata.ContentType);
        Assert.Equal(fileContent.Length, metadata.Size);
    }

    /// <summary>
    /// Test that GetDocument returns 404 for non-existent document key.
    /// </summary>
    [Fact]
    public async Task GetDocument_WithNonExistentKey_Returns404()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);
        var nonExistentKey = "documents/00000000-0000-0000-0000-000000000000-nonexistent.txt";

        // Act
        var result = await controller.GetDocument(nonExistentKey);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        var notFoundResult = (NotFoundObjectResult)result;
        var errorResponse = notFoundResult.Value;
        Assert.NotNull(errorResponse);
    }

    /// <summary>
    /// Test that GetDocument returns 400 BadRequest when key is empty or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDocument_WithEmptyOrWhitespaceKey_ReturnsBadRequest(string? key)
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);

        // Act
        var result = await controller.GetDocument(key ?? "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test that document metadata is consistent after upload and retrieval.
    /// </summary>
    [Fact]
    public async Task GetDocument_AfterUpload_HasConsistentMetadata()
    {
        // Arrange
        var storage = new FakeStorageService();
        var controller = new DocumentsController(storage);
        var fileName = "invoice.pdf";
        var contentType = "application/pdf";
        var fileContent = "Invoice document content";

        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var objectKey = $"documents/{Guid.NewGuid()}-{fileName}";
        await storage.UploadAsync(stream, objectKey, contentType);

        // Act - Get via GetDocuments
        var listResult = await controller.GetDocuments();
        var listOkResult = (OkObjectResult)listResult;
        var listDocuments = Assert.IsAssignableFrom<IEnumerable<DocumentMetadata>>(listOkResult.Value).ToList();

        // Act - Get via GetDocument
        var getResult = await controller.GetDocument(objectKey);
        var getOkResult = (OkObjectResult)getResult;
        var getMetadata = Assert.IsType<DocumentMetadata>(getOkResult.Value);

        // Assert - Both should have matching metadata
        var listedDocument = listDocuments.FirstOrDefault(d => d.Key == objectKey);
        Assert.NotNull(listedDocument);
        Assert.Equal(getMetadata.Key, listedDocument.Key);
        Assert.Equal(getMetadata.FileName, listedDocument.FileName);
        Assert.Equal(getMetadata.Size, listedDocument.Size);
        Assert.Equal(getMetadata.ContentType, listedDocument.ContentType);
    }
}
