// SmartRAG Documentation - Interactive Features

document.addEventListener('DOMContentLoaded', function() {
    console.log('SmartRAG Documentation initialized');
    
    // Initialize all features
    initThemeToggle();
    initLanguagePreference();
    initBackToTop();
    initSmoothScroll();
    initCodeTabs();
    initAnimations();
    initMobileDropdown();
    initMobileMenuClose();
    
    // Initialize Prism.js if available
    if (typeof Prism !== 'undefined') {
        Prism.highlightAll();
    }
});

// ===== THEME TOGGLE =====
function initThemeToggle() {
    const themeToggle = document.getElementById('themeToggle');
    const htmlElement = document.documentElement;
    const iconDark = document.querySelector('.theme-icon-dark');
    const iconLight = document.querySelector('.theme-icon-light');
    
    if (!themeToggle) return;
    
    // Load saved theme or default to light
    const savedTheme = localStorage.getItem('smartrag-theme') || 'light';
    setTheme(savedTheme);
    
    // Theme toggle click handler
    themeToggle.addEventListener('click', function() {
        const currentTheme = htmlElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        setTheme(newTheme);
    });
    
    function setTheme(theme) {
        htmlElement.setAttribute('data-theme', theme);
        localStorage.setItem('smartrag-theme', theme);
        
        // Update toggle button icons
        if (theme === 'dark') {
            if (iconDark) iconDark.style.display = 'none';
            if (iconLight) iconLight.style.display = 'inline-block';
        } else {
            if (iconDark) iconDark.style.display = 'inline-block';
            if (iconLight) iconLight.style.display = 'none';
        }
    }
}

// ===== LANGUAGE PREFERENCE =====
function initLanguagePreference() {
    const languageButtons = document.querySelectorAll('.language-btn');
    
    languageButtons.forEach(btn => {
        btn.addEventListener('click', function(e) {
            const lang = this.getAttribute('data-lang');
            if (lang) {
                localStorage.setItem('smartrag-language', lang);
            }
        });
    });
}

// ===== BACK TO TOP BUTTON =====
function initBackToTop() {
    const backToTop = document.querySelector('.back-to-top');
    if (!backToTop) return;
    
    // Show/hide on scroll
        window.addEventListener('scroll', function() {
            if (window.pageYOffset > 300) {
            backToTop.classList.add('show');
            } else {
            backToTop.classList.remove('show');
            }
        });
        
    // Scroll to top on click
    backToTop.addEventListener('click', function(e) {
        e.preventDefault();
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });
}

// ===== SMOOTH SCROLL =====
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;
            
            const target = document.querySelector(targetId);
            if (target) {
            e.preventDefault();
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

// ===== CODE TABS =====
function initCodeTabs() {
    const codeTabs = document.querySelectorAll('.code-tab');
    
    codeTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            const container = this.closest('.code-tabs').parentElement;
            
            // Remove active class from all tabs and panels in this container
            container.querySelectorAll('.code-tab').forEach(t => t.classList.remove('active'));
            container.querySelectorAll('.code-panel').forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding panel
            this.classList.add('active');
            const targetPanel = container.querySelector(`.code-panel[data-tab="${targetTab}"]`);
            if (targetPanel) {
                targetPanel.classList.add('active');
                
                // Re-highlight code in the newly visible panel
                if (typeof Prism !== 'undefined') {
                    Prism.highlightAllUnder(targetPanel);
                }
            }
        });
    });
}

// ===== ANIMATIONS =====
function initAnimations() {
    // Intersection Observer for fade-in animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in-up');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);
    
    // Observe elements
    const elementsToAnimate = document.querySelectorAll('.feature-card, .provider-card, .stat-card, .alert, details');
    elementsToAnimate.forEach(el => {
        observer.observe(el);
    });
}

