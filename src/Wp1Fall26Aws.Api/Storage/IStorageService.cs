namespace Wp1Fall26Aws.Api.Storage;

public interface IStorageService
{
    Task<string> UploadAsync(Stream file, string objectKey, string contentType, CancellationToken cancellationToken = default);
}
