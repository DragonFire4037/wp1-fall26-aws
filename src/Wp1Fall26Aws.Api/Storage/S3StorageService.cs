using Amazon.S3;
using Amazon.S3.Model;
using Wp1Fall26Aws.Api.Models;

namespace Wp1Fall26Aws.Api.Storage;

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
        PutObjectRequest request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = file,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task<IEnumerable<DocumentMetadata>> ListAsync(CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = _prefix,
            MaxKeys = 1000 // Limit to 1000 objects per call (adjust as needed)
        };

        var documents = new List<DocumentMetadata>();

        ListObjectsV2Response response;
        do
        {
            response = await _s3Client.ListObjectsV2Async(request, cancellationToken);

            foreach (var s3Object in response.S3Objects)
            {
                documents.Add(new DocumentMetadata
                {
                    Key = s3Object.Key,
                    FileName = ExtractFileNameFromKey(s3Object.Key),
                    Size = s3Object.Size ?? 0,
                    ContentType = await GetContentTypeAsync(s3Object.Key, cancellationToken),
                    LastModified = s3Object.LastModified ?? DateTime.UtcNow
                });
            }

            request.ContinuationToken = response.ContinuationToken;
        } while (response.IsTruncated ?? false);

        return documents.OrderBy(d => d.FileName);
    }

    public async Task<DocumentMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            return new DocumentMetadata
            {
                Key = key,
                FileName = ExtractFileNameFromKey(key),
                Size = response.ContentLength,
                ContentType = response.Headers.ContentType ?? "application/octet-stream",
                LastModified = response.LastModified ?? DateTime.UtcNow
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<string?> UpdateAsync(string key, Stream file, string contentType, CancellationToken cancellationToken = default)
    {
        // First check if the document exists
        var existingMetadata = await GetMetadataAsync(key, cancellationToken);
        if (existingMetadata == null)
        {
            return null;
        }

        // Replace the object with new content
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = file,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        return key;
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // First check if the document exists
            var metadata = await GetMetadataAsync(key, cancellationToken);
            if (metadata == null)
            {
                return false;
            }

            // Delete the object
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the file name from a key in the format "documents/{guid}-{fileName}".
    /// Uses LastIndexOf to find the separator dash (after the UUID which contains dashes).
    /// </summary>
    private static string ExtractFileNameFromKey(string key)
    {
        var parts = key.Split('/');
        if (parts.Length > 1)
        {
            var fileName = parts[^1]; // Get last part after last /
            var lastDashIndex = fileName.LastIndexOf('-');
            if (lastDashIndex > 0 && lastDashIndex < fileName.Length - 1)
            {
                return fileName[(lastDashIndex + 1)..];
            }
        }

        return key;
    }

    /// <summary>
    /// Gets the ContentType for an S3 object by querying its metadata.
    /// </summary>
    private async Task<string> GetContentTypeAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return response.Headers.ContentType ?? "application/octet-stream";
        }
        catch
        {
            return "application/octet-stream";
        }
    }
}
