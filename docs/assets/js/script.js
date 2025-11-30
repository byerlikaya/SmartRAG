// SmartRAG Documentation - Interactive Features

document.addEventListener('DOMContentLoaded', function() {
    console.log('SmartRAG Documentation initialized');
    
    // Close all dropdowns by default (both mobile and desktop)
    // Force remove .show class and ensure dropdowns are hidden
    document.querySelectorAll('.nav-item.dropdown').forEach(dropdown => {
        dropdown.classList.remove('show');
        const menu = dropdown.querySelector('.dropdown-menu');
        if (menu) {
            menu.style.display = 'none';
            menu.classList.remove('force-show');
        }
    });
    
    // Initialize all features
    initThemeToggle();
    initLanguagePreference();
    initBackToTop();
    initSmoothScroll();
    initCodeTabs();
    initAnimations();
    initMobileDropdown();
    initMobileMenuClose();
    initDesktopDropdown();
    
    // Initialize Prism.js if available
    if (typeof Prism !== 'undefined') {
        Prism.highlightAll();
    }
    
    // Wrap accordion buttons in config-section for better styling (do this first)
    wrapAccordionButtons();
    
    // Wrap Getting Started page sections in card-like containers (do this after accordion wrapping)
    // This ensures accordion content is not wrapped by wrapGettingStartedSections
    setTimeout(() => {
        wrapGettingStartedSections();
    }, 100);
});

