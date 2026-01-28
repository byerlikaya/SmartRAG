(function () {
    'use strict';

    var basePath = window.location.pathname.replace(/\/$/, '') || '/smartrag';
    if (!basePath.startsWith('/')) basePath = '/' + basePath;
    var apiBase = basePath + '/api';

    function getDocList() {
        return fetch(apiBase + '/documents?skip=0&take=100').then(function (r) {
            if (!r.ok) throw new Error('Failed to load documents');
            return r.json();
        });
    }

    function deleteDoc(id) {
        return fetch(apiBase + '/documents/' + id, { method: 'DELETE' }).then(function (r) {
            if (r.status === 404) throw new Error('Not found');
            if (!r.ok) throw new Error('Delete failed');
        });
    }

    function uploadDoc(file, uploadedBy, language) {
        var form = new FormData();
        form.append('file', file);
        form.append('uploadedBy', uploadedBy);
        if (language) form.append('language', language);
        return fetch(apiBase + '/documents', {
            method: 'POST',
            body: form
        }).then(function (r) {
            if (!r.ok) return r.text().then(function (t) { throw new Error(t || 'Upload failed'); });
            return r.json();
        });
    }

    function getSupportedTypes() {
        return fetch(apiBase + '/upload/supported-types').then(function (r) {
            if (!r.ok) return {};
            return r.json();
        });
    }

    function getChatConfig() {
        return fetch(apiBase + '/chat/config').then(function (r) {
            if (!r.ok) return { provider: '', model: 'Unknown' };
            return r.json();
        });
    }

    function sendChatMessage(message, sessionId) {
        return fetch(apiBase + '/chat/messages', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: message, sessionId: sessionId || null })
        }).then(function (r) {
            if (!r.ok) return r.text().then(function (t) { throw new Error(t || 'Chat failed'); });
            return r.json();
        });
    }

    function formatBytes(n) {
        if (n === 0) return '0 B';
        var k = 1024;
        var sizes = ['B', 'KB', 'MB', 'GB'];
        var i = Math.floor(Math.log(n) / Math.log(k));
        return parseFloat((n / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
    }

    function formatDate(s) {
        try {
            var d = new Date(s);
            return isNaN(d.getTime()) ? s : d.toLocaleString();
        } catch (e) {
            return s;
        }
    }

    function renderDocTable(items, totalCount) {
        var tbody = document.getElementById('sr-doc-tbody');
        var summary = document.getElementById('sr-doc-summary');
        summary.textContent = totalCount + ' document(s)';
        tbody.innerHTML = '';
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4">No documents.</td></tr>';
            return;
        }
        items.forEach(function (doc) {
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + escapeHtml(doc.fileName) + '</td>' +
                '<td>' + formatBytes(doc.fileSize) + '</td>' +
                '<td>' + formatDate(doc.uploadedAt) + '</td>' +
                '<td><button type="button" class="sr-btn sr-btn-danger sr-delete-btn" data-id="' + escapeHtml(doc.id) + '">Delete</button></td>';
            tbody.appendChild(tr);
        });
        tbody.querySelectorAll('.sr-delete-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = btn.getAttribute('data-id');
                if (id && confirm('Delete this document?')) {
                    deleteDoc(id).then(loadDocuments).catch(function (err) {
                        alert(err.message || 'Delete failed');
                    });
                }
            });
        });
    }

    function escapeHtml(s) {
        if (s == null) return '';
        var div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }

    function loadDocuments() {
        getDocList().then(function (data) {
            renderDocTable(data.items || [], data.totalCount || 0);
        }).catch(function () {
            document.getElementById('sr-doc-summary').textContent = 'Failed to load documents.';
            document.getElementById('sr-doc-tbody').innerHTML = '<tr><td colspan="4">Error loading list.</td></tr>';
        });
    }

    function loadSupportedTypes() {
        getSupportedTypes().then(function (data) {
            var extensions = (data.extensions || []).join(',');
            var input = document.getElementById('sr-file-input');
            if (extensions && input) input.setAttribute('accept', extensions);
        });
    }

    function loadChatConfig() {
        getChatConfig().then(function (data) {
            var badge = document.getElementById('sr-model-badge');
            if (badge) badge.textContent = (data.provider || '') + (data.model ? ' / ' + data.model : '');
        }).catch(function () {
            var badge = document.getElementById('sr-model-badge');
            if (badge) badge.textContent = 'Model unknown';
        });
    }

    function appendChatMessage(role, text) {
        var container = document.getElementById('sr-chat-messages');
        if (!container) return;
        var div = document.createElement('div');
        div.className = 'sr-msg sr-msg-' + role;
        div.textContent = text;
        container.appendChild(div);
        container.scrollTop = container.scrollHeight;
    }

    var chatSessionId = null;

    function sendChat() {
        var input = document.getElementById('sr-chat-input');
        if (!input) return;
        var text = (input.value || '').trim();
        if (!text) return;
        input.value = '';
        appendChatMessage('user', text);
        var sendBtn = document.getElementById('sr-chat-send');
        if (sendBtn) sendBtn.disabled = true;
        sendChatMessage(text, chatSessionId).then(function (res) {
            if (res.sessionId) chatSessionId = res.sessionId;
            appendChatMessage('assistant', res.answer || '');
        }).catch(function (err) {
            appendChatMessage('assistant', 'Error: ' + (err.message || 'Request failed'));
        }).finally(function () {
            if (sendBtn) sendBtn.disabled = false;
        });
    }

    function switchView(viewId) {
        var documentsView = document.getElementById('sr-view-documents');
        var chatView = document.getElementById('sr-view-chat');
        var navItems = document.querySelectorAll('.sr-nav-item');
        if (viewId === 'documents') {
            if (documentsView) documentsView.classList.remove('sr-view-hidden');
            if (chatView) chatView.classList.add('sr-view-hidden');
        } else {
            if (documentsView) documentsView.classList.add('sr-view-hidden');
            if (chatView) chatView.classList.remove('sr-view-hidden');
        }
        navItems.forEach(function (el) {
            var isActive = el.getAttribute('data-view') === viewId;
            el.classList.toggle('sr-nav-item-active', isActive);
            el.setAttribute('aria-current', isActive ? 'page' : null);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        loadDocuments();
        loadSupportedTypes();
        loadChatConfig();

        document.querySelectorAll('.sr-nav-item').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var view = btn.getAttribute('data-view');
                if (view) switchView(view);
            });
        });

        document.getElementById('sr-refresh-btn').addEventListener('click', loadDocuments);

        document.getElementById('sr-upload-btn').addEventListener('click', function () {
            var fileInput = document.getElementById('sr-file-input');
            var uploadedByInput = document.getElementById('sr-uploaded-by');
            var statusEl = document.getElementById('sr-upload-status');
            var file = fileInput && fileInput.files && fileInput.files[0];
            if (!file) {
                if (statusEl) statusEl.textContent = 'Select a file.';
                return;
            }
            var uploadedBy = (uploadedByInput && uploadedByInput.value) || 'dashboard';
            if (statusEl) statusEl.textContent = 'Uploading...';
            uploadDoc(file, uploadedBy).then(function () {
                if (statusEl) statusEl.textContent = 'Uploaded.';
                if (fileInput) fileInput.value = '';
                loadDocuments();
            }).catch(function (err) {
                if (statusEl) statusEl.textContent = 'Error: ' + (err.message || 'Upload failed');
            });
        });

        document.getElementById('sr-chat-send').addEventListener('click', sendChat);
        document.getElementById('sr-chat-input').addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendChat();
            }
        });
    });
})();
