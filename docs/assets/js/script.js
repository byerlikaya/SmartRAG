// SmartRAG Documentation - Modern JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Theme Toggle Functionality
    const themeToggle = document.getElementById('themeToggle');
    const themeIcon = document.getElementById('themeIcon');
    const body = document.body;
    
    console.log('Theme toggle found:', themeToggle);
    console.log('Theme icon found:', themeIcon);
    
    // Check for saved theme preference or default to 'light'
    const currentTheme = localStorage.getItem('theme') || 'light';
    console.log('Current theme from localStorage:', currentTheme);
    
    // Apply the current theme
    function applyTheme(theme) {
        try {
            console.log('Applying theme:', theme);
            if (theme === 'dark') {
                body.setAttribute('data-theme', 'dark');
                if (themeIcon) {
                    themeIcon.className = 'fas fa-moon';
                    console.log('Dark theme applied, icon changed to moon');
                }
                localStorage.setItem('theme', 'dark');
            } else {
                body.removeAttribute('data-theme');
                if (themeIcon) {
                    themeIcon.className = 'fas fa-sun';
                    console.log('Light theme applied, icon changed to sun');
                }
                localStorage.setItem('theme', 'light');
            }
        } catch (error) {
            console.warn('Theme application error:', error);
        }
    }
    
    // Initialize theme immediately to prevent flash
    applyTheme(currentTheme);
    
    // Also apply theme on page load to ensure persistence
    window.addEventListener('load', function() {
        const savedTheme = localStorage.getItem('theme') || 'light';
        console.log('Page loaded, applying saved theme:', savedTheme);
        applyTheme(savedTheme);
    });
    
    // Theme toggle click handler
    if (themeToggle) {
        console.log('Setting up theme toggle click handler');
        themeToggle.addEventListener('click', function() {
            try {
                console.log('Theme toggle clicked!');
                const currentTheme = localStorage.getItem('theme') || 'light';
                const newTheme = currentTheme === 'light' ? 'dark' : 'light';
                console.log('Switching from', currentTheme, 'to', newTheme);
                applyTheme(newTheme);
            } catch (error) {
                console.warn('Theme toggle error:', error);
            }
        });
    } else {
        console.warn('Theme toggle button not found!');
    }
    
    // Back to Top Button
    const backToTopButton = document.getElementById('backToTop');
    
    if (backToTopButton) {
        // Show button when scrolling down
        window.addEventListener('scroll', function() {
            try {
                if (window.pageYOffset > 300) {
                    backToTopButton.classList.add('show');
                } else {
                    backToTopButton.classList.remove('show');
                }
            } catch (error) {
                console.warn('Scroll handler error:', error);
            }
        });
        
        // Scroll to top when clicked
        backToTopButton.addEventListener('click', function() {
            try {
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            } catch (error) {
                console.warn('Scroll to top error:', error);
                // Fallback for older browsers
                window.scrollTo(0, 0);
            }
        });
    }
    
    // Language Selection Functionality - Manual dropdown control
    const languageDropdown = document.getElementById('languageDropdown');
    const languageDropdownMenu = document.querySelector('#languageDropdown + .dropdown-menu');
    const languageLinks = document.querySelectorAll('.language-link');
    
    console.log('Language dropdown found:', languageDropdown);
    console.log('Language dropdown menu found:', languageDropdownMenu);
    console.log('Language links found:', languageLinks.length);
    
    if (languageDropdown && languageDropdownMenu && languageLinks.length > 0) {
        console.log('Setting up manual language dropdown...');
        
        // Toggle dropdown on click
        languageDropdown.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const isOpen = languageDropdownMenu.classList.contains('show');
            if (isOpen) {
                languageDropdownMenu.classList.remove('show');
            } else {
                languageDropdownMenu.classList.add('show');
            }
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!languageDropdown.contains(e.target) && !languageDropdownMenu.contains(e.target)) {
                languageDropdownMenu.classList.remove('show');
            }
        });
        
        // Add click handlers to language links
        languageLinks.forEach((link, index) => {
            console.log(`Adding click handler to language link ${index}:`, link);
            
            link.addEventListener('click', function(e) {
                try {
                    e.preventDefault();
                    e.stopPropagation();
                    
                    const targetLang = this.getAttribute('data-lang');
                    const currentPath = window.location.pathname;
                    
                    console.log('Language switch requested:', targetLang);
                    console.log('Current path:', currentPath);
                    
                    // Get base URL from Jekyll site configuration
                    const baseUrl = window.siteConfig ? window.siteConfig.baseurl : '/SmartRAG';
                    console.log('Base URL:', baseUrl);
                    
                    // Remove baseurl and current language prefix if exists
                    let newPath = currentPath;
                    
                    // Remove baseurl if present
                    if (baseUrl && baseUrl !== '/') {
                        newPath = currentPath.replace(new RegExp(`^${baseUrl}`), '');
                        console.log('Path after removing baseurl:', newPath);
                    }
                    
                    // Remove current language prefix
                    newPath = newPath.replace(/^\/(en|tr|de|ru)/, '');
                    console.log('Path after removing language prefix:', newPath);
                    
                    // Ensure path starts with /
                    if (!newPath.startsWith('/')) {
                        newPath = '/' + newPath;
                    }
                    
                    // Add new language prefix
                    if (newPath === '/') {
                        newPath = `/${targetLang}/`;
                    } else {
                        newPath = `/${targetLang}${newPath}`;
                    }
                    console.log('Path after adding language prefix:', newPath);
                    
                    // Add baseurl back
                    if (baseUrl && baseUrl !== '/') {
                        newPath = baseUrl + newPath;
                    }
                    
                    console.log('Final new path:', newPath);
                    
                    // Close dropdown
                    languageDropdownMenu.classList.remove('show');
                    
                    // Navigate to new language version
                    window.location.href = newPath;
                } catch (error) {
                    console.error('Language switch error:', error);
                }
            });
        });
        
        console.log('Manual language dropdown setup completed');
    } else {
        console.warn('Language dropdown elements not found');
        if (!languageDropdown) console.warn('languageDropdown element not found');
        if (!languageDropdownMenu) console.warn('languageDropdownMenu element not found');
        if (languageLinks.length === 0) console.warn('No language-link elements found');
    }
    
    // Smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            try {
                const href = this.getAttribute('href');
                
                // Skip if href is just "#" or empty
                if (!href || href === '#' || href === '#!') {
                    return;
                }
                
                e.preventDefault();
                const target = document.querySelector(href);
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            } catch (error) {
                console.warn('Smooth scroll error:', error);
            }
        });
    });
    
    // Code Tabs Functionality
    const codeTabs = document.querySelectorAll('.code-tab');
    const codePanels = document.querySelectorAll('.code-panel');
    
    codeTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            
            // Remove active class from all tabs and panels
            codeTabs.forEach(t => t.classList.remove('active'));
            codePanels.forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            const targetPanel = document.getElementById(targetTab);
            if (targetPanel) {
                targetPanel.classList.add('active');
            }
        });
    });
    
    // Provider Tabs Functionality
    const providerTabs = document.querySelectorAll('.provider-tab');
    const providerPanels = document.querySelectorAll('.provider-panel');
    
    providerTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            
            // Remove active class from all tabs and panels
            providerTabs.forEach(t => t.classList.remove('active'));
            providerPanels.forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            const targetPanel = document.getElementById(targetTab);
            if (targetPanel) {
                targetPanel.classList.add('active');
            }
        });
    });
    
    // Storage Tabs Functionality
    const storageTabs = document.querySelectorAll('.storage-tab');
    const storagePanels = document.querySelectorAll('.storage-panel');
    
    storageTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            
            // Remove active class from all tabs and panels
            storageTabs.forEach(t => t.classList.remove('active'));
            storagePanels.forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            const targetPanel = document.getElementById(targetTab);
            if (targetPanel) {
                targetPanel.classList.add('active');
            }
        });
    });
    
    // Intersection Observer for animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);
    
    // Observe elements for animation
    const animatedElements = document.querySelectorAll('.feature-card, .provider-card, .doc-card, .step');
    animatedElements.forEach(el => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(30px)';
        el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        observer.observe(el);
    });
    
    // Language switching enhancement
    // This section is now handled by the manual dropdown control above.
    
    // Navbar scroll effect
    const navbar = document.querySelector('.navbar');
    if (navbar) {
        window.addEventListener('scroll', function() {
            if (window.scrollY > 50) {
                navbar.style.background = 'rgba(255, 255, 255, 0.98)';
                navbar.style.boxShadow = '0 4px 6px -1px rgba(0, 0, 0, 0.1)';
            } else {
                navbar.style.background = 'rgba(255, 255, 255, 0.95)';
                navbar.style.boxShadow = 'none';
            }
        });
    }
    
    // Copy code functionality
    const codeBlocks = document.querySelectorAll('pre code');
    codeBlocks.forEach(block => {
        const button = document.createElement('button');
        button.className = 'copy-btn';
        button.innerHTML = '<i class="fas fa-copy"></i>';
        button.title = 'Copy code';
        
        button.addEventListener('click', function() {
            navigator.clipboard.writeText(block.textContent).then(() => {
                button.innerHTML = '<i class="fas fa-check"></i>';
                button.style.color = '#10b981';
                setTimeout(() => {
                    button.innerHTML = '<i class="fas fa-copy"></i>';
                    button.style.color = '';
                }, 2000);
            });
        });
        
        block.parentElement.style.position = 'relative';
        block.parentElement.appendChild(button);
    });
    
    // Parallax effect for hero background
    const heroBackground = document.querySelector('.hero-background');
    if (heroBackground) {
        window.addEventListener('scroll', function() {
            const scrolled = window.pageYOffset;
            const rate = scrolled * -0.5;
            heroBackground.style.transform = `translateY(${rate}px)`;
        });
    }
    
    // Add loading animation
    window.addEventListener('load', function() {
        document.body.classList.add('loaded');
    });
});

// Add CSS for copy button
const style = document.createElement('style');
style.textContent = `
    .copy-btn {
        position: absolute;
        top: 10px;
        right: 10px;
        background: rgba(0, 0, 0, 0.1);
        border: none;
        border-radius: 4px;
        padding: 8px;
        cursor: pointer;
        opacity: 0;
        transition: opacity 0.3s ease;
        color: #6b7280;
    }
    
    pre:hover .copy-btn {
        opacity: 1;
    }
    
    .copy-btn:hover {
        background: rgba(0, 0, 0, 0.2);
    }
    
    body.loaded {
        animation: fadeIn 0.5s ease-in;
    }
    
    @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
    }
`;
document.head.appendChild(style);