// ===== ACCORDION BUTTONS WRAPPER =====
function wrapAccordionButtons() {
    // Only apply to version history page
    const pathname = window.location.pathname;
    if (!pathname.includes('changelog/version-history') && !pathname.includes('changelog')) {
        return;
    }
    
    const accordionButtons = document.querySelectorAll('.accordion-button:not(.config-section-accordion *)');
    
    accordionButtons.forEach(button => {
        // Skip if already wrapped
        if (button.closest('.config-section-accordion')) {
            return;
        }
        
        // Get the parent accordion-header
        const accordionHeader = button.closest('.accordion-header');
        if (!accordionHeader) {
            return;
        }
        
        // Get the parent accordion-item
        const accordionItem = button.closest('.accordion-item');
        if (!accordionItem) {
            return;
        }
        
        // Get the accordion-collapse and accordion-body
        const accordionCollapse = accordionItem.querySelector('.accordion-collapse');
        const accordionBody = accordionCollapse ? accordionCollapse.querySelector('.accordion-body') : null;
        
        // Create wrapper for the button and body content
        const wrapper = document.createElement('div');
        wrapper.className = 'config-section config-section-accordion';
        
        // Function to recursively remove config-section wrappers (define first)
        const removeConfigSections = (container) => {
            if (!container) return;
            
            // Get all config-sections that are NOT the accordion wrapper itself
            const allConfigSections = Array.from(container.querySelectorAll('.config-section')).filter(section => 
                !section.classList.contains('config-section-accordion') && 
                section !== wrapper
            );
            
            // Process in reverse order to handle nested sections correctly
            allConfigSections.reverse().forEach(section => {
                // Skip if this section is inside another config-section that will be removed
                const parentConfigSection = section.parentElement?.closest('.config-section:not(.config-section-accordion)');
                if (parentConfigSection && allConfigSections.includes(parentConfigSection)) {
                    return; // Will be handled when parent is processed
                }
                
                // Move all children out of config-section
                const fragment = document.createDocumentFragment();
                while (section.firstChild) {
                    fragment.appendChild(section.firstChild);
                }
                // Insert children before the section
                if (section.parentNode) {
                    section.parentNode.insertBefore(fragment, section);
                    // Remove the empty config-section
                    section.remove();
                }
            });
        };
        
        // Pre-move content to wrapper (but keep it hidden until accordion opens)
        // This prevents the lag where content appears before background extends
        let contentContainer = null;
        if (accordionBody && accordionBody.children.length > 0) {
            // Create a container for content that can be hidden/shown
            contentContainer = document.createElement('div');
            contentContainer.className = 'accordion-content-container';
            contentContainer.style.display = accordionCollapse.classList.contains('show') ? 'block' : 'none';
            
            // Pre-move content to content container
            const fragment = document.createDocumentFragment();
            while (accordionBody.firstChild) {
                fragment.appendChild(accordionBody.firstChild);
            }
            contentContainer.appendChild(fragment);
            
            // Remove config-sections from pre-moved content
            removeConfigSections(contentContainer);
            
            // Add content container to wrapper
            wrapper.appendChild(contentContainer);
        }
        
        // Hide accordion-body since content is now in wrapper
        if (accordionBody) {
            accordionBody.style.display = 'none';
        }
        
        // Remove button from header
        accordionHeader.removeChild(button);
        
        // Put button directly in wrapper (without accordion-header)
        wrapper.insertBefore(button, wrapper.firstChild);
        
        // Function to move body content to wrapper when expanded
        const moveBodyContent = (optimize = false) => {
            if (!contentContainer) {
                contentContainer = wrapper.querySelector('.accordion-content-container');
            }
            
            if (accordionBody && accordionCollapse.classList.contains('show')) {
                // Content is already in wrapper (pre-moved), just make it visible
                if (contentContainer) {
                    contentContainer.style.display = 'block';
                }
                
                // Remove any remaining config-section wrappers
                const maxIterations = optimize ? 5 : 10;
                for (let i = 0; i < maxIterations; i++) {
                    const beforeCount = wrapper.querySelectorAll('.config-section:not(.config-section-accordion)').length;
                    removeConfigSections(wrapper);
                    const afterCount = wrapper.querySelectorAll('.config-section:not(.config-section-accordion)').length;
                    if (beforeCount === afterCount) {
                        break; // No more config-sections to remove
                    }
                }
            } else if (accordionBody && !accordionCollapse.classList.contains('show')) {
                // When collapsed, hide content container
                if (contentContainer) {
                    contentContainer.style.display = 'none';
                }
            }
        };
        
        // Initialize content position based on current state
        if (accordionCollapse && accordionCollapse.classList.contains('show')) {
            if (contentContainer) {
                contentContainer.style.display = 'block';
            }
        } else {
            if (contentContainer) {
                contentContainer.style.display = 'none';
            }
        }
        
        // Listen for accordion show/hide events
        if (accordionCollapse) {
            let savedScrollTop = null;
            let buttonViewportTop = null; // Button's position relative to viewport
            let scrollRestoreInterval = null;
            let isRestoringScroll = false;
            let scrollRestoreTimeout = null;
            
            // Save scroll position and button viewport position when button is clicked
            button.addEventListener('click', (e) => {
                savedScrollTop = window.pageYOffset || document.documentElement.scrollTop;
                const buttonRect = button.getBoundingClientRect();
                buttonViewportTop = buttonRect.top; // Position relative to viewport
                
                // Clear any existing intervals/timeouts
                if (scrollRestoreInterval) {
                    clearInterval(scrollRestoreInterval);
                }
                if (scrollRestoreTimeout) {
                    clearTimeout(scrollRestoreTimeout);
                }
                
                
                // Start aggressive scroll restoration
                isRestoringScroll = true;
                
                // Immediate restore
                const restoreScroll = () => {
                    if (savedScrollTop !== null && buttonViewportTop !== null && isRestoringScroll) {
                        const currentScroll = window.pageYOffset || document.documentElement.scrollTop;
                        const currentButtonRect = button.getBoundingClientRect();
                        const currentButtonViewportTop = currentButtonRect.top;
                        const viewportDiff = currentButtonViewportTop - buttonViewportTop;
                        
                        // If scroll position changed or button moved, restore it
                        if (Math.abs(currentScroll - savedScrollTop) > 1 || Math.abs(viewportDiff) > 1) {
                            if (Math.abs(viewportDiff) > 1) {
                                // Adjust scroll to compensate for button movement
                                const newScrollTop = savedScrollTop - viewportDiff;
                                window.scrollTo(0, newScrollTop);
                            } else {
                                // Just restore scroll position
                                window.scrollTo(0, savedScrollTop);
                            }
                        }
                    }
                };
                
                // Restore immediately
                restoreScroll();
                
                // Start interval for continuous restoration
                scrollRestoreInterval = setInterval(restoreScroll, 8); // ~120fps for more aggressive restoration
                
                // Also use requestAnimationFrame for smooth restoration
                const rafRestore = () => {
                    if (isRestoringScroll) {
                        restoreScroll();
                        requestAnimationFrame(rafRestore);
                    }
                };
                requestAnimationFrame(rafRestore);
            }, { capture: true });
            
            // Use 'show.bs.collapse' to reveal content before animation starts
            accordionCollapse.addEventListener('show.bs.collapse', () => {
                // Save scroll position if not already saved
                if (savedScrollTop === null) {
                    savedScrollTop = window.pageYOffset || document.documentElement.scrollTop;
                    const buttonRect = button.getBoundingClientRect();
                    buttonViewportTop = buttonRect.top;
                }
                
                // Reveal content immediately (it's already in wrapper, just hidden)
                // This prevents the lag where content appears before background extends
                const contentContainer = wrapper.querySelector('.accordion-content-container');
                if (contentContainer) {
                    contentContainer.style.display = 'block';
                }
                
                // Clean up any config-sections
                moveBodyContent(true);
                
                // Restore scroll position to keep button in same viewport position
                const restoreScroll = () => {
                    if (savedScrollTop !== null && buttonViewportTop !== null) {
                        const currentButtonRect = button.getBoundingClientRect();
                        const currentButtonViewportTop = currentButtonRect.top;
                        const viewportDiff = currentButtonViewportTop - buttonViewportTop;
                        
                        if (Math.abs(viewportDiff) > 1) {
                            // Adjust scroll to compensate for button movement
                            const newScrollTop = savedScrollTop - viewportDiff;
                            window.scrollTo({
                                top: newScrollTop,
                                behavior: 'auto'
                            });
                        } else {
                            // Even if button hasn't moved, ensure scroll position is maintained
                            const currentScroll = window.pageYOffset || document.documentElement.scrollTop;
                            if (Math.abs(currentScroll - savedScrollTop) > 1) {
                                window.scrollTo({
                                    top: savedScrollTop,
                                    behavior: 'auto'
                                });
                            }
                        }
                    }
                };
                
                // Restore immediately and multiple times
                restoreScroll();
                requestAnimationFrame(restoreScroll);
                requestAnimationFrame(() => requestAnimationFrame(restoreScroll));
                requestAnimationFrame(() => requestAnimationFrame(() => requestAnimationFrame(restoreScroll)));
            });
            
            accordionCollapse.addEventListener('shown.bs.collapse', () => {
                // Final cleanup after accordion is fully shown
                moveBodyContent(true); // Use optimized version
                
                // Stop aggressive scroll restoration after animation completes
                scrollRestoreTimeout = setTimeout(() => {
                    isRestoringScroll = false;
                    if (scrollRestoreInterval) {
                        clearInterval(scrollRestoreInterval);
                        scrollRestoreInterval = null;
                    }
                    
                    // Final scroll position restore (multiple times to ensure it sticks)
                    const finalRestore = () => {
                        if (savedScrollTop !== null && buttonViewportTop !== null) {
                            const currentScroll = window.pageYOffset || document.documentElement.scrollTop;
                            const currentButtonRect = button.getBoundingClientRect();
                            const currentButtonViewportTop = currentButtonRect.top;
                            const viewportDiff = currentButtonViewportTop - buttonViewportTop;
                            
                            if (Math.abs(viewportDiff) > 1) {
                                const newScrollTop = savedScrollTop - viewportDiff;
                                window.scrollTo(0, newScrollTop);
                            } else if (Math.abs(currentScroll - savedScrollTop) > 1) {
                                window.scrollTo(0, savedScrollTop);
                            }
                        }
                    };
                    
                    // Restore multiple times to ensure it sticks
                    finalRestore();
                    requestAnimationFrame(() => {
                        finalRestore();
                        requestAnimationFrame(() => {
                            finalRestore();
                        });
                    });
                    
                    // Also remove config-sections after a short delay to catch any dynamically added ones
                    for (let i = 0; i < 10; i++) {
                        const beforeCount = wrapper.querySelectorAll('.config-section:not(.config-section-accordion)').length;
                        removeConfigSections(wrapper);
                        const afterCount = wrapper.querySelectorAll('.config-section:not(.config-section-accordion)').length;
                        if (beforeCount === afterCount) {
                            break; // No more config-sections to remove
                        }
                    }
                    
                    savedScrollTop = null; // Reset after use
                    buttonViewportTop = null;
                }, 400); // Wait for animation to complete (increased from 300ms)
            });
            
            accordionCollapse.addEventListener('hidden.bs.collapse', () => {
                moveBodyContent();
                savedScrollTop = null; // Reset when closed
                buttonViewportTop = null;
                isRestoringScroll = false;
                if (scrollRestoreInterval) {
                    clearInterval(scrollRestoreInterval);
                    scrollRestoreInterval = null;
                }
                if (scrollRestoreTimeout) {
                    clearTimeout(scrollRestoreTimeout);
                    scrollRestoreTimeout = null;
                }
            });
        }
        
        // Also remove config-sections immediately after wrapper is created (for already expanded accordions)
        // Use multiple timeouts to catch any that are added later
        setTimeout(() => {
            if (accordionCollapse && accordionCollapse.classList.contains('show')) {
                for (let i = 0; i < 5; i++) {
                    removeConfigSections(wrapper);
                }
            }
        }, 200);
        
        setTimeout(() => {
            if (accordionCollapse && accordionCollapse.classList.contains('show')) {
                for (let i = 0; i < 5; i++) {
                    removeConfigSections(wrapper);
                }
            }
        }, 500);
        
        setTimeout(() => {
            if (accordionCollapse && accordionCollapse.classList.contains('show')) {
                for (let i = 0; i < 5; i++) {
                    removeConfigSections(wrapper);
                }
            }
        }, 1000);
        
        // Insert wrapper before accordion-header in accordion-item
        accordionItem.insertBefore(wrapper, accordionHeader);
        
        // Hide accordion-header but keep it for Bootstrap structure (aria-labelledby reference)
        accordionHeader.style.display = 'none';
        
        // Keep accordion-collapse and accordion-body functional for Bootstrap
        // Content will be moved to wrapper, but Bootstrap still needs the structure
    });
}

