// Main JavaScript file for SmartRAG documentation

document.addEventListener('DOMContentLoaded', function() {
    // Initialize syntax highlighting
    initSyntaxHighlighting();
    
    // Initialize search functionality
    initSearch();
    
    // Initialize smooth scrolling
    initSmoothScrolling();
    
    // Initialize copy code buttons
    initCopyCodeButtons();
});

function initSyntaxHighlighting() {
    // Add syntax highlighting to code blocks
    const codeBlocks = document.querySelectorAll('pre code');
    codeBlocks.forEach(block => {
        block.classList.add('hljs');
    });
}

function initSearch() {
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        searchInput.addEventListener('input', function(e) {
            const query = e.target.value.toLowerCase();
            performSearch(query);
        });
    }
}

function performSearch(query) {
    const searchResults = document.querySelector('.search-results');
    if (!searchResults) return;
    
    if (query.length < 2) {
        searchResults.style.display = 'none';
        return;
    }
    
    // Simple search implementation
    const searchableContent = document.querySelectorAll('h1, h2, h3, h4, h5, h6, p, li');
    const results = [];
    
    searchableContent.forEach(element => {
        const text = element.textContent.toLowerCase();
        if (text.includes(query)) {
            results.push({
                element: element,
                text: element.textContent,
                relevance: calculateRelevance(text, query)
            });
        }
    });
    
    // Sort by relevance
    results.sort((a, b) => b.relevance - a.relevance);
    
    displaySearchResults(results.slice(0, 10));
}

function calculateRelevance(text, query) {
    let relevance = 0;
    const words = query.split(' ');
    
    words.forEach(word => {
        if (text.includes(word)) {
            relevance += 1;
            // Bonus for exact matches
            if (text.indexOf(word) === 0) relevance += 0.5;
        }
    });
    
    return relevance;
}

function displaySearchResults(results) {
    const searchResults = document.querySelector('.search-results');
    if (!searchResults) return;
    
    if (results.length === 0) {
        searchResults.innerHTML = '<p>No results found</p>';
        searchResults.style.display = 'block';
        return;
    }
    
    const html = results.map(result => 
        `<div class="search-result">
            <a href="#${result.element.id || ''}" onclick="scrollToElement('${result.element.id || ''}')">
                ${result.text.substring(0, 100)}...
            </a>
        </div>`
    ).join('');
    
    searchResults.innerHTML = html;
    searchResults.style.display = 'block';
}

function scrollToElement(elementId) {
    if (!elementId) return;
    
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
    }
}

function initSmoothScrolling() {
    // Smooth scrolling for internal links
    const internalLinks = document.querySelectorAll('a[href^="#"]');
    internalLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const targetId = this.getAttribute('href').substring(1);
            const targetElement = document.getElementById(targetId);
            
            if (targetElement) {
                targetElement.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });
}

function initCopyCodeButtons() {
    // Add copy buttons to code blocks
    const codeBlocks = document.querySelectorAll('pre');
    codeBlocks.forEach(block => {
        const copyButton = document.createElement('button');
        copyButton.className = 'copy-code-btn';
        copyButton.textContent = 'Copy';
        copyButton.onclick = () => copyCode(block);
        
        block.style.position = 'relative';
        block.appendChild(copyButton);
    });
}

function copyCode(codeBlock) {
    const code = codeBlock.querySelector('code');
    if (!code) return;
    
    navigator.clipboard.writeText(code.textContent).then(() => {
        const button = codeBlock.querySelector('.copy-code-btn');
        button.textContent = 'Copied!';
        button.style.backgroundColor = '#28a745';
        
        setTimeout(() => {
            button.textContent = 'Copy';
            button.style.backgroundColor = '';
        }, 2000);
    }).catch(err => {
        console.error('Failed to copy code:', err);
    });
}

// Add CSS for copy button
const style = document.createElement('style');
style.textContent = `
    .copy-code-btn {
        position: absolute;
        top: 8px;
        right: 8px;
        background: #0366d6;
        color: white;
        border: none;
        border-radius: 4px;
        padding: 4px 8px;
        font-size: 12px;
        cursor: pointer;
        opacity: 0;
        transition: opacity 0.2s;
    }
    
    pre:hover .copy-code-btn {
        opacity: 1;
    }
    
    .search-results {
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: white;
        border: 1px solid #e1e4e8;
        border-radius: 6px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        max-height: 400px;
        overflow-y: auto;
        z-index: 1000;
        display: none;
    }
    
    .search-result {
        padding: 8px 12px;
        border-bottom: 1px solid #f1f1f1;
    }
    
    .search-result:last-child {
        border-bottom: none;
    }
    
    .search-result a {
        color: #0366d6;
        text-decoration: none;
    }
    
    .search-result a:hover {
        text-decoration: underline;
    }
`;
document.head.appendChild(style);
