// ==================== Configuration ====================
// Configure this to point to your running API instance
const API_BASE_URL = 'http://localhost:5000';

// ==================== State ====================
let selectedDocumentKey = null;
let documents = [];

// ==================== DOM Elements ====================
// These will be initialized in setupEventListeners() after DOM is ready
let apiStatusEl;
let uploadForm;
let fileInput;
let uploadButton;
let refreshButton;
let statusMessageEl;
let loadingIndicatorEl;
let documentsTableBody;
let emptyStateEl;
let documentCountEl;
let detailsSection;
let deleteButton;
let closeDetailsButton;
let copyKeyButton;
let replaceFileInput;
let replaceButton;

// ==================== Initialization ====================
document.addEventListener('DOMContentLoaded', () => {
    setupEventListeners();
    checkApiStatus();
    loadDocuments();

    // Refresh documents every 5 seconds
    setInterval(loadDocuments, 5000);
});

// ==================== Event Listeners ====================
function setupEventListeners() {
    // Initialize DOM elements after DOM is ready
    apiStatusEl = document.getElementById('apiStatus');
    uploadForm = document.getElementById('uploadForm');
    fileInput = document.getElementById('fileInput');
    uploadButton = document.getElementById('uploadButton');
    refreshButton = document.getElementById('refreshButton');
    statusMessageEl = document.getElementById('statusMessage');
    loadingIndicatorEl = document.getElementById('loadingIndicator');
    documentsTableBody = document.getElementById('documentsTableBody');
    emptyStateEl = document.getElementById('emptyState');
    documentCountEl = document.getElementById('documentCount');
    detailsSection = document.getElementById('detailsSection');
    deleteButton = document.getElementById('deleteButton');
    closeDetailsButton = document.getElementById('closeDetailsButton');
    copyKeyButton = document.getElementById('copyKeyButton');
    replaceFileInput = document.getElementById('replaceFileInput');
    replaceButton = document.getElementById('replaceButton');

    uploadForm.addEventListener('submit', handleUpload);
    refreshButton.addEventListener('click', loadDocuments);
    deleteButton.addEventListener('click', handleDelete);
    closeDetailsButton.addEventListener('click', closeDetails);
    copyKeyButton.addEventListener('click', copyKeyToClipboard);
    replaceButton.addEventListener('click', handleReplace);
    replaceFileInput.addEventListener('change', updateReplaceFileLabel);

    // Drag and drop support
    fileInput.addEventListener('change', updateFileLabel);
    const fileLabel = document.querySelector('.file-input-label');

    fileLabel.addEventListener('dragover', (e) => {
        e.preventDefault();
        fileLabel.classList.add('drag-over');
    });

    fileLabel.addEventListener('dragleave', () => {
        fileLabel.classList.remove('drag-over');
    });

    fileLabel.addEventListener('drop', (e) => {
        e.preventDefault();
        fileLabel.classList.remove('drag-over');
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            fileInput.files = files;
            updateFileLabel();
        }
    });
}

