// SmartRAG Documentation - Modern JavaScript

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing SmartRAG documentation...');
    
    // Theme Toggle Functionality
    const themeToggle = document.getElementById('themeToggle');
    const themeIcon = document.getElementById('themeIcon');
    const body = document.body;
    
    console.log('Theme toggle found:', themeToggle);
    console.log('Theme icon found:', themeIcon);
    
    if (themeToggle) {
        console.log('Setting up theme toggle...');
        themeToggle.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.log('Theme toggle clicked!');
            
            const currentTheme = body.getAttribute('data-theme') || 'light';
            const newTheme = currentTheme === 'light' ? 'dark' : 'light';
            
            if (newTheme === 'dark') {
                body.setAttribute('data-theme', 'dark');
                if (themeIcon) {
                    themeIcon.className = 'fas fa-moon';
                }
                localStorage.setItem('theme', 'dark');
                console.log('Dark theme applied');
            } else {
                body.removeAttribute('data-theme');
                if (themeIcon) {
                    themeIcon.className = 'fas fa-sun';
                }
                localStorage.setItem('theme', 'light');
                console.log('Light theme applied');
            }
        });
        
        // Apply saved theme
        const savedTheme = localStorage.getItem('theme');
        if (savedTheme === 'dark') {
            body.setAttribute('data-theme', 'dark');
            if (themeIcon) {
                themeIcon.className = 'fas fa-moon';
            }
        }
    } else {
        console.warn('Theme toggle button not found!');
    }
    
    // Tab Functionality for Configuration Pages
    const codeTabs = document.querySelectorAll('.code-tab');
    const codePanels = document.querySelectorAll('.code-panel');
    
    if (codeTabs.length > 0) {
        console.log('Setting up code tabs...');
        
        codeTabs.forEach(tab => {
            tab.addEventListener('click', function() {
                const targetTab = this.getAttribute('data-tab');
                
                // Remove active class from all tabs and panels
                codeTabs.forEach(t => t.classList.remove('active'));
                codePanels.forEach(p => p.classList.remove('active'));
                
                // Add active class to clicked tab
                this.classList.add('active');
                
                // Show corresponding panel
                const targetPanel = document.getElementById(targetTab);
                if (targetPanel) {
                    targetPanel.classList.add('active');
                }
                
                console.log('Tab switched to:', targetTab);
            });
        });
    }
    
    // Language Selection Functionality
    const languageButtons = document.querySelectorAll('.language-btn');
    
    if (languageButtons.length > 0) {
        console.log('Setting up language selection...');
        
        languageButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                
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
                
                console.log('Final URL:', newPath);
                
                // Navigate to new URL
                window.location.href = newPath;
            });
        });
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
                console.warn('Back to top scroll error:', error);
            }
        });
        
        // Scroll to top when button is clicked
        backToTopButton.addEventListener('click', function() {
            try {
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            } catch (error) {
                console.warn('Back to top click error:', error);
                // Fallback for older browsers
                window.scrollTo(0, 0);
            }
        });
    }
    
    // Initialize any additional components
    console.log('SmartRAG documentation initialization complete!');
});