// ===== COPY CODE BUTTON =====
function addCopyButtons() {
    const codeBlocks = document.querySelectorAll('pre code');
    
    codeBlocks.forEach((codeBlock) => {
        const pre = codeBlock.parentElement;
        const button = document.createElement('button');
        button.className = 'copy-code-btn';
        button.innerHTML = '<i class="fas fa-copy"></i>';
        button.title = 'Copy code';
        
        button.addEventListener('click', async function() {
            const code = codeBlock.textContent;
            try {
                await navigator.clipboard.writeText(code);
                button.innerHTML = '<i class="fas fa-check"></i>';
                button.style.color = '#10b981';
                
                setTimeout(() => {
                    button.innerHTML = '<i class="fas fa-copy"></i>';
                    button.style.color = '';
                }, 2000);
            } catch (err) {
                console.error('Failed to copy:', err);
            }
        });
        
        pre.style.position = 'relative';
        pre.appendChild(button);
    });
}

// Add copy buttons after page load
setTimeout(addCopyButtons, 500);

// ===== SEARCH FUNCTIONALITY (Optional for future) =====
function initSearch() {
    const searchInput = document.getElementById('searchInput');
    if (!searchInput) return;
    
    searchInput.addEventListener('input', function(e) {
        const query = e.target.value.toLowerCase();
        // Future: Implement client-side search
        console.log('Search query:', query);
    });
}

// ===== MOBILE MENU CLOSE ON CLICK =====
function initMobileMenuClose() {
    // Mobile'da sadece gerçek linklere tıklandığında menüyü kapat
    // Dropdown toggle'ları hariç tut
    document.querySelectorAll('.navbar-nav > .nav-item:not(.dropdown) > .nav-link').forEach(link => {
        link.addEventListener('click', function(e) {
            const navbarCollapse = document.querySelector('.navbar-collapse');
            const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
            if (bsCollapse && window.innerWidth < 992) {
                bsCollapse.hide();
            }
        });
    });
}

// ===== MOBILE DROPDOWN SUPPORT =====
function initMobileDropdown() {
    // Manual dropdown toggle for mobile devices
    document.querySelectorAll('.dropdown-toggle').forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            // Only handle on mobile
            if (window.innerWidth >= 992) return;
            
            e.preventDefault();
            e.stopPropagation();
            
            const dropdown = this.closest('.dropdown');
            const isActive = dropdown.classList.contains('show');
            
            // Close all dropdowns
            document.querySelectorAll('.dropdown').forEach(d => {
                d.classList.remove('show');
            });
            
            // Toggle current dropdown
            if (!isActive) {
                dropdown.classList.add('show');
            }
        });
    });
    
    // Close mobile menu when a dropdown item is clicked
    document.querySelectorAll('.dropdown-menu .dropdown-item').forEach(item => {
        item.addEventListener('click', function(e) {
            const navbarCollapse = document.querySelector('.navbar-collapse');
            const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
            if (bsCollapse && window.innerWidth < 992) {
                bsCollapse.hide();
            }
        });
    });
    
    // Close dropdown when clicking outside
    document.addEventListener('click', function(e) {
        if (window.innerWidth >= 992) return;
        
        if (!e.target.closest('.dropdown')) {
            document.querySelectorAll('.dropdown').forEach(d => {
                d.classList.remove('show');
            });
        }
    });
}

// ===== NAVBAR SCROLL EFFECT =====
let lastScroll = 0;
window.addEventListener('scroll', function() {
    const navbar = document.querySelector('.navbar');
    const currentScroll = window.pageYOffset;
    
    if (currentScroll > 100) {
        navbar.style.boxShadow = 'var(--shadow-md)';
    } else {
        navbar.style.boxShadow = 'var(--shadow-sm)';
    }
    
    lastScroll = currentScroll;
});

// ===== EXTERNAL LINKS IN NEW TAB =====
document.querySelectorAll('a[href^="http"]').forEach(link => {
    if (!link.hostname.includes('byerlikaya.github.io')) {
        link.setAttribute('target', '_blank');
        link.setAttribute('rel', 'noopener noreferrer');
    }
});

console.log('SmartRAG Documentation ready!');

