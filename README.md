# wp1-fall26-aws

## Storage

Uploads are handled through the `IStorageService` abstraction.

The current implementation uses Amazon S3 through `S3StorageService`.

### Upload behavior

- Maximum file size: 5 MB
- Allowed MIME types:
  - `text/plain`
  - `application/pdf`
- Objects are stored under the `documents/` prefix.
- Uploaded objects use a generated GUID in their object key.
- The application uses the institution-provided AWS SSO/IAM role.
- No long-lived AWS access keys are stored in the application or repository.

### Testing

Unit tests cover:
- Rejection of unsupported MIME types
- Rejection of files larger than 5 MB

These tests use `FakeStorageService` and do not require AWS access.

### Troubleshooting

If the application reports an AWS credential error, verify that the institutional AWS SSO session is active.

If S3 reports that the bucket does not exist, verify that the configured bucket name and AWS Region match the instructor-provided resources.