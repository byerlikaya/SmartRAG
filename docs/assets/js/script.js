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
    
    // Language Selection Functionality
    const languageDropdown = document.getElementById('languageDropdown');
    const dropdownMenu = document.querySelector('.dropdown-menu');
    const languageLinks = document.querySelectorAll('.language-link');
    
    console.log('Language dropdown found:', languageDropdown);
    console.log('Dropdown menu found:', dropdownMenu);
    console.log('Language links found:', languageLinks.length);
    
    if (languageDropdown && dropdownMenu) {
        console.log('Setting up language dropdown...');
        
        // Toggle dropdown on click
        languageDropdown.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.log('Language dropdown clicked!');
            
            const isOpen = dropdownMenu.classList.contains('show');
            if (isOpen) {
                dropdownMenu.classList.remove('show');
                console.log('Closing dropdown');
            } else {
                dropdownMenu.classList.add('show');
                console.log('Opening dropdown');
            }
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!languageDropdown.contains(e.target) && !dropdownMenu.contains(e.target)) {
                dropdownMenu.classList.remove('show');
                console.log('Closing dropdown (click outside)');
            }
        });
        
        // Language link click handlers
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
                    
                    console.log('Final URL:', newPath);
                    
                    // Navigate to new URL
                    window.location.href = newPath;
                    
                } catch (error) {
                    console.error('Language switch error:', error);
                }
            });
        });
    } else {
        console.warn('Language dropdown elements not found!');
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