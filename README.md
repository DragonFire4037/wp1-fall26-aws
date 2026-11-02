# wp1-fall26-aws

## Document Management API

The application provides a RESTful API for managing documents stored in Amazon S3. All document operations are performed through the `/api/documents` endpoint.

### API Endpoints

| Method | Endpoint | Description | Success | Failure |
|--------|----------|-------------|---------|---------|
| GET | `/api/documents` | Retrieve all documents | 200 | 500 |
| GET | `/api/documents/{key}` | Retrieve document metadata | 200 | 400, 404, 500 |
| POST | `/api/documents` | Upload a document | 201 | 400, 500 |
| PUT | `/api/documents/{key}` | Replace an existing document | 200 | 400, 404, 500 |
| DELETE | `/api/documents/{key}` | Delete a document | 204 | 400, 404, 500 |

### Upload Requirements

When uploading or updating documents via POST or PUT:

**Form Field:**
- Field name: `file` (multipart form field required)
- Expected header: `Content-Type: multipart/form-data`

**Supported MIME Types:**
- `text/plain` (text files)
- `application/pdf` (PDF files)

**File Size Limit:**
- Maximum: 5 MB per file

### API Responses

#### List Documents (GET /api/documents)

**Success (200 OK):**
```json
[
  {
    "key": "documents/550e8400-e29b-41d4-a716-446655440000-example.txt",
    "fileName": "example.txt",
    "size": 1024,
    "contentType": "text/plain",
    "lastModified": "2024-01-15T10:30:00Z"
  }
]
```

#### Get Document Metadata (GET /api/documents/{key})

**Success (200 OK):**
```json
{
  "key": "documents/550e8400-e29b-41d4-a716-446655440000-example.txt",
  "fileName": "example.txt",
  "size": 1024,
  "contentType": "text/plain",
  "lastModified": "2024-01-15T10:30:00Z"
}
```

**Not Found (404):**
```json
{
  "message": "Document not found.",
  "key": "documents/550e8400-e29b-41d4-a716-446655440000-nonexistent.txt"
}
```

#### Upload Document (POST /api/documents)

**Success (201 Created):**
```json
{
  "objectKey": "documents/550e8400-e29b-41d4-a716-446655440000-example.txt",
  "fileName": "example.txt",
  "contentType": "text/plain",
  "size": 1024
}
```

**Bad Request (400):**
- File is required
- File exceeds the 5 MB size limit
- File type is not allowed (must be text/plain or application/pdf)

#### Update Document (PUT /api/documents/{key})

**Success (200 OK):**
```json
{
  "key": "documents/550e8400-e29b-41d4-a716-446655440000-example.txt",
  "fileName": "example.txt",
  "size": 2048,
  "contentType": "text/plain",
  "lastModified": "2024-01-15T11:00:00Z"
}
```

**Not Found (404):**
```json
{
  "message": "Document not found.",
  "key": "documents/550e8400-e29b-41d4-a716-446655440000-nonexistent.txt"
}
```

#### Delete Document (DELETE /api/documents/{key})

**Success (204 No Content):**
- No response body

**Not Found (404):**
```json
{
  "message": "Document not found.",
  "key": "documents/550e8400-e29b-41d4-a716-446655440000-nonexistent.txt"
}
```

## Running Locally

### Prerequisites

- .NET 10 SDK or later
- AWS credentials configured (via SSO or IAM)
- Visual Studio 2026 or another C# IDE

### Build and Run

1. Clone the repository:
   ```bash
   git clone https://github.com/DragonFire4037/wp1-fall26-aws.git
   cd wp1-fall26-aws
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the API:
   ```bash
   dotnet run --project src/Wp1Fall26Aws.Api
   ```

4. The API will start on `http://localhost:5000` (or the configured port).

### API Documentation

During development, access the OpenAPI (Swagger) specification:
- **Swagger UI:** `http://localhost:5000/swagger`
- **OpenAPI JSON:** `http://localhost:5000/openapi/v1.json`

This interactive documentation allows you to test endpoints directly in the browser.

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
- All four CRUD operations (CREATE, READ, UPDATE, DELETE)
- Edge cases (nonexistent documents, empty keys, invalid content types)

These tests use `FakeStorageService` and do not require AWS access.

Run tests with:
```bash
dotnet test
```

### Troubleshooting

If the application reports an AWS credential error, verify that the institutional AWS SSO session is active.

If S3 reports that the bucket does not exist, verify that the configured bucket name and AWS Region match the instructor-provided resources.