// ===== GETTING STARTED PAGE SECTIONS =====
function wrapGettingStartedSections() {
    const main = document.querySelector('main .page-body, main .container, main .main-content, main');
    if (!main) return;
    
    // Check if we're on a configuration page or API reference page
    const isConfigPage = window.location.pathname.includes('/configuration/');
    const isApiReferencePage = window.location.pathname.includes('/api-reference');
    
    // Target headings for Getting Started page
    const gettingStartedHeadings = [
        'Basic Configuration',
        'Temel Yapılandırma',
        'Configuration File',
        'Yapılandırma Dosyası',
        'Quick Usage Example',
        'Hızlı Kullanım Örneği',
        'Conversation History',
        'Konuşma Geçmişi'
    ];
    
    // Exclude headings that should NOT be wrapped (main page title, comparison tables, next steps)
    const excludeHeadings = [
        'AI Provider Configuration',
        'AI Provider Yapılandırması',
        'Storage Provider Configuration',
        'Depolama Sağlayıcı Yapılandırması',
        'Database Configuration',
        'Veritabanı Yapılandırması',
        'Audio & OCR Configuration',
        'Ses ve OCR Yapılandırması',
        'Advanced Configuration',
        'Gelişmiş Yapılandırma',
        'Configuration Categories',
        'Yapılandırma Kategorileri',
        'Provider Comparison',
        'Sağlayıcı Karşılaştırması',
        'Storage Provider Comparison',
        'Depolama Sağlayıcı Karşılaştırması',
        'Audio and OCR Comparison',
        'Ses ve OCR Karşılaştırması',
        'Next Steps',
        'Sonraki Adımlar',
        'Related Categories',
        'İlgili Kategoriler',
        'Core Interfaces',
        'Temel Arayüzler',
        'API Reference Categories',
        'API Referans Kategorileri',
        'Strategy Pattern Interfaces',
        'Strateji Deseni Arayüzleri',
        'Examples',
        'Örnekler',
        'Examples Categories',
        'Örnekler Kategorileri',
        'Changelog Categories',
        'Değişiklikler Kategorileri',
        'Changelog',
        'Değişiklikler',
        'Version History',
        'Versiyon Geçmişi',
        'Migration Guides',
        'Taşınma Kılavuzları',
        'Deprecation Notices',
        'Kullanımdan Kaldırma Bildirimleri'
    ];
    
    const h2Elements = main.querySelectorAll('h2');
    h2Elements.forEach(h2 => {
        const headingText = h2.textContent.trim();
        
        // Skip if inside accordion-body, config-section-accordion, or any accordion-related element (version history page)
        if (h2.closest('.accordion-body') || 
            h2.closest('.config-section-accordion') || 
            h2.closest('.accordion-item') || 
            h2.closest('.accordion-collapse')) {
            return;
        }
        
        // Skip if inside a card (category cards on index pages)
        if (h2.closest('.card')) {
            return;
        }
        
        // Skip if already wrapped or should be excluded
        if (h2.parentElement.classList.contains('config-section') || 
            excludeHeadings.includes(headingText)) {
            return;
        }
        
        // Special handling for "Core Interfaces", "API Reference Categories", "Strategy Pattern Interfaces", "Examples", and "Changelog" - don't wrap the paragraph after it
        if (headingText === 'Core Interfaces' || headingText === 'Temel Arayüzler' ||
            headingText === 'API Reference Categories' || headingText === 'API Referans Kategorileri' ||
            headingText === 'Strategy Pattern Interfaces' || headingText === 'Strateji Deseni Arayüzleri' ||
            headingText === 'Examples' || headingText === 'Örnekler' ||
            headingText === 'Changelog Categories' || headingText === 'Değişiklikler Kategorileri' ||
            headingText === 'Version History' || headingText === 'Versiyon Geçmişi' ||
            headingText === 'Migration Guides' || headingText === 'Taşınma Kılavuzları' ||
            headingText === 'Deprecation Notices' || headingText === 'Kullanımdan Kaldırma Bildirimleri') {
            // Find all siblings until next h2 or hr and mark them to not be wrapped
            let nextSibling = h2.nextSibling;
            while (nextSibling) {
                if (nextSibling.nodeType === Node.ELEMENT_NODE) {
                    const tagName = nextSibling.tagName.toLowerCase();
                    // Mark all elements until hr or next h2 to not be wrapped
                    if (tagName === 'h2' || tagName === 'hr') {
                        break;
                    }
                    nextSibling.classList.add('no-config-section');
                }
                nextSibling = nextSibling.nextSibling;
            }
            return;
        }
        
        // For Getting Started page, only wrap specific headings
        // For configuration pages, API reference pages, examples pages, and changelog pages, wrap all h2 except excluded ones
        const isExamplesPage = window.location.pathname.includes('/examples');
        const isChangelogPage = window.location.pathname.includes('/changelog');
        const isChangelogIndexPage = window.location.pathname.match(/\/changelog\/?$/);
        const shouldWrap = (isConfigPage || isApiReferencePage || isExamplesPage || (isChangelogPage && !isChangelogIndexPage)) ? true : gettingStartedHeadings.includes(headingText);
        
        // Skip if h2 is inside a card (category cards on index pages)
        if (h2.closest('.card')) {
            return;
        }
        
        if (shouldWrap) {
            // Find all siblings until next h2 or hr or row
            const wrapper = document.createElement('div');
            wrapper.className = 'config-section';
            
            let nextSibling = h2.nextSibling;
            const elementsToWrap = [];
            
            while (nextSibling) {
                if (nextSibling.nodeType === Node.ELEMENT_NODE) {
                    const tagName = nextSibling.tagName.toLowerCase();
                    // Skip elements with no-config-section class
                    if (nextSibling.classList.contains('no-config-section')) {
                        nextSibling = nextSibling.nextSibling;
                        continue;
                    }
                    // Skip if inside a card or if it's a card/row element
                    if (nextSibling.classList.contains('card') || 
                        nextSibling.classList.contains('row') ||
                        nextSibling.closest('.card')) {
                        break;
                    }
                    // For examples page, stop at h3 headings (they will be wrapped separately)
                    if (isExamplesPage && tagName === 'h3') {
                        break;
                    }
                    if (tagName === 'h2' || tagName === 'hr' || 
                        nextSibling.classList.contains('config-section')) {
                        break;
                    }
                    elementsToWrap.push(nextSibling);
                } else if (nextSibling.nodeType === Node.TEXT_NODE) {
                    // Skip empty text nodes
                    if (nextSibling.textContent.trim()) {
                        // If it's not empty, it might be part of content, but we'll skip it for now
                    }
                }
                nextSibling = nextSibling.nextSibling;
            }
            
            if (elementsToWrap.length > 0) {
                elementsToWrap.forEach(el => wrapper.appendChild(el));
                h2.parentNode.insertBefore(wrapper, h2.nextSibling);
            }
        }
    });
    
    // For examples page, also wrap h3 headings
    if (window.location.pathname.includes('/examples')) {
        const h3Elements = main.querySelectorAll('h3');
        h3Elements.forEach(h3 => {
            // Skip if inside accordion-body, config-section-accordion, or any accordion-related element (version history page)
            if (h3.closest('.accordion-body') || 
                h3.closest('.config-section-accordion') || 
                h3.closest('.accordion-item') || 
                h3.closest('.accordion-collapse')) {
                return;
            }
            
            // Skip if inside a card (category cards on index pages)
            if (h3.closest('.card')) {
                return;
            }
            
            // Skip if already wrapped or inside a config-section
            if (h3.parentElement.classList.contains('config-section') || 
                h3.closest('.config-section')) {
                return;
            }
            
            // Find all siblings until next h2, h3, or hr
            const wrapper = document.createElement('div');
            wrapper.className = 'config-section';
            
            let nextSibling = h3.nextSibling;
            const elementsToWrap = [];
            
            while (nextSibling) {
                if (nextSibling.nodeType === Node.ELEMENT_NODE) {
                    const tagName = nextSibling.tagName.toLowerCase();
                    // Skip elements with no-config-section class
                    if (nextSibling.classList.contains('no-config-section')) {
                        nextSibling = nextSibling.nextSibling;
                        continue;
                    }
                    if (tagName === 'h2' || tagName === 'h3' || tagName === 'hr' || 
                        nextSibling.classList.contains('row') ||
                        nextSibling.classList.contains('config-section')) {
                        break;
                    }
                    elementsToWrap.push(nextSibling);
                } else if (nextSibling.nodeType === Node.TEXT_NODE) {
                    // Skip empty text nodes
                    if (nextSibling.textContent.trim()) {
                        // If it's not empty, it might be part of content, but we'll skip it for now
                    }
                }
                nextSibling = nextSibling.nextSibling;
            }
            
            if (elementsToWrap.length > 0) {
                elementsToWrap.forEach(el => wrapper.appendChild(el));
                h3.parentNode.insertBefore(wrapper, h3.nextSibling);
            }
        });
    }
}

