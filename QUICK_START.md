# Quick Start Guide - Static Document Management Client

## Prerequisites
- ✅ .NET 10 SDK installed
- ✅ Python 3 installed
- ✅ Modern web browser

## Run Locally (5 minutes)

### Terminal 1: Start the API
```powershell
cd C:\Users\natha\Documents\GitHub\wp1-fall26-aws
dotnet run --project src/Wp1Fall26Aws.Api/Wp1Fall26Aws.Api.csproj
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
	  Now listening on: http://localhost:5000
```

### Terminal 2: Start the Static Server
```powershell
cd C:\Users\natha\Documents\GitHub\wp1-fall26-aws\consumer
python -m http.server 8000
```

**Expected Output:**
```
Serving HTTP on 0.0.0.0 port 8000 (http://localhost:8000/) ...
```

### Browser: Access the Application
1. Open: `http://localhost:8000`
2. Confirm API status shows "● Connected" (green)
3. Start uploading documents!

---

## Test Workflow

### Step 1: Upload a Document
1. Click the upload area or drag a .txt or .pdf file
2. Choose a file (max 5 MB)
3. Click "Upload"
4. Success message appears: "Document uploaded successfully"

### Step 2: View Documents
- Table auto-updates every 5 seconds
- Or click "Refresh" button for immediate update
- Shows: File Name, Size, Content Type, Last Modified

### Step 3: View Details
1. Click "View" button next to any document
2. Document Details panel appears below
3. Shows full metadata and Object Key
4. Click copy button (📋) to copy key to clipboard

### Step 4: Replace a Document
1. Select a document (click "View")
2. In "Replace File" section, choose new file
3. Click "Replace File"
4. Document updated with new content

### Step 5: Delete a Document  
1. Select a document (click "View")
2. Click "Delete Document"
3. Confirm deletion
4. Document removed from list

### Step 6: Close Details
- Click "Close" button
- Or upload/replace/delete automatically closes it

---

## Troubleshooting

### "API Disconnected" Status
**Problem:** Red "● Disconnected" indicator  
**Solution:**
1. Ensure API is running on Terminal 1
2. Check API URL in `consumer/app.js` line 3
3. Verify firewall allows localhost:5000

### "File size exceeds 5 MB limit"
**Problem:** Upload fails for larger files  
**Solution:** Files must be ≤ 5 MB (API requirement)

### "Only TXT and PDF files are allowed"
**Problem:** Can't upload other file types  
**Solution:** Use .txt or .pdf files only

### Port Already in Use
**Problem:** "Address already in use" error  
**Solution:** Change port in command:
```powershell
python -m http.server 9000  # Use port 9000 instead
# Then access: http://localhost:9000
```

### CORS Errors in Browser Console
**Problem:** "Access to XMLHttpRequest blocked by CORS policy"  
**Solution:**
- Ensure API has CORS configured (✅ already done in Program.cs)
- Restart API after code changes

---

## File Structure

```
consumer/
├── index.html              Application layout (7.3 KB)
├── styles.css              Responsive styling (15.2 KB)
├── app.js                  Client logic (16.6 KB)
└── serve.py                Optional Python server wrapper

src/Wp1Fall26Aws.Api/
├── Program.cs              CORS, FakeStorageService setup
├── Controllers/
│   └── DocumentsController.cs      REST endpoints
├── Storage/
│   ├── IStorageService.cs          Interface
│   ├── S3StorageService.cs         AWS implementation
│   └── FakeStorageService.cs       In-memory implementation
└── Models/
	├── DocumentMetadata.cs
	└── UploadResponse.cs
```

---

## API Endpoints (Reference)

```
GET    /api/documents              List all documents
GET    /api/documents/{key}        Get document metadata
POST   /api/documents              Upload new document (form-data)
PUT    /api/documents/{key}        Replace document (form-data)
DELETE /api/documents/{key}        Delete document
```

All requests/responses use JSON (except multipart for upload/replace).

---

## Configuration

### Change API URL
Edit `consumer/app.js` line 3:
```javascript
// Local development
const API_BASE_URL = 'http://localhost:5000';

// Or for remote API:
const API_BASE_URL = 'https://api.example.com';
```

### Add File Types
Edit `consumer/index.html` line 31 (input accept attribute):
```html
<!-- Current (TXT and PDF) -->
<input accept=".txt,.pdf" />

<!-- Add more types -->
<input accept=".txt,.pdf,.doc,.docx" />
```

### Adjust Auto-Refresh Interval
Edit `consumer/app.js` line 32:
```javascript
// Current: refresh every 5 seconds
setInterval(loadDocuments, 5000);

// Change to 10 seconds
setInterval(loadDocuments, 10000);
```

---

## Browser Console Tips

Open browser DevTools (F12) → Console to see:
- ✅ API requests/responses
- ✅ Error messages
- ✅ Loading state logs
- ✅ Network timing

Example:
```javascript
// View all documents in memory
console.log(documents);

// Check API status
fetch('http://localhost:5000/api/documents').then(r => r.json()).then(d => console.log(d));
```

---

## Next Steps

1. **Test with real files:** Upload sample .txt and .pdf files
2. **Validate all operations:** Test GET, POST, PUT, DELETE
3. **Check error handling:** Try invalid files, missing documents
4. **Review code:** Examine consumer/app.js for implementation details
5. **Deploy:** Host static files on S3, Netlify, Vercel, etc.

---

## Production Deployment

### Deploy Static Files (Consumer)
```bash
# Option 1: AWS S3
aws s3 sync consumer/ s3://my-bucket/

# Option 2: GitHub Pages
cp -r consumer/* docs/
git add docs/
git commit -m "Deploy client"
git push

# Option 3: Docker
# In Dockerfile: COPY consumer /var/www/html (nginx)
# Build and push to container registry
```

### Deploy API
```bash
# Option 1: Azure App Service
dotnet publish -c Release
# Deploy to App Service

# Option 2: Docker
dotnet publish -c Release
# Build image with Dockerfile, push to registry

# Option 3: Local server
# Same as above, just on production hardware
```

### Environment Variables
```bash
# API (production)
AWS:BucketName=my-prod-bucket
AWS:Prefix=documents/
ASPNETCORE_ENVIRONMENT=Production

# Client (update ASPNETCORE environment = Production in API)
# JavaScript: Update API_BASE_URL in app.js
const API_BASE_URL = 'https://api.example.com';
```

---

## Support

For issues, check:
1. `STATIC_CLIENT_DEMO.md` - Full documentation
2. Browser DevTools Console - Error messages
3. API logs - Server-side issues
4. Network tab (F12) - Request/response details

---

## Success Indicators

- [x] Both API and static server running
- [x] Browser loads http://localhost:8000
- [x] API status shows "● Connected"
- [x] Can upload documents
- [x] Can view, update, delete documents
- [x] Table updates automatically and manually
- [x] Error messages display correctly
- [x] All buttons respond to clicks

**You're ready to use the application! 🎉**
