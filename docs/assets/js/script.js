// SmartRAG Documentation - Clean JavaScript

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing SmartRAG documentation...');
    
    // Initialize Prism.js syntax highlighting
    if (typeof Prism !== 'undefined') {
        console.log('Initializing Prism.js syntax highlighting...');
        Prism.highlightAll();
    } else {
        console.warn('Prism.js not loaded');
    }
    
    // Tab Functionality for Code Examples
    const codeTabs = document.querySelectorAll('.code-tab');
    codeTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const target = this.getAttribute('data-target');
            const tabContainer = this.closest('.code-example');
            
            // Remove active class from all tabs and panels
            tabContainer.querySelectorAll('.code-tab').forEach(t => t.classList.remove('active'));
            tabContainer.querySelectorAll('.code-panel').forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            tabContainer.querySelector(`.code-panel[data-panel="${target}"]`).classList.add('active');
        });
    });
    
    // Provider Tabs Functionality
    const providerTabs = document.querySelectorAll('.provider-tab');
    providerTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const target = this.getAttribute('data-target');
            const tabContainer = this.closest('.provider-content');
            
            // Remove active class from all tabs and panels
            tabContainer.querySelectorAll('.provider-tab').forEach(t => t.classList.remove('active'));
            tabContainer.querySelectorAll('.provider-panel').forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            tabContainer.querySelector(`.provider-panel[data-panel="${target}"]`).classList.add('active');
        });
    });
    
    // Storage Tabs Functionality
    const storageTabs = document.querySelectorAll('.storage-tab');
    storageTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const target = this.getAttribute('data-target');
            const tabContainer = this.closest('.storage-content');
            
            // Remove active class from all tabs and panels
            tabContainer.querySelectorAll('.storage-tab').forEach(t => t.classList.remove('active'));
            tabContainer.querySelectorAll('.storage-panel').forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            tabContainer.querySelector(`.storage-panel[data-panel="${target}"]`).classList.add('active');
        });
    });
    
    // Language Dropdown Functionality
    const languageDropdown = document.getElementById('languageDropdown');
    if (languageDropdown) {
        languageDropdown.addEventListener('click', function(e) {
            e.preventDefault();
            const dropdownMenu = this.querySelector('.dropdown-menu');
            dropdownMenu.classList.toggle('show');
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!languageDropdown.contains(e.target)) {
                const dropdownMenu = languageDropdown.querySelector('.dropdown-menu');
                dropdownMenu.classList.remove('show');
            }
        });
    }
    
    // Back to Top Button
    const backToTopBtn = document.querySelector('.back-to-top');
    if (backToTopBtn) {
        window.addEventListener('scroll', function() {
            if (window.pageYOffset > 300) {
                backToTopBtn.classList.add('show');
            } else {
                backToTopBtn.classList.remove('show');
            }
        });
        
        backToTopBtn.addEventListener('click', function() {
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });
    }
    
    // Smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
    
    console.log('SmartRAG documentation initialized successfully');
});