// ==================== API Status ====================
async function checkApiStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/documents`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        });

        if (response.ok) {
            updateApiStatus(true);
        } else {
            updateApiStatus(false);
        }
    } catch (error) {
        console.error('API status check failed:', error);
        updateApiStatus(false);
    }
}

function updateApiStatus(connected) {
    apiStatusEl.classList.remove('connected', 'disconnected');
    apiStatusEl.textContent = connected ? '● Connected' : '● Disconnected';
    apiStatusEl.classList.add(connected ? 'connected' : 'disconnected');
}

// ==================== Document Loading ====================
async function loadDocuments() {
    try {
        showLoading(true);
        const response = await fetch(`${API_BASE_URL}/api/documents`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        documents = await response.json();
        renderDocuments();
        updateApiStatus(true);
        hideStatusMessage();
    } catch (error) {
        console.error('Failed to load documents:', error);
        showError(`Failed to load documents: ${error.message}`);
        updateApiStatus(false);
        renderDocuments([]);
    } finally {
        showLoading(false);
    }
}

function renderDocuments() {
    const sortedDocs = [...documents].sort((a, b) => 
        (a.fileName || '').localeCompare(b.fileName || '')
    );

    documentsTableBody.innerHTML = '';

    if (sortedDocs.length === 0) {
        emptyStateEl.classList.remove('hidden');
        document.querySelector('.table-container').style.display = 'none';
        documentCountEl.textContent = '0 documents';
        return;
    }

    emptyStateEl.classList.add('hidden');
    document.querySelector('.table-container').style.display = 'block';
    documentCountEl.textContent = `${sortedDocs.length} document${sortedDocs.length !== 1 ? 's' : ''}`;

    sortedDocs.forEach((doc) => {
        const row = createTableRow(doc);
        documentsTableBody.appendChild(row);
    });

    // If a document is selected, update its details panel with latest data
    if (selectedDocumentKey) {
        const selectedDoc = documents.find(d => d.key === selectedDocumentKey);
        if (selectedDoc) {
            console.log('Updating details panel for selected document');
            showDetailsSection(selectedDoc);
        }
    }
}

function createTableRow(doc) {
    const row = document.createElement('tr');
    if (selectedDocumentKey === doc.key) {
        row.classList.add('selected');
    }

    const fileSize = formatFileSize(doc.size);
    const lastModified = doc.lastModified 
        ? new Date(doc.lastModified).toLocaleString()
        : '—';

    row.innerHTML = `
        <td>${escapeHtml(doc.fileName || 'Unknown')}</td>
        <td>${fileSize}</td>
        <td>${escapeHtml(doc.contentType || 'unknown')}</td>
        <td>${lastModified}</td>
        <td>
            <div class="table-actions">
                <button class="btn-action" onclick="selectDocument('${escapeHtmlAttr(doc.key)}')">View</button>
            </div>
        </td>
    `;

    return row;
}

// ==================== Document Selection ====================
function selectDocument(key) {
    selectedDocumentKey = key;
    const doc = documents.find(d => d.key === key);

    if (doc) {
        showDetailsSection(doc);
        renderDocuments();
    }
}

function showDetailsSection(doc) {
    detailsSection.classList.remove('hidden');

    const fileNameEl = document.getElementById('detailFileName');
    const keyEl = document.getElementById('detailKey');
    const sizeEl = document.getElementById('detailSize');
    const contentTypeEl = document.getElementById('detailContentType');
    const lastModEl = document.getElementById('detailLastModified');

    fileNameEl.textContent = escapeHtml(doc.fileName || '—');
    keyEl.textContent = escapeHtml(doc.key || '—');
    sizeEl.textContent = formatFileSize(doc.size || 0);
    contentTypeEl.textContent = escapeHtml(doc.contentType || '—');

    if (doc.lastModified) {
        lastModEl.textContent = new Date(doc.lastModified).toLocaleString();
    } else {
        lastModEl.textContent = '—';
    }

    // Scroll to details section
    detailsSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function closeDetails() {
    selectedDocumentKey = null;
    detailsSection.classList.add('hidden');
    renderDocuments();
}

function copyKeyToClipboard() {
    const keyElement = document.getElementById('detailKey');
    const text = keyElement.textContent;

    navigator.clipboard.writeText(text).then(() => {
        showSuccess('Object key copied to clipboard');

        // Visual feedback
        const originalText = copyKeyButton.textContent;
        copyKeyButton.textContent = '✓';
        setTimeout(() => {
            copyKeyButton.textContent = originalText;
        }, 2000);
    }).catch(err => {
        console.error('Failed to copy:', err);
        showError('Failed to copy to clipboard');
    });
}

// ==================== File Upload ====================
function updateFileLabel() {
    const fileLabel = document.querySelector('.file-input-label');
    const fileName = fileInput.files[0]?.name;

    if (fileName) {
        const fileText = fileLabel.querySelector('.file-text');
        fileText.textContent = escapeHtml(fileName);
        fileText.style.color = '#16a34a';
        fileLabel.style.borderColor = '#16a34a';
    } else {
        const fileText = fileLabel.querySelector('.file-text');
        fileText.textContent = 'Choose File (TXT or PDF)';
        fileText.style.color = 'var(--color-primary)';
        fileLabel.style.borderColor = 'var(--color-primary)';
    }
}

function updateReplaceFileLabel() {
    const fileLabel = document.querySelector('.replace-section .file-input-label');
    const fileName = replaceFileInput.files[0]?.name;

    if (fileName) {
        const fileText = fileLabel.querySelector('.file-text');
        fileText.textContent = escapeHtml(fileName);
        fileText.style.color = '#16a34a';
        fileLabel.style.borderColor = '#16a34a';
    } else {
        const fileText = fileLabel.querySelector('.file-text');
        fileText.textContent = 'Choose New File (TXT or PDF)';
        fileText.style.color = 'var(--color-primary)';
        fileLabel.style.borderColor = 'var(--color-primary)';
    }
}

async function handleUpload(e) {
    e.preventDefault();

    const file = fileInput.files[0];
    if (!file) {
        showError('Please select a file');
        return;
    }

    // Validate file type
    const allowedTypes = ['text/plain', 'application/pdf'];
    if (!allowedTypes.includes(file.type)) {
        showError('Only TXT and PDF files are allowed');
        fileInput.value = '';
        updateFileLabel();
        return;
    }

    // Validate file size (5 MB)
    const maxSize = 5 * 1024 * 1024;
    if (file.size > maxSize) {
        showError('File size exceeds 5 MB limit');
        fileInput.value = '';
        updateFileLabel();
        return;
    }

    try {
        uploadButton.disabled = true;
        uploadButton.textContent = '⏳ Uploading...';

        const formData = new FormData();
        formData.append('file', file);

        const response = await fetch(`${API_BASE_URL}/api/documents`, {
            method: 'POST',
            body: formData
            // Do NOT set Content-Type header; browser will set it with correct boundary
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            const errorMessage = errorData.message || errorData.error || `Upload failed: ${response.status}`;
            console.error('Upload error response:', errorData);
            throw new Error(errorMessage);
        }

        const result = await response.json();
        showSuccess(`Document uploaded successfully: ${escapeHtml(result.fileName || 'unknown')}`);

        // Reset form
        fileInput.value = '';
        updateFileLabel();

        // Reload documents
        await loadDocuments();
    } catch (error) {
        console.error('Upload error:', error);
        showError(`Upload failed: ${error.message}`);
    } finally {
        uploadButton.disabled = false;
        uploadButton.innerHTML = '<span class="btn-icon">⬆️</span>Upload';
    }
}

// ==================== Document Deletion ====================
async function handleDelete() {
    if (!selectedDocumentKey) {
        showError('No document selected');
        return;
    }

    if (!confirm('Are you sure you want to delete this document? This action cannot be undone.')) {
        return;
    }

    try {
        deleteButton.disabled = true;
        deleteButton.textContent = '🗑️ Deleting...';

        const url = `${API_BASE_URL}/api/documents/${selectedDocumentKey}`;

        const response = await fetch(url, {
            method: 'DELETE',
            headers: {
                'Accept': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Delete failed: ${response.status}`);
        }

        showSuccess('Document deleted successfully');
        closeDetails();
        await loadDocuments();
    } catch (error) {
        console.error('Delete error:', error);
        showError(`Failed to delete document: ${error.message}`);
    } finally {
        deleteButton.disabled = false;
        deleteButton.innerHTML = '<span class="btn-icon">🗑️</span>Delete Document';
    }
}

