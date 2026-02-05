(function () {
    'use strict';

    var basePath = window.location.pathname.replace(/\/$/, '') || '/smartrag';
    if (!basePath.startsWith('/')) basePath = '/' + basePath;
    var apiBase = basePath + '/api';

    var activeConversationId = null;

    function getDocList() {
        return fetch(apiBase + '/documents?skip=0&take=100').then(function (r) {
            if (!r.ok) throw new Error('Failed to load documents');
            return r.json();
        });
    }

    function getSchemaDocList() {
        return fetch(apiBase + '/documents/schemas?skip=0&take=100').then(function (r) {
            if (!r.ok) throw new Error('Failed to load schema documents');
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

    function renderDocTable(items, totalCount, tbodyId, summaryId, emptyText, options) {
        options = options || {};
        var showFileType = options.showFileType === true;
        var showDatabaseType = options.showDatabaseType === true;
        var showCollectionName = options.showCollectionName === true;
        var colCount = 4 + (showFileType ? 1 : 0) + (showDatabaseType ? 1 : 0) + (showCollectionName ? 1 : 0);

        var tbody = document.getElementById(tbodyId);
        var summary = document.getElementById(summaryId);
        if (summary) {
            summary.textContent = totalCount + ' document(s)';
        }
        if (!tbody) return;
        tbody.innerHTML = '';
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="' + colCount + '">' + (emptyText || 'No documents.') + '</td></tr>';
            return;
        }
        var copyIconSvg = '<span class="sr-copy-icon" aria-hidden="true"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><path d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12V1zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm0 16H8V7h11v14z"/></svg></span>';
        items.forEach(function (doc) {
            var tr = document.createElement('tr');
            var nameCell = '<td>' + escapeHtml(doc.fileName) + '</td>';
            var fileTypeCell = showFileType ? '<td>' + escapeHtml(doc.contentType || '') + '</td>' : '';
            var dbTypeCell = showDatabaseType ? '<td>' + escapeHtml(doc.databaseType || '') + '</td>' : '';
            var collectionValue = doc.collectionName || '';
            var collectionCell = showCollectionName
                ? '<td class="sr-doc-cell-collection" title="' + escapeAttr(collectionValue) + '">' +
                    '<div class="sr-doc-cell-collection-inner">' +
                    '<span class="sr-doc-collection-text">' + escapeHtml(collectionValue) + '</span> ' +
                    '<button type="button" class="sr-btn sr-btn-secondary sr-doc-copy-btn" data-collection="' + escapeAttr(collectionValue) + '" title="Copy collection name" aria-label="Copy collection name">' + copyIconSvg + '</button>' +
                    '</div></td>'
                : '';
            tr.innerHTML =
                nameCell +
                fileTypeCell +
                dbTypeCell +
                collectionCell +
                '<td>' + formatBytes(doc.fileSize) + '</td>' +
                '<td>' + formatDate(doc.uploadedAt) + '</td>' +
                '<td>' +
                    '<button type="button" class="sr-btn sr-btn-secondary sr-doc-details-btn" data-id="' + escapeHtml(doc.id) + '" data-name="' + escapeHtml(doc.fileName) + '">Details</button> ' +
                    '<button type="button" class="sr-btn sr-btn-danger sr-delete-btn" data-id="' + escapeHtml(doc.id) + '">Delete</button>' +
                '</td>';
            tbody.appendChild(tr);
        });
        tbody.querySelectorAll('.sr-delete-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = btn.getAttribute('data-id');
                if (id && confirm('Delete this document?')) {
                    deleteDoc(id).then(function () {
                        loadDocuments();
                        loadSchemaDocuments();
                    }).catch(function (err) {
                        alert(err.message || 'Delete failed');
                    });
                }
            });
        });

        tbody.querySelectorAll('.sr-doc-details-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = btn.getAttribute('data-id');
                var name = btn.getAttribute('data-name') || '';
                if (id) {
                    loadDocumentDetails(id, name);
                }
            });
        });

        var copyIconSvg = '<span class="sr-copy-icon" aria-hidden="true"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><path d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12V1zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm0 16H8V7h11v14z"/></svg></span>';
        var checkIconSvg = '<span class="sr-copy-icon" aria-hidden="true"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/></svg></span>';
        tbody.querySelectorAll('.sr-doc-copy-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var value = btn.getAttribute('data-collection') || '';
                if (!value) return;
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(value).then(function () {
                        btn.innerHTML = checkIconSvg;
                        btn.setAttribute('title', 'Copied!');
                        setTimeout(function () {
                            btn.innerHTML = copyIconSvg;
                            btn.setAttribute('title', 'Copy collection name');
                        }, 1500);
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

    function escapeAttr(s) {
        if (s == null) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function loadDocuments() {
        getDocList().then(function (data) {
            renderDocTable(
                data.items || [],
                data.totalCount || 0,
                'sr-doc-tbody-user',
                'sr-doc-summary-user',
                'No documents.',
                { showFileType: true, showCollectionName: true }
            );
        }).catch(function () {
            var summary = document.getElementById('sr-doc-summary-user');
            var tbody = document.getElementById('sr-doc-tbody-user');
            if (summary) summary.textContent = 'Failed to load documents.';
            if (tbody) tbody.innerHTML = '<tr><td colspan="6">Error loading list.</td></tr>';
        });
    }

    function loadSchemaDocuments() {
        getSchemaDocList().then(function (data) {
            renderDocTable(
                data.items || [],
                data.totalCount || 0,
                'sr-doc-tbody-schemas',
                'sr-doc-summary-schemas',
                'No schema documents.',
                { showDatabaseType: true, showCollectionName: true }
            );
        }).catch(function () {
            var summary = document.getElementById('sr-doc-summary-schemas');
            var tbody = document.getElementById('sr-doc-tbody-schemas');
            if (summary) summary.textContent = 'Failed to load schema documents.';
            if (tbody) tbody.innerHTML = '<tr><td colspan="6">Error loading list.</td></tr>';
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

            var featuresContainer = document.getElementById('sr-chat-features');
            var mcpContainer = document.getElementById('sr-chat-mcp');

            if (featuresContainer) {
                featuresContainer.innerHTML = '';
                var f = data.features || {};

                var featureList = [
                    { key: 'enableDatabaseSearch', label: 'DB search' },
                    { key: 'enableDocumentSearch', label: 'Document search' },
                    { key: 'enableAudioSearch', label: 'Audio' },
                    { key: 'enableImageSearch', label: 'Image' },
                    { key: 'enableMcpSearch', label: 'MCP' }
                ];

                featureList.forEach(function (item) {
                    var enabled = !!f[item.key];
                    var badgeEl = document.createElement('span');
                    badgeEl.className = 'sr-chat-feature-badge ' + (enabled ? 'sr-chat-feature-badge--on' : 'sr-chat-feature-badge--off');
                    var labelEl = document.createElement('span');
                    labelEl.className = 'sr-chat-feature-badge-label';
                    labelEl.textContent = item.label;
                    var statusEl = document.createElement('span');
                    statusEl.className = 'sr-chat-feature-badge-status';
                    statusEl.textContent = enabled ? 'ON' : 'OFF';
                    badgeEl.appendChild(labelEl);
                    badgeEl.appendChild(statusEl);
                    featuresContainer.appendChild(badgeEl);
                });
            }

            var mcpRow = document.getElementById('sr-chat-mcp-row');
            if (mcpContainer && mcpRow) {
                mcpContainer.innerHTML = '';
                var servers = data.mcpServers || [];
                if (servers.length === 0) {
                    mcpRow.style.display = 'none';
                } else {
                    mcpRow.style.display = 'flex';
                    mcpContainer.style.display = 'flex';
                    var label = document.createElement('span');
                    label.className = 'sr-chat-mcp-label';
                    label.textContent = 'MCP';
                    mcpContainer.appendChild(label);

                    servers.forEach(function (s) {
                        var tag = document.createElement('span');
                        tag.className = 'sr-chat-mcp-tag';
                        var url = (s.endpoint || s.id || '').toString();
                        if (url) {
                            tag.setAttribute('data-mcp-url', url);
                        }
                        tag.textContent = s.id || s.endpoint || 'Server';
                        if (url) {
                            var tooltipEl = document.createElement('span');
                            tooltipEl.className = 'sr-chat-mcp-tooltip';
                            tooltipEl.textContent = url;
                            tag.appendChild(tooltipEl);
                        }
                        mcpContainer.appendChild(tag);
                    });
                }
            }
        }).catch(function () {
            var badge = document.getElementById('sr-model-badge');
            if (badge) badge.textContent = 'Model unknown';

            var mcpRow = document.getElementById('sr-chat-mcp-row');
            if (mcpRow) mcpRow.style.display = 'none';
        });
    }

    function getUserAvatarSvg() {
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg>';
    }

    function getAiAvatarSvg() {
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M12 2l1.5 4.5L18 8l-4.5 1.5L12 14l-1.5-4.5L6 8l4.5-1.5L12 2z"/><path d="M5 16l.75 2.25L8 19l-2.25.75L5 22l-.75-2.25L2 19l2.25-.75L5 16zm14 0l.75 2.25L22 19l-2.25.75L19 22l-.75-2.25L16 19l2.25-.75L19 16z"/></svg>';
    }

    function appendChatMessage(role, text) {
        var container = document.getElementById('sr-chat-messages');
        if (!container) return;
        var row = document.createElement('div');
        row.className = 'sr-msg-row sr-msg-row-' + role;
        var avatar = document.createElement('div');
        avatar.className = 'sr-msg-avatar sr-msg-avatar-' + role;
        avatar.setAttribute('aria-hidden', 'true');
        avatar.innerHTML = role === 'user' ? getUserAvatarSvg() : getAiAvatarSvg();
        var body = document.createElement('div');
        body.className = 'sr-msg sr-msg-' + role;
        body.textContent = text;
        row.appendChild(avatar);
        row.appendChild(body);
        container.appendChild(row);
        container.scrollTop = container.scrollHeight;
    }

    function groupSourcesByDocument(sources) {
        var byDoc = {};
        sources.forEach(function (src) {
            var key = (src.documentId || '').toString() || (src.fileName || 'Document');
            if (!byDoc[key]) byDoc[key] = { fileName: src.fileName || 'Document', documentId: src.documentId, chunks: [] };
            byDoc[key].chunks.push(src);
        });
        Object.keys(byDoc).forEach(function (key) {
            byDoc[key].chunks.sort(function (a, b) {
                var ia = a.chunkIndex != null ? a.chunkIndex : -1;
                var ib = b.chunkIndex != null ? b.chunkIndex : -1;
                return ia - ib;
            });
        });
        return Object.keys(byDoc).map(function (key) { return byDoc[key]; });
    }

    function appendAssistantMessageWithSources(text, sources) {
        var container = document.getElementById('sr-chat-messages');
        if (!container) return;
        var wrap = document.createElement('div');
        wrap.className = 'sr-msg-assistant-wrap';
        var row = document.createElement('div');
        row.className = 'sr-msg-row sr-msg-row-assistant';
        var avatar = document.createElement('div');
        avatar.className = 'sr-msg-avatar sr-msg-avatar-assistant';
        avatar.setAttribute('aria-hidden', 'true');
        avatar.innerHTML = getAiAvatarSvg();
        var bodyWrap = document.createElement('div');
        bodyWrap.className = 'sr-msg-body-wrap';
        var msgDiv = document.createElement('div');
        msgDiv.className = 'sr-msg sr-msg-assistant';
        msgDiv.textContent = text;
        bodyWrap.appendChild(msgDiv);
        if (sources && sources.length > 0) {
            var groups = groupSourcesByDocument(sources);
            var sourcesRow = document.createElement('div');
            sourcesRow.className = 'sr-msg-sources';
            var label = document.createElement('span');
            label.className = 'sr-msg-sources-label';
            label.textContent = 'Sources:';
            sourcesRow.appendChild(label);
            groups.forEach(function (group) {
                var pill = document.createElement('button');
                pill.type = 'button';
                pill.className = 'sr-source-pill';
                var count = group.chunks.length;
                var label = group.fileName + (count > 1 ? ' (' + count + ' chunks)' : ' (1 chunk)');
                pill.textContent = label;
                pill.setAttribute('aria-label', 'View document chunks');
                (function (docGroup) {
                    pill.addEventListener('click', function () {
                        var query = getQueryForSourceDetail(pill);
                        openSourceDetailByDocument(docGroup.fileName, docGroup.chunks, query);
                    });
                })(group);
                sourcesRow.appendChild(pill);
            });
            bodyWrap.appendChild(sourcesRow);
        }
        row.appendChild(avatar);
        row.appendChild(bodyWrap);
        wrap.appendChild(row);
        container.appendChild(wrap);
        container.scrollTop = container.scrollHeight;
    }

    function getQueryForSourceDetail(pillElement) {
        var wrap = pillElement && pillElement.closest && pillElement.closest('.sr-msg-assistant-wrap');
        if (!wrap || !wrap.previousElementSibling) return '';
        var el = wrap.previousElementSibling;
        while (el) {
            if (el.classList && el.classList.contains('sr-msg-row') && el.classList.contains('sr-msg-row-user')) {
                var msg = el.querySelector('.sr-msg');
                return msg ? (msg.textContent || '').trim() : '';
            }
            el = el.previousElementSibling;
        }
        return '';
    }

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }

    function escapeRegex(s) {
        return String(s).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    function highlightQueryInChunkText(text, query) {
        if (!text) return '';
        var escaped = escapeHtml(text);
        var words = (query || '').split(/\s+/).filter(function (w) { return w.length >= 2; });
        if (words.length === 0) return escaped;
        var pattern;
        try {
            pattern = new RegExp('(' + words.map(escapeRegex).join('|') + ')', 'gi');
        } catch (e) {
            return escaped;
        }
        return escaped.replace(pattern, function (m) {
            return '<span class="sr-chunk-highlight">' + m + '</span>';
        });
    }

    function openSourceDetailByDocument(fileName, chunks, query) {
        var panel = document.getElementById('sr-source-details');
        var titleEl = document.getElementById('sr-source-details-title');
        var subtitleEl = document.getElementById('sr-source-details-subtitle');
        var chunksEl = document.getElementById('sr-source-details-chunks');
        if (!panel || !titleEl || !subtitleEl || !chunksEl) return;
        titleEl.textContent = fileName || 'Document';
        subtitleEl.textContent = (chunks && chunks.length) ? chunks.length + ' chunk(s)' : 'No chunks';
        chunksEl.innerHTML = '';
        if (chunks && chunks.length > 0) {
            chunks.forEach(function (chunk, idx) {
                var div = document.createElement('div');
                div.className = 'sr-doc-chunk';
                var header = document.createElement('div');
                header.className = 'sr-doc-chunk-header';
                header.textContent = 'Chunk #' + (chunk.chunkIndex != null ? chunk.chunkIndex : idx);
                var body = document.createElement('pre');
                body.className = 'sr-doc-chunk-body';
                var content = chunk.relevantContent || '';
                body.innerHTML = highlightQueryInChunkText(content, query);
                div.appendChild(header);
                div.appendChild(body);
                chunksEl.appendChild(div);
            });
        }
        panel.classList.add('sr-source-details-visible');
    }

    function closeSourceDetail() {
        var panel = document.getElementById('sr-source-details');
        if (panel) panel.classList.remove('sr-source-details-visible');
    }

    function showChatTypingIndicator() {
        var container = document.getElementById('sr-chat-messages');
        if (!container) return;
        removeChatTypingIndicator();
        var row = document.createElement('div');
        row.id = 'sr-chat-typing-indicator';
        row.className = 'sr-msg-row sr-msg-row-assistant';
        row.setAttribute('aria-live', 'polite');
        row.setAttribute('aria-label', 'Waiting for response');
        var avatar = document.createElement('div');
        avatar.className = 'sr-msg-avatar sr-msg-avatar-assistant';
        avatar.setAttribute('aria-hidden', 'true');
        avatar.innerHTML = getAiAvatarSvg();
        var wrap = document.createElement('div');
        wrap.className = 'sr-msg sr-msg-assistant sr-msg-typing';
        var label = document.createElement('span');
        label.className = 'sr-typing-label';
        label.textContent = 'Thinking';
        var dots = document.createElement('span');
        dots.className = 'sr-typing-dots';
        dots.setAttribute('aria-hidden', 'true');
        dots.innerHTML = '<span class="sr-typing-dot"></span><span class="sr-typing-dot"></span><span class="sr-typing-dot"></span>';
        wrap.appendChild(label);
        wrap.appendChild(dots);
        row.appendChild(avatar);
        row.appendChild(wrap);
        container.appendChild(row);
        container.scrollTop = container.scrollHeight;
    }

    function removeChatTypingIndicator() {
        var el = document.getElementById('sr-chat-typing-indicator');
        if (el && el.parentNode) el.parentNode.removeChild(el);
    }

    var chatSessionId = null;
    var isChatRequestPending = false;

    var chatLastUpdated = null;

    function updateSessionIdDisplay() {
        var el = document.getElementById('sr-chat-session-id');
        if (el) {
            el.textContent = chatSessionId || '—';
            el.setAttribute('title', chatSessionId || '');
        }
        var copyBtn = document.getElementById('sr-chat-session-copy');
        if (copyBtn) {
            copyBtn.style.display = chatSessionId ? 'inline-flex' : 'none';
        }
        var updatedRow = document.getElementById('sr-chat-last-updated-row');
        var updatedEl = document.getElementById('sr-chat-last-updated');
        if (updatedRow && updatedEl) {
            if (chatSessionId && chatLastUpdated) {
                updatedEl.textContent = new Date(chatLastUpdated).toLocaleString();
                updatedRow.style.display = '';
            } else {
                updatedRow.style.display = 'none';
            }
        }
    }

    function setChatPending(pending) {
        isChatRequestPending = pending;
        var sidebar = document.getElementById('sr-chat-sidebar');
        if (sidebar) sidebar.classList.toggle('sr-chat-sidebar-pending', pending);
        var input = document.getElementById('sr-chat-input');
        var sendBtn = document.getElementById('sr-chat-send');
        var inputWrap = document.getElementById('sr-chat-input-wrap');
        if (input) input.disabled = pending;
        if (sendBtn) sendBtn.disabled = pending;
        if (inputWrap) inputWrap.classList.toggle('sr-chat-input-pending', pending);
    }

    function sendChat() {
        if (isChatRequestPending) return;
        var input = document.getElementById('sr-chat-input');
        if (!input) return;
        var text = (input.value || '').trim();
        if (!text) return;
        setChatPending(true);
        input.value = '';
        appendChatMessage('user', text);
        showChatTypingIndicator();
        var sendBtn = document.getElementById('sr-chat-send');
        sendChatMessage(text, chatSessionId).then(function (res) {
            removeChatTypingIndicator();
            if (res.sessionId) chatSessionId = res.sessionId;
            if (res.lastUpdated) chatLastUpdated = res.lastUpdated;
            if (res.sources && res.sources.length > 0) {
                appendAssistantMessageWithSources(res.answer || '', res.sources);
            } else {
                appendChatMessage('assistant', res.answer || '');
            }
            updateSessionIdDisplay();
            reloadConversations(res.sessionId);
        }).catch(function (err) {
            removeChatTypingIndicator();
            appendChatMessage('assistant', 'Error: ' + (err.message || 'Request failed'));
        }).finally(function () {
            setChatPending(false);
            if (input) input.focus();
        });
    }

    function switchView(viewId) {
        var documentsView = document.getElementById('sr-view-documents');
        var chatView = document.getElementById('sr-view-chat');
        var settingsView = document.getElementById('sr-view-settings');
        var navItems = document.querySelectorAll('.sr-nav-item');
        if (documentsView) documentsView.classList.toggle('sr-view-hidden', viewId !== 'documents');
        if (chatView) chatView.classList.toggle('sr-view-hidden', viewId !== 'chat');
        if (settingsView) {
            settingsView.classList.toggle('sr-view-hidden', viewId !== 'settings');
            if (viewId === 'settings') loadSettings();
        }
        navItems.forEach(function (el) {
            var isActive = el.getAttribute('data-view') === viewId;
            el.classList.toggle('sr-nav-item-active', isActive);
            el.setAttribute('aria-current', isActive ? 'page' : null);
        });
    }

    function getSettings() {
        return fetch(apiBase + '/settings').then(function (r) {
            if (!r.ok) throw new Error('Failed to load settings');
            return r.json();
        });
    }

    function renderSettings(data) {
        var container = document.getElementById('sr-settings-content');
        var loading = document.getElementById('sr-settings-loading');
        if (!container) return;
        if (loading) loading.remove();
        container.innerHTML = '';

        function section(title, content) {
            var sectionEl = document.createElement('div');
            sectionEl.className = 'sr-settings-section';
            var h3 = document.createElement('h3');
            h3.className = 'sr-settings-section-title';
            h3.textContent = title;
            sectionEl.appendChild(h3);
            if (typeof content === 'string') {
                sectionEl.innerHTML = sectionEl.innerHTML + content;
            } else if (content && content.tagName === 'TABLE') {
                var wrap = document.createElement('div');
                wrap.className = 'sr-settings-table-wrap';
                wrap.appendChild(content);
                sectionEl.appendChild(wrap);
            } else {
                sectionEl.appendChild(content);
            }
            container.appendChild(sectionEl);
        }

        function isBooleanLike(val) {
            if (val === null || val === undefined) return false;
            if (typeof val === 'boolean') return true;
            var s = String(val).trim().toLowerCase();
            return s === 'true' || s === 'false' || s === 'yes' || s === 'no' || s === '1' || s === '0';
        }

        function badgeValue(val) {
            if (val === null || val === undefined) return false;
            if (typeof val === 'boolean') return !!val;
            var s = String(val).trim().toLowerCase();
            return s === 'true' || s === 'yes' || s === '1';
        }

        function keyValueTable(rows) {
            var table = document.createElement('table');
            table.className = 'sr-settings-table';
            rows.forEach(function (row) {
                var tr = document.createElement('tr');
                var tdKey = document.createElement('td');
                tdKey.className = 'sr-settings-key';
                tdKey.textContent = row.key;
                var tdVal = document.createElement('td');
                tdVal.className = 'sr-settings-value';
                var useBadge = row.badge || isBooleanLike(row.value);
                if (useBadge) {
                    tdVal.innerHTML = badgeHtml(badgeValue(row.value));
                } else {
                    tdVal.textContent = row.value;
                }
                tr.appendChild(tdKey);
                tr.appendChild(tdVal);
                table.appendChild(tr);
            });
            return table;
        }

        function badgeHtml(on) {
            var cls = on ? 'sr-settings-badge sr-settings-badge-on' : 'sr-settings-badge sr-settings-badge-off';
            var text = on ? 'ON' : 'OFF';
            return '<span class="' + escapeHtml(cls) + '">' + escapeHtml(text) + '</span>';
        }

        function badge(on) {
            var span = document.createElement('span');
            span.className = 'sr-settings-badge ' + (on ? 'sr-settings-badge-on' : 'sr-settings-badge-off');
            span.textContent = on ? 'ON' : 'OFF';
            return span;
        }

        function keyValueRow(rows) {
            var rowEl = document.createElement('div');
            rowEl.className = 'sr-settings-kv-row';
            rows.forEach(function (row) {
                var item = document.createElement('div');
                item.className = 'sr-settings-kv-item';
                var label = document.createElement('span');
                label.className = 'sr-settings-kv-label';
                label.textContent = row.key;
                var valWrap = document.createElement('span');
                valWrap.className = 'sr-settings-kv-value';
                if (row.badge || isBooleanLike(row.value)) {
                    valWrap.appendChild(badge(badgeValue(row.value)));
                } else {
                    valWrap.textContent = row.value != null ? String(row.value) : '—';
                }
                item.appendChild(label);
                item.appendChild(valWrap);
                rowEl.appendChild(item);
            });
            return rowEl;
        }

        section('Providers', keyValueTable([
            { key: 'AI provider', value: data.providers && data.providers.ai ? data.providers.ai : '—' },
            { key: 'Storage provider', value: data.providers && data.providers.storage ? data.providers.storage : '—' },
            { key: 'Conversation storage', value: data.providers && data.providers.conversation ? data.providers.conversation : '—' }
        ]));

        if (data.features) {
            var featDiv = document.createElement('div');
            featDiv.className = 'sr-settings-features';
            var featNames = [
                { k: 'enableDatabaseSearch', l: 'Database search' },
                { k: 'enableDocumentSearch', l: 'Document search' },
                { k: 'enableAudioSearch', l: 'Audio search' },
                { k: 'enableImageSearch', l: 'Image search' },
                { k: 'enableMcpSearch', l: 'MCP search' },
                { k: 'enableFileWatcher', l: 'File watcher' }
            ];
            featNames.forEach(function (f) {
                var on = data.features[f.k];
                var wrap = document.createElement('div');
                wrap.className = 'sr-settings-feature-item';
                var label = document.createElement('span');
                label.textContent = f.l;
                wrap.appendChild(label);
                wrap.appendChild(badge(!!on));
                featDiv.appendChild(wrap);
            });
            section('Features', featDiv);
        }

        if (data.chunking) {
            section('Chunking', keyValueTable([
                { key: 'Max chunk size', value: String(data.chunking.maxChunkSize) },
                { key: 'Min chunk size', value: String(data.chunking.minChunkSize) },
                { key: 'Chunk overlap', value: String(data.chunking.chunkOverlap) }
            ]));
        }

        if (data.retry) {
            var retryRows = [
                { key: 'Max retry attempts', value: String(data.retry.maxRetryAttempts) },
                { key: 'Retry delay (ms)', value: String(data.retry.retryDelayMs) },
                { key: 'Retry policy', value: data.retry.retryPolicy || '—' },
                { key: 'Fallback providers', value: data.retry.enableFallbackProviders, badge: true }
            ];
            if (data.retry.fallbackProviders && data.retry.fallbackProviders.length) {
                retryRows.push({ key: 'Fallback list', value: data.retry.fallbackProviders.join(', ') });
            }
            section('Retry', keyValueTable(retryRows));
        }

        if (data.whisper) {
            section('Whisper', keyValueTable([
                { key: 'Model path', value: data.whisper.modelPath || '—' },
                { key: 'Default language', value: data.whisper.defaultLanguage || '—' },
                { key: 'Min confidence', value: String(data.whisper.minConfidenceThreshold) },
                { key: 'Max threads', value: String(data.whisper.maxThreads) }
            ]));
        }

        if (data.activeAi) {
            section('Active AI', keyValueTable([
                { key: 'Provider', value: data.activeAi.provider || '—' },
                { key: 'Model', value: data.activeAi.model || '—' },
                { key: 'Max tokens', value: String(data.activeAi.maxTokens) },
                { key: 'Temperature', value: String(data.activeAi.temperature) },
                { key: 'Endpoint', value: data.activeAi.endpoint || '—' }
            ]));
        }

        if (data.mcpServers && data.mcpServers.length > 0) {
            var mcpTable = document.createElement('table');
            mcpTable.className = 'sr-settings-table';
            mcpTable.innerHTML = '<tr><th>Server ID</th><th>Endpoint</th><th>Auto connect</th><th>Timeout (s)</th></tr>';
            data.mcpServers.forEach(function (s) {
                var tr = document.createElement('tr');
                tr.innerHTML = '<td class="sr-settings-key">' + escapeHtml(s.serverId || '') + '</td><td class="sr-settings-value">' + escapeHtml(s.endpoint || '') + '</td><td>' + badgeHtml(s.autoConnect) + '</td><td>' + (s.timeoutSeconds || '—') + '</td>';
                mcpTable.appendChild(tr);
            });
            section('MCP servers', mcpTable);
        }

        if (data.watchedFolders && data.watchedFolders.length > 0) {
            var wfTable = document.createElement('table');
            wfTable.className = 'sr-settings-table';
            wfTable.innerHTML = '<tr><th>Folder</th><th>Extensions</th><th>Subdirs</th><th>Auto upload</th></tr>';
            data.watchedFolders.forEach(function (w) {
                var tr = document.createElement('tr');
                var ext = (w.allowedExtensions && w.allowedExtensions.length) ? w.allowedExtensions.join(', ') : '—';
                tr.innerHTML = '<td class="sr-settings-key">' + escapeHtml(w.folderPath || '') + '</td><td class="sr-settings-value">' + escapeHtml(ext) + '</td><td>' + badgeHtml(w.includeSubdirectories) + '</td><td>' + badgeHtml(w.autoUpload) + '</td>';
                wfTable.appendChild(tr);
            });
            section('Watched folders', wfTable);
        }

        if (data.databaseConnections && data.databaseConnections.length > 0) {
            var dbTable = document.createElement('table');
            dbTable.className = 'sr-settings-table';
            dbTable.innerHTML = '<tr><th>Name</th><th>Type</th><th>Enabled</th></tr>';
            data.databaseConnections.forEach(function (d) {
                var tr = document.createElement('tr');
                tr.innerHTML = '<td class="sr-settings-key">' + escapeHtml(d.name || '') + '</td><td class="sr-settings-value">' + escapeHtml(d.databaseType || '') + '</td><td>' + badgeHtml(d.enabled) + '</td>';
                dbTable.appendChild(tr);
            });
            section('Database connections', dbTable);
        }

        if (data.remainingByCategory && typeof data.remainingByCategory === 'object') {
            var allRows = [];
            var keys = Object.keys(data.remainingByCategory);
            keys.forEach(function (categoryName) {
                var entries = data.remainingByCategory[categoryName];
                if (entries && entries.length > 0) {
                    entries.forEach(function (e) {
                        var v = e.value;
                        var keyLabel = e.path ? e.path.split(':').pop() : e.path;
                        if (isBooleanLike(v)) {
                            allRows.push({ key: keyLabel, value: badgeValue(v), badge: true });
                        } else {
                            allRows.push({ key: keyLabel, value: v });
                        }
                    });
                }
            });
            if (allRows.length > 0) {
                section('Configuration', keyValueRow(allRows));
            }
        }
    }

    function loadSettings() {
        var container = document.getElementById('sr-settings-content');
        var loading = document.getElementById('sr-settings-loading');
        if (loading && container) {
            container.innerHTML = '';
            container.appendChild(loading);
            loading.textContent = 'Loading...';
        }
        getSettings().then(function (data) {
            renderSettings(data);
        }).catch(function (err) {
            if (container) container.innerHTML = '<p class="sr-settings-error">Error: ' + escapeHtml(err.message || 'Failed to load settings') + '</p>';
        });
    }

    function switchDocTab(tabId) {
        var userWrap = document.getElementById('sr-doc-table-wrap-user');
        var schemaWrap = document.getElementById('sr-doc-table-wrap-schemas');
        var summaryUser = document.getElementById('sr-doc-summary-user');
        var summarySchemas = document.getElementById('sr-doc-summary-schemas');
        var tabs = document.querySelectorAll('.sr-doc-tab');

        if (tabId === 'user') {
            if (userWrap) userWrap.classList.remove('sr-doc-table-wrap-hidden');
            if (schemaWrap) schemaWrap.classList.add('sr-doc-table-wrap-hidden');
            if (summaryUser) summaryUser.classList.remove('sr-doc-summary-hidden');
            if (summarySchemas) summarySchemas.classList.add('sr-doc-summary-hidden');
        } else {
            if (userWrap) userWrap.classList.add('sr-doc-table-wrap-hidden');
            if (schemaWrap) schemaWrap.classList.remove('sr-doc-table-wrap-hidden');
            if (summaryUser) summaryUser.classList.add('sr-doc-summary-hidden');
            if (summarySchemas) summarySchemas.classList.remove('sr-doc-summary-hidden');
        }

        tabs.forEach(function (tab) {
            var isActive = tab.getAttribute('data-doc-tab') === tabId;
            tab.classList.toggle('sr-doc-tab-active', isActive);
            tab.setAttribute('aria-current', isActive ? 'page' : null);
        });
    }

    function getChatSessions() {
        return fetch(apiBase + '/chat/sessions').then(function (r) {
            if (!r.ok) throw new Error('Failed to load chat sessions');
            return r.json();
        });
    }

    function getChatSessionDetails(sessionId) {
        return fetch(apiBase + '/chat/sessions/' + encodeURIComponent(sessionId)).then(function (r) {
            if (r.status === 404) return null;
            if (!r.ok) throw new Error('Failed to load chat session');
            return r.json();
        });
    }

    function deleteConversation(sessionId) {
        if (!sessionId) return;
        if (!confirm('Delete this conversation?')) {
            return;
        }

        fetch(apiBase + '/chat/sessions/' + encodeURIComponent(sessionId), { method: 'DELETE' })
            .then(function (r) {
                if (!r.ok) throw new Error('Failed to delete conversation');

                if (activeConversationId === sessionId) {
                    activeConversationId = null;
                    chatSessionId = null;
                    chatLastUpdated = null;
                    var messages = document.getElementById('sr-chat-messages');
                    if (messages) {
                        messages.innerHTML = '';
                    }
                    updateSessionIdDisplay();
                }

                reloadConversations(null);
            })
            .catch(function (err) {
                alert(err.message || 'Failed to delete conversation');
            });
    }

    function renderConversationList(summaries) {
        var container = document.getElementById('sr-chat-conversations');
        var clearBtn = document.getElementById('sr-chat-clear-all');
        if (!container) return;
        container.innerHTML = '';
        var filtered = (summaries || []).filter(function (c) {
            var id = (c.id || '').toString();
            if (id === 'smartrag-current-session') return false;
            if (id.toLowerCase().indexOf('sources:') === 0) return false;
            return true;
        });
        if (filtered.length === 0) {
            if (clearBtn) clearBtn.style.display = 'none';
            var empty = document.createElement('div');
            empty.className = 'sr-chat-conversation-item';
            empty.textContent = 'No conversations yet.';
            container.appendChild(empty);
            return;
        }
        if (clearBtn) clearBtn.style.display = 'inline-flex';
        filtered.forEach(function (conv) {
            var div = document.createElement('div');
            var isActive = conv.id === activeConversationId;
            div.className = 'sr-chat-conversation-item' + (isActive ? ' sr-chat-conversation-item-active' : '');
            div.setAttribute('data-conv-id', conv.id);

            var header = document.createElement('div');
            header.className = 'sr-chat-conversation-item-header';

            var title = document.createElement('div');
            title.className = 'sr-chat-conversation-title';
            title.textContent = conv.title || 'Conversation';

            var deleteBtn = document.createElement('button');
            deleteBtn.type = 'button';
            deleteBtn.className = 'sr-chat-conversation-delete-btn';
            deleteBtn.setAttribute('aria-label', 'Delete conversation');
            deleteBtn.textContent = '×';

            deleteBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                deleteConversation(conv.id);
            });

            header.appendChild(title);
            header.appendChild(deleteBtn);

            var meta = document.createElement('div');
            meta.className = 'sr-chat-conversation-meta';
            meta.textContent = conv.createdAt ? new Date(conv.createdAt).toLocaleString() : (conv.lastUpdated ? new Date(conv.lastUpdated).toLocaleString() : '');

            div.appendChild(header);
            div.appendChild(meta);

            div.addEventListener('click', function () {
                var id = div.getAttribute('data-conv-id');
                if (id) {
                    activateConversation(id);
                    var layoutEl = document.getElementById('sr-chat-layout');
                    if (layoutEl) layoutEl.classList.remove('sr-chat-sidebar-open');
                }
            });

            container.appendChild(div);
        });
    }

    function renderConversationMessages(conversation) {
        var container = document.getElementById('sr-chat-messages');
        if (!container) return;
        container.innerHTML = '';
        if (!conversation || !conversation.messages) return;
        conversation.messages.forEach(function (m) {
            if (m.role === 'assistant' && m.sources && m.sources.length > 0) {
                appendAssistantMessageWithSources(m.text, m.sources);
            } else {
                appendChatMessage(m.role, m.text);
            }
        });
        container.scrollTop = container.scrollHeight;
    }

    function activateConversation(id) {
        if (!id) return;
        if (isChatRequestPending) return;
        getChatSessionDetails(id).then(function (detail) {
            if (!detail) return;
            activeConversationId = detail.id;
            chatSessionId = detail.id;
            chatLastUpdated = detail.lastUpdated || null;
            var container = document.getElementById('sr-chat-messages');
            if (container) {
                container.innerHTML = '';
            }
            renderConversationMessages(detail);
            updateSessionIdDisplay();
            reloadConversations(detail.id);
        }).catch(function () {
            // ignore
        });
    }

    function startNewConversation() {
        if (isChatRequestPending) return;
        chatSessionId = null;
        activeConversationId = null;
        chatLastUpdated = null;
        var container = document.getElementById('sr-chat-messages');
        if (container) {
            container.innerHTML = '';
        }
        updateSessionIdDisplay();
        reloadConversations(null);
        var input = document.getElementById('sr-chat-input');
        if (input) input.focus();
    }

    function reloadConversations(preferredId) {
        getChatSessions().then(function (sessions) {
            if (preferredId) {
                activeConversationId = preferredId;
            }
            renderConversationList(sessions || []);
        }).catch(function () {
            renderConversationList([]);
        });
    }

    function getDocumentChunks(documentId) {
        return fetch(apiBase + '/documents/' + encodeURIComponent(documentId) + '/chunks').then(function (r) {
            if (r.status === 404) return [];
            if (!r.ok) throw new Error('Failed to load document chunks');
            return r.json();
        });
    }

    function loadDocumentDetails(documentId, fileName) {
        var panel = document.getElementById('sr-doc-details');
        var titleEl = document.getElementById('sr-doc-details-title');
        var subtitleEl = document.getElementById('sr-doc-details-subtitle');
        var chunksEl = document.getElementById('sr-doc-chunks');
        if (!panel || !titleEl || !subtitleEl || !chunksEl) return;

        titleEl.textContent = fileName || 'Document details';
        subtitleEl.textContent = 'Loading chunks...';
        chunksEl.innerHTML = '';
        panel.classList.add('sr-doc-details-visible');

        getDocumentChunks(documentId).then(function (chunks) {
            if (!chunks || chunks.length === 0) {
                subtitleEl.textContent = 'No chunks found for this document.';
                return;
            }
            subtitleEl.textContent = chunks.length + ' chunk(s)';
            chunks.forEach(function (chunk) {
                var div = document.createElement('div');
                div.className = 'sr-doc-chunk';
                var header = document.createElement('div');
                header.className = 'sr-doc-chunk-header';
                header.textContent = 'Chunk #' + (typeof chunk.chunkIndex === 'number' ? chunk.chunkIndex : '');
                var body = document.createElement('pre');
                body.className = 'sr-doc-chunk-body';
                body.textContent = chunk.content || '';
                div.appendChild(header);
                div.appendChild(body);
                chunksEl.appendChild(div);
            });
        }).catch(function (err) {
            subtitleEl.textContent = 'Error loading chunks: ' + (err.message || 'Request failed');
        });
    }

    function clearAllDocuments() {
        if (!confirm('Delete all documents (including schema documents)?')) {
            return;
        }

        fetch(apiBase + '/documents', { method: 'DELETE' })
            .then(function (r) {
                if (!r.ok) throw new Error('Failed to delete all documents');
                loadDocuments();
                loadSchemaDocuments();
                var panel = document.getElementById('sr-doc-details');
                if (panel) panel.classList.remove('sr-doc-details-visible');
            })
            .catch(function (err) {
                alert(err.message || 'Failed to delete all documents');
            });
    }

    function clearAllConversations() {
        if (!confirm('Clear all chat conversations?')) {
            return;
        }

        fetch(apiBase + '/chat/sessions', { method: 'DELETE' })
            .then(function (r) {
                if (!r.ok) throw new Error('Failed to clear conversations');
                activeConversationId = null;
                chatSessionId = null;
                chatLastUpdated = null;
                var messages = document.getElementById('sr-chat-messages');
                if (messages) {
                    messages.innerHTML = '';
                }
                updateSessionIdDisplay();
                reloadConversations(null);
            })
            .catch(function (err) {
                alert(err.message || 'Failed to clear conversations');
            });
    }

    var THEME_STORAGE_KEY = 'sr-theme';

    function getPreferredTheme() {
        var stored = null;
        try {
            stored = window.localStorage.getItem(THEME_STORAGE_KEY);
        } catch (e) {
        }
        if (stored === 'dark' || stored === 'light') {
            return stored;
        }
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    function updateThemeToggle(theme) {
        var btn = document.getElementById('sr-theme-toggle');
        if (!btn) return;
        var isDark = theme === 'dark';
        var moonIcon = '<span class="sr-theme-toggle-icon" aria-hidden="true"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="18" height="18"><path d="M21 12.79A9 9 0 0111.21 3 7 7 0 1019 14.79 9.05 9.05 0 0121 12.79z"/></svg></span>';
        var sunIcon = '<span class="sr-theme-toggle-icon" aria-hidden="true"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="18" height="18"><path d="M6.76 4.84L5.35 3.43 3.93 4.85l1.41 1.41L6.76 4.84zM1 11h3v2H1v-2zm10-7h2v3h-2V4zm7.07-.57l1.41 1.41-1.41 1.41-1.41-1.41 1.41-1.41zM17 11h3v2h-3v-2zm-5 4a4 4 0 110-8 4 4 0 010 8zm4.24 2.76l1.41 1.41 1.41-1.41-1.41-1.41-1.41 1.41zM4.22 17.66l1.41-1.41 1.41 1.41-1.41 1.41-1.41-1.41zM11 17h2v3h-2v-3z"/></svg></span>';
        btn.innerHTML = isDark ? sunIcon : moonIcon;
        var label = isDark ? 'Switch to light mode' : 'Switch to dark mode';
        btn.setAttribute('aria-label', label);
        btn.setAttribute('title', label);
    }

    function applyTheme(theme) {
        var root = document.documentElement;
        if (theme === 'dark') {
            root.setAttribute('data-theme', 'dark');
        } else {
            root.removeAttribute('data-theme');
            theme = 'light';
        }
        updateThemeToggle(theme);
    }

    document.addEventListener('DOMContentLoaded', function () {
        var initialTheme = getPreferredTheme();
        applyTheme(initialTheme);

        loadDocuments();
        loadSchemaDocuments();
        loadSupportedTypes();
        loadChatConfig();

        reloadConversations(null);
        updateSessionIdDisplay();

        document.querySelectorAll('.sr-nav-item').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var view = btn.getAttribute('data-view');
                if (view) switchView(view);
            });
        });

        var themeToggle = document.getElementById('sr-theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', function () {
                var isDark = document.documentElement.getAttribute('data-theme') === 'dark';
                var newTheme = isDark ? 'light' : 'dark';
                applyTheme(newTheme);
                try {
                    window.localStorage.setItem(THEME_STORAGE_KEY, newTheme);
                } catch (e) {
                }
            });
        }

        document.getElementById('sr-refresh-btn').addEventListener('click', function () {
            loadDocuments();
            loadSchemaDocuments();
        });

        var fileInput = document.getElementById('sr-file-input');
        var selectedFileNameEl = document.getElementById('sr-selected-file-name');
        if (fileInput && selectedFileNameEl) {
            fileInput.addEventListener('change', function () {
                var file = fileInput.files && fileInput.files[0];
                selectedFileNameEl.value = file ? file.name : '';
                selectedFileNameEl.placeholder = 'No file chosen';
                if (file && file.name) {
                    selectedFileNameEl.style.width = Math.min(60, Math.max(12, file.name.length + 2)) + 'ch';
                } else {
                    selectedFileNameEl.style.width = '';
                }
            });
        }

        document.getElementById('sr-upload-btn').addEventListener('click', function () {
            var fileInputEl = document.getElementById('sr-file-input');
            var statusEl = document.getElementById('sr-upload-status');
            var file = fileInputEl && fileInputEl.files && fileInputEl.files[0];
            if (!file) {
                if (statusEl) statusEl.textContent = 'Select a file.';
                return;
            }
            if (statusEl) statusEl.textContent = 'Uploading...';
            uploadDoc(file, 'dashboard').then(function () {
                if (statusEl) statusEl.textContent = 'Uploaded.';
                if (fileInputEl) fileInputEl.value = '';
                var display = document.getElementById('sr-selected-file-name');
                if (display) {
                    display.value = '';
                    display.placeholder = 'No file chosen';
                    display.style.width = '';
                }
                loadDocuments();
                loadSchemaDocuments();
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

        var sessionCopyBtn = document.getElementById('sr-chat-session-copy');
        if (sessionCopyBtn) {
            var copyIconHtml = '<span class="sr-copy-icon" aria-hidden="true"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><path d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12V1zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm0 16H8V7h11v14z"/></svg></span>';
            var checkIconHtml = '<span class="sr-copy-icon" aria-hidden="true"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/></svg></span>';
            sessionCopyBtn.addEventListener('click', function () {
                if (!chatSessionId) return;
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(chatSessionId).then(function () {
                        sessionCopyBtn.innerHTML = checkIconHtml;
                        sessionCopyBtn.setAttribute('title', 'Copied!');
                        setTimeout(function () {
                            sessionCopyBtn.innerHTML = copyIconHtml;
                            sessionCopyBtn.setAttribute('title', 'Copy session ID');
                        }, 1500);
                    });
                }
            });
        }

        var flagsToggle = document.getElementById('sr-chat-flags-toggle');
        var flagsDetail = document.getElementById('sr-chat-flags-detail');
        if (flagsToggle && flagsDetail) {
            flagsToggle.addEventListener('click', function () {
                var expanded = flagsDetail.classList.toggle('sr-chat-flags-detail-hidden');
                flagsToggle.setAttribute('aria-expanded', !expanded);
            });
        }

        var newBtn = document.getElementById('sr-chat-new');
        if (newBtn) {
            newBtn.addEventListener('click', startNewConversation);
        }

        var chatClearBtn = document.getElementById('sr-chat-clear-all');
        if (chatClearBtn) {
            chatClearBtn.addEventListener('click', clearAllConversations);
        }

        var chatLayout = document.getElementById('sr-chat-layout');
        var sidebarToggle = document.getElementById('sr-chat-sidebar-toggle');
        var sidebarBackdrop = document.getElementById('sr-chat-sidebar-backdrop');
        if (sidebarToggle && chatLayout) {
            sidebarToggle.addEventListener('click', function () {
                chatLayout.classList.toggle('sr-chat-sidebar-open');
            });
        }
        if (sidebarBackdrop && chatLayout) {
            sidebarBackdrop.addEventListener('click', function () {
                chatLayout.classList.remove('sr-chat-sidebar-open');
            });
        }
        function closeChatSidebar() {
            if (chatLayout) chatLayout.classList.remove('sr-chat-sidebar-open');
        }
        var sidebarCloseBtn = document.getElementById('sr-chat-sidebar-close');
        if (sidebarCloseBtn) sidebarCloseBtn.addEventListener('click', closeChatSidebar);
        var sidebarCloseMobile = document.getElementById('sr-chat-sidebar-close-mobile');
        if (sidebarCloseMobile) sidebarCloseMobile.addEventListener('click', closeChatSidebar);

        document.querySelectorAll('.sr-doc-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                var tabId = tab.getAttribute('data-doc-tab');
                if (tabId) {
                    switchDocTab(tabId);
                }
            });
        });

        var closeDetails = document.getElementById('sr-doc-details-close');
        if (closeDetails) {
            closeDetails.addEventListener('click', function () {
                var panel = document.getElementById('sr-doc-details');
                if (panel) panel.classList.remove('sr-doc-details-visible');
            });
        }

        var closeSourceDetails = document.getElementById('sr-source-details-close');
        if (closeSourceDetails) {
            closeSourceDetails.addEventListener('click', closeSourceDetail);
        }

        var clearDocsBtn = document.getElementById('sr-doc-clear-all');
        if (clearDocsBtn) {
            clearDocsBtn.addEventListener('click', clearAllDocuments);
        }
    });
})();
