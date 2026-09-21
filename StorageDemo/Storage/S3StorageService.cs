using Amazon.S3;
using Amazon.S3.Model;

namespace StorageDemo.Storage;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _prefix;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:BucketName"]
            ?? throw new InvalidOperationException("AWS bucket name is not configured.");

        _prefix = configuration["AWS:Prefix"] ?? "documents/";
    }

    public async Task<string> UploadAsync(Stream file, string objectKey, string contentType, CancellationToken cancellationToken = default)
    {
        string key = objectKey;
        PutObjectRequest request = new PutObjectRequest{BucketName = _bucketName, Key = key, InputStream = file, ContentType = contentType};

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return key;
    }
}