// ==================== Document Replacement (PUT) ====================
async function handleReplace() {
    if (!selectedDocumentKey) {
        showError('No document selected');
        return;
    }

    const file = replaceFileInput.files[0];
    if (!file) {
        showError('Please select a file to replace with');
        return;
    }

    // Validate file type
    const allowedTypes = ['text/plain', 'application/pdf'];
    if (!allowedTypes.includes(file.type)) {
        showError('Only TXT and PDF files are allowed');
        replaceFileInput.value = '';
        updateReplaceFileLabel();
        return;
    }

    // Validate file size (5 MB)
    const maxSize = 5 * 1024 * 1024;
    if (file.size > maxSize) {
        showError('File size exceeds 5 MB limit');
        replaceFileInput.value = '';
        updateReplaceFileLabel();
        return;
    }

    try {
        replaceButton.disabled = true;
        replaceButton.textContent = '⏳ Replacing...';

        const formData = new FormData();
        formData.append('file', file);

        const url = `${API_BASE_URL}/api/documents/${selectedDocumentKey}`;

        const response = await fetch(url, {
            method: 'PUT',
            body: formData
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `Replace failed: ${response.status}`);
        }

        const result = await response.json();
        showSuccess(`Document replaced successfully`);

        // Reset file input
        replaceFileInput.value = '';
        updateReplaceFileLabel();

        // Update the document in memory with the new metadata from API
        const docIndex = documents.findIndex(d => d.key === selectedDocumentKey);
        if (docIndex !== -1) {
            documents[docIndex] = result;
            showDetailsSection(documents[docIndex]);
            renderDocuments();
        }

        // Reload documents silently in background to ensure everything is in sync
        loadDocuments().catch(err => console.error('Background reload failed:', err));
    } catch (error) {
        console.error('Replace error:', error);
        showError(`Failed to replace document: ${error.message}`);
    } finally {
        replaceButton.disabled = false;
        replaceButton.innerHTML = '<span class="btn-icon">🔄</span>Replace File';
    }
}

// ==================== UI Helpers ====================
function showLoading(show) {
    if (show) {
        loadingIndicatorEl.classList.remove('hidden');
    } else {
        loadingIndicatorEl.classList.add('hidden');
    }
}

function showStatusMessage(message, type = 'info') {
    statusMessageEl.textContent = message;
    statusMessageEl.className = `status-message show ${type}`;

    if (type !== 'error') {
        setTimeout(() => {
            statusMessageEl.classList.remove('show');
        }, 5000);
    }
}

function hideStatusMessage() {
    statusMessageEl.classList.remove('show');
}

function showSuccess(message) {
    showStatusMessage(message, 'success');
}

function showError(message) {
    showStatusMessage(message, 'error');
}

function showWarning(message) {
    showStatusMessage(message, 'warning');
}

// ==================== Utilities ====================
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
}

function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

function escapeHtmlAttr(text) {
    return escapeHtml(text).replace(/"/g, '&quot;');
}