// ===== THEME TOGGLE =====
function initThemeToggle() {
    const themeToggle = document.getElementById('themeToggle');
    const themeToggleMobile = document.getElementById('themeToggleMobile');
    const htmlElement = document.documentElement;
    const iconDark = document.querySelectorAll('.theme-icon-dark');
    const iconLight = document.querySelectorAll('.theme-icon-light');
    
    // Load saved theme or default to dark
    const savedTheme = localStorage.getItem('smartrag-theme') || 'dark';
    setTheme(savedTheme);
    
    // Theme toggle click handler for desktop
    if (themeToggle) {
        themeToggle.addEventListener('click', function() {
            const currentTheme = htmlElement.getAttribute('data-theme');
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            setTheme(newTheme);
        });
    }
    
    // Theme toggle click handler for mobile
    if (themeToggleMobile) {
        themeToggleMobile.addEventListener('click', function() {
            const currentTheme = htmlElement.getAttribute('data-theme');
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            setTheme(newTheme);
        });
    }
    
    function setTheme(theme) {
        htmlElement.setAttribute('data-theme', theme);
        localStorage.setItem('smartrag-theme', theme);
        
        // Update toggle button icons (both desktop and mobile)
        if (theme === 'dark') {
            iconDark.forEach(icon => icon.style.display = 'none');
            iconLight.forEach(icon => icon.style.display = 'inline-block');
        } else {
            iconDark.forEach(icon => icon.style.display = 'inline-block');
            iconLight.forEach(icon => icon.style.display = 'none');
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
                const menu = dropdown.querySelector('.dropdown-menu');
                if (menu) {
                    menu.classList.add('force-show');
                    menu.style.display = 'block';
                }
            } else {
                const menu = dropdown.querySelector('.dropdown-menu');
                if (menu) {
                    menu.classList.remove('force-show');
                    menu.style.display = 'none';
                }
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

// ===== DESKTOP DROPDOWN HOVER HANDLING =====
function initDesktopDropdown() {
    // Only apply to desktop (>= 992px)
    if (window.innerWidth < 992) return;
    
    const dropdowns = document.querySelectorAll('.nav-item.dropdown');
    let hoverTimeouts = new Map();
    let closeTimeouts = new Map();
    
    dropdowns.forEach(dropdown => {
        const toggle = dropdown.querySelector('.dropdown-toggle');
        const menu = dropdown.querySelector('.dropdown-menu');
        
        if (!toggle || !menu) return;
        
        // Show dropdown on hover (with small delay to prevent accidental opens)
        dropdown.addEventListener('mouseenter', function() {
            const dropdownId = dropdown.id || Math.random().toString(36);
            clearTimeout(closeTimeouts.get(dropdownId));
            
            hoverTimeouts.set(dropdownId, setTimeout(() => {
                const bsDropdown = bootstrap.Dropdown.getInstance(toggle);
                if (bsDropdown) {
                    bsDropdown.show();
                } else {
                    // Create new dropdown instance if it doesn't exist
                    new bootstrap.Dropdown(toggle, { autoClose: false }).show();
                }
            }, 100)); // Small delay to prevent accidental opens
        });
        
        // Keep dropdown open when mouse is over dropdown or menu
        dropdown.addEventListener('mouseleave', function(e) {
            const dropdownId = dropdown.id || Math.random().toString(36);
            clearTimeout(hoverTimeouts.get(dropdownId));
            
            // Check if mouse is moving to the menu
            const relatedTarget = e.relatedTarget;
            if (relatedTarget && (dropdown.contains(relatedTarget) || menu.contains(relatedTarget))) {
                return; // Mouse is still within dropdown area
            }
            
            // Close with delay to allow mouse to move to menu
            closeTimeouts.set(dropdownId, setTimeout(() => {
                const bsDropdown = bootstrap.Dropdown.getInstance(toggle);
                if (bsDropdown) {
                    bsDropdown.hide();
                }
            }, 300)); // Delay to allow mouse movement to menu
        });
        
        // Also handle menu mouseenter/leave
        menu.addEventListener('mouseenter', function() {
            const dropdownId = dropdown.id || Math.random().toString(36);
            clearTimeout(closeTimeouts.get(dropdownId));
        });
        
        menu.addEventListener('mouseleave', function(e) {
            const dropdownId = dropdown.id || Math.random().toString(36);
            const relatedTarget = e.relatedTarget;
            if (relatedTarget && dropdown.contains(relatedTarget)) {
                return; // Mouse is moving back to dropdown
            }
            
            closeTimeouts.set(dropdownId, setTimeout(() => {
                const bsDropdown = bootstrap.Dropdown.getInstance(toggle);
                if (bsDropdown) {
                    bsDropdown.hide();
                }
            }, 300));
        });
    });
    
    // Re-initialize on window resize
    let resizeTimeout;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(() => {
            if (window.innerWidth >= 992) {
                // Remove old event listeners by re-initializing
                initDesktopDropdown();
            }
        }, 250);
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

