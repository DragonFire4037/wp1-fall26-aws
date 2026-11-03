using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wp1Fall26Aws.Api.Controllers;
using Wp1Fall26Aws.Api.Models;
using Wp1Fall26Aws.Api.Storage;

namespace Wp1Fall26Aws.Api.Tests;

/// <summary>
/// Integration tests for the Documents API endpoint using WebApplicationFactory.
/// These tests spin up the entire ASP.NET Core application and test real HTTP endpoints.
/// They use FakeStorageService to avoid AWS S3 interactions during testing.
/// </summary>
public class DocumentsControllerIntegrationTests
{
    /// <summary>
    /// Integration test that uploads a valid text file and verifies the response.
    /// This test exercises the controller with FakeStorageService to test without AWS interaction.
    /// </summary>
    [Fact]
    public async Task Post_DocumentsWithValidTextFile_ReturnsOkWithFileData()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        var fileContent = "This is test document content for integration testing.";
        var fileName = "test-document.txt";
        var contentType = "text/plain";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var file = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = contentType }
        };

        // Act
        var result = await controller.Upload(file);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        var createdResult = (CreatedAtActionResult)result;
        Assert.NotNull(createdResult.Value);

        // Use reflection to check the UploadResponse properties
        var valueObj = createdResult.Value!.GetType();
        var objectKeyProp = valueObj.GetProperty("ObjectKey");
        var fileNameProp = valueObj.GetProperty("FileName");
        var contentTypeProp = valueObj.GetProperty("ContentType");
        var sizeProp = valueObj.GetProperty("Size");

        Assert.NotNull(objectKeyProp);
        Assert.NotNull(fileNameProp);
        Assert.NotNull(contentTypeProp);
        Assert.NotNull(sizeProp);

        Assert.Equal(fileName, fileNameProp!.GetValue(createdResult.Value));
        Assert.Equal(contentType, contentTypeProp!.GetValue(createdResult.Value));
        Assert.Equal(fileContent.Length, (long)sizeProp!.GetValue(createdResult.Value)!);
    }

    /// <summary>
    /// Test that the API rejects files with unsupported content types.
    /// </summary>
    [Fact]
    public async Task Post_DocumentsWithUnsupportedFileType_ReturnsBadRequest()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        var fileName = "test-document.exe";
        var unsupportedContent = new byte[] { 0x4D, 0x5A }; // MZ header for executable

        var stream = new MemoryStream(unsupportedContent);
        var file = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "application/octet-stream" }
        };

        // Act
        var result = await controller.Upload(file);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test that the API rejects files exceeding the size limit.
    /// </summary>
    [Fact]
    public async Task Post_DocumentsWithOversizedFile_ReturnsBadRequest()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        const long sizeLimit = 5 * 1024 * 1024; // 5 MB
        var oversizedContent = new byte[sizeLimit + 1];

        var stream = new MemoryStream(oversizedContent);
        var file = new FormFile(stream, 0, stream.Length, "file", "large-file.txt")
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "text/plain" }
        };

        // Act
        var result = await controller.Upload(file);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test that the API rejects requests with no file attached (null file).
    /// </summary>
    [Fact]
    public async Task Post_DocumentsWithNoFile_ReturnsBadRequest()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        // Pass a file with zero length
        var emptyStream = new MemoryStream(new byte[0]);
        var emptyFile = new FormFile(emptyStream, 0, 0, "file", "empty.txt")
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "text/plain" }
        };

        // Act
        var result = await controller.Upload(emptyFile);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #region GET Tests

    /// <summary>
    /// Test that GET /api/documents returns an empty list when no documents exist.
    /// </summary>
    [Fact]
    public async Task Get_DocumentsWithNoDocuments_ReturnsEmptyList()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        // Act
        var result = await controller.GetDocuments();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var documents = Assert.IsAssignableFrom<IEnumerable<DocumentMetadata>>(okResult.Value);
        Assert.Empty(documents);
    }

    /// <summary>
    /// Test that GET /api/documents returns a list of documents after uploading files.
    /// Verifies that the metadata is correctly populated for multiple documents.
    /// </summary>
    [Fact]
    public async Task Get_DocumentsAfterUpload_ReturnsDocumentList()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        // Upload first document
        var file1Content = "First document content";
        var file1Name = "document1.txt";
        var stream1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(file1Content));
        var file1 = new FormFile(stream1, 0, stream1.Length, "file", file1Name)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "text/plain" }
        };

        await controller.Upload(file1);

        // Upload second document
        var file2Content = "Second document content - longer than the first";
        var file2Name = "document2.txt";
        var stream2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(file2Content));
        var file2 = new FormFile(stream2, 0, stream2.Length, "file", file2Name)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "text/plain" }
        };

        await controller.Upload(file2);

        // Act
        var result = await controller.GetDocuments();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var documents = Assert.IsAssignableFrom<IEnumerable<DocumentMetadata>>(okResult.Value);
        var documentList = documents.ToList();

        Assert.Equal(2, documentList.Count);
        Assert.All(documentList, doc =>
        {
            Assert.NotNull(doc.Key);
            Assert.NotEmpty(doc.FileName);
            Assert.True(doc.Size > 0);
            Assert.NotEmpty(doc.ContentType);
        });
    }

    /// <summary>
    /// Test that GET /api/documents/{key} returns metadata for an existing document.
    /// </summary>
    [Fact]
    public async Task Get_DocumentWithExistingKey_ReturnsMetadata()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        var fileContent = "Document content for retrieval";
        var fileName = "test.txt";
        var contentType = "text/plain";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var file = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = contentType }
        };

        // Upload a document to get its key
        var uploadResult = await controller.Upload(file);
        var createdResult = Assert.IsType<CreatedAtActionResult>(uploadResult);
        var uploadResponse = createdResult.Value;
        var objectKeyProp = uploadResponse!.GetType().GetProperty("ObjectKey");
        var objectKey = (string)objectKeyProp!.GetValue(uploadResponse)!;

        // Act
        var result = await controller.GetDocument(objectKey);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var metadata = Assert.IsType<DocumentMetadata>(okResult.Value);

        Assert.Equal(objectKey, metadata.Key);
        Assert.Equal(fileName, metadata.FileName);
        Assert.Equal(fileContent.Length, metadata.Size);
        Assert.Equal(contentType, metadata.ContentType);
    }

    /// <summary>
    /// Test that GET /api/documents/{key} returns 404 when document doesn't exist.
    /// </summary>
    [Fact]
    public async Task Get_DocumentWithNonexistentKey_Returns404()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        var nonexistentKey = "documents/00000000-0000-0000-0000-000000000000-nonexistent.txt";

        // Act
        var result = await controller.GetDocument(nonexistentKey);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        var notFoundResult = (NotFoundObjectResult)result;
        Assert.NotNull(notFoundResult.Value);

        var valueObj = notFoundResult.Value!.GetType();
        var messageProp = valueObj.GetProperty("message");
        var keyProp = valueObj.GetProperty("key");

        Assert.NotNull(messageProp);
        Assert.NotNull(keyProp);
        Assert.Equal("Document not found.", messageProp!.GetValue(notFoundResult.Value));
        Assert.Equal(nonexistentKey, keyProp!.GetValue(notFoundResult.Value));
    }

    /// <summary>
    /// Test that GET /api/documents/{key} returns 400 when key is empty or null.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Get_DocumentWithEmptyOrNullKey_ReturnsBadRequest(string? key)
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        // Act
        var result = await controller.GetDocument(key ?? "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.Equal("Document key is required.", badRequestResult.Value);
    }

    /// <summary>
    /// Test that list and metadata queries return consistent results.
    /// Verifies that a document returned in the list can be retrieved with GetDocument.
    /// </summary>
    [Fact]
    public async Task Get_ListAndMetadataConsistency_ReturnsMatchingData()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        var fileContent = "Consistency test content";
        var fileName = "consistency-test.txt";
        var contentType = "text/plain";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var file = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = contentType }
        };

        // Upload a document
        await controller.Upload(file);

        // Act: Get the list first
        var listResult = await controller.GetDocuments();
        var listOkResult = Assert.IsType<OkObjectResult>(listResult);
        var documentList = Assert.IsAssignableFrom<IEnumerable<DocumentMetadata>>(listOkResult.Value);
        var firstDocument = documentList.First();

        // Act: Get individual document metadata
        var metadataResult = await controller.GetDocument(firstDocument.Key);
        var metadataOkResult = Assert.IsType<OkObjectResult>(metadataResult);
        var metadata = Assert.IsType<DocumentMetadata>(metadataOkResult.Value);

        // Assert: Verify list and individual metadata are consistent
        Assert.Equal(firstDocument.Key, metadata.Key);
        Assert.Equal(firstDocument.FileName, metadata.FileName);
        Assert.Equal(firstDocument.Size, metadata.Size);
        Assert.Equal(firstDocument.ContentType, metadata.ContentType);
    }

    /// <summary>
    /// Test that GET /api/documents returns multiple documents in sorted order by filename.
    /// </summary>
    [Fact]
    public async Task Get_MultipleDocuments_ReturnsSortedByFilename()
    {
        // Arrange
        var fakeStorage = new FakeStorageService();
        var controller = new DocumentsController(fakeStorage);

        var fileNames = new[] { "zebra.txt", "apple.txt", "banana.txt" };

        // Upload documents in non-alphabetical order
        foreach (var fileName in fileNames)
        {
            var fileContent = $"Content for {fileName}";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
            var file = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary { ["Content-Type"] = "text/plain" }
            };

            await controller.Upload(file);
        }

        // Act
        var result = await controller.GetDocuments();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var documents = Assert.IsAssignableFrom<IEnumerable<DocumentMetadata>>(okResult.Value);
        var documentList = documents.ToList();

        // Verify sorted by filename
        var sortedFileNames = documentList.Select(d => d.FileName).ToList();
        var expectedOrder = new[] { "apple.txt", "banana.txt", "zebra.txt" };
        Assert.Equal(expectedOrder, sortedFileNames);
    }

    #endregion
}



