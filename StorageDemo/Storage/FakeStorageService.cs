namespace StorageDemo.Storage;

public class FakeStorageService : IStorageService
{
    private readonly Dictionary<string, byte[]> _files = new();

    public async Task<string> UploadAsync( Stream file, string objectKey, string contentType, CancellationToken cancellationToken = default)
{
    using MemoryStream memoryStream = new MemoryStream();

    await file.CopyToAsync(memoryStream, cancellationToken);
    _files[objectKey] = memoryStream.ToArray();
    return objectKey;
}
}