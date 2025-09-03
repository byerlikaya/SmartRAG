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
            const target = this.getAttribute('data-tab');
            const tabContainer = this.closest('.code-example');
            
            if (!tabContainer) {
                console.warn('Tab container not found');
                return;
            }
            
            // Remove active class from all tabs and panels
            tabContainer.querySelectorAll('.code-tab').forEach(t => t.classList.remove('active'));
            tabContainer.querySelectorAll('.code-panel').forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            const targetPanel = tabContainer.querySelector(`.code-panel[data-tab="${target}"]`);
            if (targetPanel) {
                targetPanel.classList.add('active');
            } else {
                console.warn(`Panel with data-tab="${target}" not found`);
            }
        });
    });
    
    // Provider Tabs Functionality
    const providerTabs = document.querySelectorAll('.provider-tab');
    providerTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const target = this.getAttribute('data-tab');
            const tabContainer = this.closest('.provider-content');
            
            if (!tabContainer) {
                console.warn('Provider tab container not found');
                return;
            }
            
            // Remove active class from all tabs and panels
            tabContainer.querySelectorAll('.provider-tab').forEach(t => t.classList.remove('active'));
            tabContainer.querySelectorAll('.provider-panel').forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            const targetPanel = tabContainer.querySelector(`.provider-panel[data-tab="${target}"]`);
            if (targetPanel) {
                targetPanel.classList.add('active');
            } else {
                console.warn(`Provider panel with data-tab="${target}" not found`);
            }
        });
    });
    
    // Storage Tabs Functionality
    const storageTabs = document.querySelectorAll('.storage-tab');
    storageTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const target = this.getAttribute('data-tab');
            const tabContainer = this.closest('.storage-content');
            
            if (!tabContainer) {
                console.warn('Storage tab container not found');
                return;
            }
            
            // Remove active class from all tabs and panels
            tabContainer.querySelectorAll('.storage-tab').forEach(t => t.classList.remove('active'));
            tabContainer.querySelectorAll('.storage-panel').forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            const targetPanel = tabContainer.querySelector(`.storage-panel[data-tab="${target}"]`);
            if (targetPanel) {
                targetPanel.classList.add('active');
            } else {
                console.warn(`Storage panel with data-tab="${target}" not found`);
            }
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