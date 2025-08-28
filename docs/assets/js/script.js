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