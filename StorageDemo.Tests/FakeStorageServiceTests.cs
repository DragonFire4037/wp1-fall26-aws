using Wp1Fall26Aws.Api.Storage;

namespace StorageDemo.Tests;

/// <summary>
/// Unit tests for FakeStorageService demonstrating Arrange/Act/Assert pattern.
/// These tests verify storage operations without requiring an S3 connection.
/// </summary>
public class FakeStorageServiceTests
{
    /// <summary>
    /// Test that UploadAsync successfully stores a file and returns the object key.
    /// Arrange: Create a storage service and a test file stream
    /// Act: Call UploadAsync with the file
    /// Assert: Verify the returned key matches the input key
    /// </summary>
    [Fact]
    public async Task UploadAsync_WithValidFile_ReturnsObjectKey()
    {
        // Arrange
        var storage = new FakeStorageService();
        var fileContent = "This is test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var objectKey = "documents/test-file-123.txt";
        var contentType = "text/plain";

        // Act
        var result = await storage.UploadAsync(stream, objectKey, contentType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(objectKey, result);
    }

    /// <summary>
    /// Test that UploadAsync can handle multiple files and they are stored independently.
    /// Arrange: Create a storage service and upload multiple files
    /// Act: Upload two different files with different object keys
    /// Assert: Verify both uploads succeed and return their respective keys
    /// </summary>
    [Fact]
    public async Task UploadAsync_WithMultipleFiles_StoresEachIndependently()
    {
        // Arrange
        var storage = new FakeStorageService();
        var file1Content = "First file content";
        var file2Content = "Second file content";
        var stream1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(file1Content));
        var stream2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(file2Content));
        var objectKey1 = "documents/file-1.txt";
        var objectKey2 = "documents/file-2.txt";
        var contentType = "text/plain";

        // Act
        var result1 = await storage.UploadAsync(stream1, objectKey1, contentType);
        var result2 = await storage.UploadAsync(stream2, objectKey2, contentType);

        // Assert
        Assert.Equal(objectKey1, result1);
        Assert.Equal(objectKey2, result2);
        Assert.NotEqual(result1, result2);
    }
}
