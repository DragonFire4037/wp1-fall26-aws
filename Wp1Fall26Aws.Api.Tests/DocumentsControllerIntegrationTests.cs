using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wp1Fall26Aws.Api.Controllers;
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
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.NotNull(okResult.Value);

        // Use reflection to check the anonymous object properties
        var valueObj = okResult.Value!.GetType();
        var objectKeyProp = valueObj.GetProperty("objectKey");
        var fileNameProp = valueObj.GetProperty("fileName");
        var contentTypeProp = valueObj.GetProperty("contentType");
        var sizeProp = valueObj.GetProperty("size");

        Assert.NotNull(objectKeyProp);
        Assert.NotNull(fileNameProp);
        Assert.NotNull(contentTypeProp);
        Assert.NotNull(sizeProp);

        Assert.Equal(fileName, fileNameProp!.GetValue(okResult.Value));
        Assert.Equal(contentType, contentTypeProp!.GetValue(okResult.Value));
        Assert.Equal(fileContent.Length, (long)sizeProp!.GetValue(okResult.Value)!);
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
}


