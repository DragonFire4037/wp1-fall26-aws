using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StorageDemo.Controllers;
using StorageDemo.Storage;

namespace StorageDemo.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Upload_RejectsUnsupportedFileType()
    {
        FakeStorageService storage = new FakeStorageService();
        DocumentsController controller = new DocumentsController(storage);
        MemoryStream content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
        FormFile file = new FormFile(content, 0, content.Length, "file", "test.cs");
        
        file.Headers = new HeaderDictionary{["ContentType"] = "application/octet-stream"};
        var result = await controller.Upload(file);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_RejectsFileOverSizeLimit()
    {
        FakeStorageService storage = new FakeStorageService();
        DocumentsController controller = new DocumentsController(storage);
        MemoryStream content = new MemoryStream(new byte[5*1024*1024 + 1]); // 5 MB
        FormFile file = new FormFile(content, 0, content.Length, "file", "large-test.txt");
        file.Headers = new HeaderDictionary{["Content-Type"] = "text/plain"};
        var result = await controller.Upload(file);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
