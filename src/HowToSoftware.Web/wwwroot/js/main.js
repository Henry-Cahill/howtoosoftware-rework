/**
 * HowToSoftware Theme - Main JavaScript
 * Complete, self-contained theme functionality
 * Author: Henry Lawrence Cahill <henry.cahill@howtoosoftware.com>
 * Website: https://howtoosoftware.com
 * Version is automatically detected from Ghost's asset versioning
 */

(function() {
    'use strict';

    // ========================================
    // THEME VERSION - Auto-detected from asset URL or fallback
    // ========================================
    var THEME_VERSION = (function() {
        // Try to get version from the script's URL (Ghost appends ?v=HASH)
        var scripts = document.querySelectorAll('script[src*="main.min.js"], script[src*="main.js"]');
        for (var i = 0; i < scripts.length; i++) {
            var src = scripts[i].src;
            var match = src.match(/[?&]v=([^&]+)/);
            if (match) {
                return match[1];
            }
        }
        return '2.0.1'; // Fallback version
    })();
    var THEME_NAME = 'HowToSoftware Theme';
    var THEME_AUTHOR = 'Henry Lawrence Cahill';

    // ========================================
    // DEBUG LOGGING SYSTEM - VERBOSE MODE
    // ========================================
    
    var DEBUG = true; // Set to false in production
    var DEBUG_PREFIX = '🔧 [HTS]';
    
    // Simple logging functions that always output
    function logInfo(msg, data) {
        if (!DEBUG) return;
        if (data !== undefined) {
            console.log('%c' + DEBUG_PREFIX + ' ℹ️ %s', 'color: #667eea; font-weight: bold;', msg, data);
        } else {
            console.log('%c' + DEBUG_PREFIX + ' ℹ️ %s', 'color: #667eea; font-weight: bold;', msg);
        }
    }
    
    function logSuccess(msg, data) {
        if (!DEBUG) return;
        if (data !== undefined) {
            console.log('%c' + DEBUG_PREFIX + ' ✅ ' + msg, 'color: #28a745; font-weight: bold;', data);
        } else {
            console.log('%c' + DEBUG_PREFIX + ' ✅ ' + msg, 'color: #28a745; font-weight: bold;');
        }
    }
    
    function logWarn(msg, data) {
        if (!DEBUG) return;
        if (data !== undefined) {
            console.warn('%c' + DEBUG_PREFIX + ' ⚠️ ' + msg, 'color: #ffc107; font-weight: bold;', data);
        } else {
            console.warn('%c' + DEBUG_PREFIX + ' ⚠️ ' + msg, 'color: #ffc107; font-weight: bold;');
        }
    }
    
    function logError(msg, data) {
        if (!DEBUG) return;
        if (data !== undefined) {
            console.error('%c' + DEBUG_PREFIX + ' ❌ ' + msg, 'color: #dc3545; font-weight: bold;', data);
        } else {
            console.error('%c' + DEBUG_PREFIX + ' ❌ ' + msg, 'color: #dc3545; font-weight: bold;');
        }
    }
    
    function logCSS(msg, data) {
        if (!DEBUG) return;
        if (data !== undefined) {
            console.log('%c' + DEBUG_PREFIX + ' 🎨 ' + msg, 'color: #764ba2; font-weight: bold;', data);
        } else {
            console.log('%c' + DEBUG_PREFIX + ' 🎨 ' + msg, 'color: #764ba2; font-weight: bold;');
        }
    }
    
    function logInit(msg, data) {
        if (!DEBUG) return;
        if (data !== undefined) {
            console.log('%c' + DEBUG_PREFIX + ' 🚀 ' + msg, 'color: #17a2b8; font-weight: bold;', data);
        } else {
            console.log('%c' + DEBUG_PREFIX + ' 🚀 ' + msg, 'color: #17a2b8; font-weight: bold;');
        }
    }
    
    function logPerf(msg, data) {
        if (!DEBUG) return;
        if (data !== undefined) {
            console.log('%c' + DEBUG_PREFIX + ' ⏱️ ' + msg, 'color: #fd7e14; font-weight: bold;', data);
        } else {
            console.log('%c' + DEBUG_PREFIX + ' ⏱️ ' + msg, 'color: #fd7e14; font-weight: bold;');
        }
    }
    
    function logDOM(msg, data) {
        if (!DEBUG) return;
        if (data !== undefined) {
            console.log('%c' + DEBUG_PREFIX + ' 🏗️ ' + msg, 'color: #20c997; font-weight: bold;', data);
        } else {
            console.log('%c' + DEBUG_PREFIX + ' 🏗️ ' + msg, 'color: #20c997; font-weight: bold;');
        }
    }

    // Expose debug functions globally
    window.HTSDebug = {
        info: logInfo,
        success: logSuccess,
        warn: logWarn,
        error: logError,
        css: logCSS,
        init: logInit,
        perf: logPerf,
        dom: logDOM,
        runDiagnostics: function() { runAllDiagnostics(); }
    };

    // ========================================
    // DARK MODE DEFAULT - Set immediately & FORCE
    // ========================================
    
    // ALWAYS default to dark mode - clear any cached light mode preference
    (function setDarkModeDefault() {
        var html = document.documentElement;
        var body = document.body;
        
        console.log('%c[HTS] 🌙 Theme Initializer Running...', 'color: #667eea; font-weight: bold;');
        console.log('%c[HTS] 🌙 Previous localStorage theme: ' + localStorage.getItem('theme'), 'color: #667eea;');
        console.log('%c[HTS] 🌙 HTML classes before: ' + html.className, 'color: #667eea;');
        console.log('%c[HTS] 🌙 Body classes before: ' + (body ? body.className : 'body not ready'), 'color: #667eea;');
        
        // FORCE dark mode - always set it
        html.setAttribute('data-theme', 'dark');
        html.classList.remove('light-mode');
        html.classList.add('dark-mode');
        
        if (body) {
            body.classList.remove('light-mode');
            body.classList.add('dark-mode');
        }
        
        // Save dark as the preference
        localStorage.setItem('theme', 'dark');
        
        console.log('%c[HTS] 🌙 HTML classes after: ' + html.className, 'color: #28a745; font-weight: bold;');
        console.log('%c[HTS] 🌙 Body classes after: ' + (body ? body.className : 'body not ready'), 'color: #28a745; font-weight: bold;');
        console.log('%c[HTS] 🌙 data-theme attribute: ' + html.getAttribute('data-theme'), 'color: #28a745; font-weight: bold;');
        console.log('%c[HTS] ✅ Dark mode FORCED as default', 'color: #28a745; font-weight: bold; font-size: 12px;');
    })();

    // ========================================
    // IMMEDIATE STARTUP LOGGING
    // ========================================
    
    var startTime = performance.now();

    // ========================================
    // GLOBAL ERROR HANDLER - Catch all JS errors
    // ========================================
    window.onerror = function(message, source, lineno, colno, error) {
        console.error('%c╔══════════════════════════════════════════════════════════════╗', 'color: #dc3545;');
        console.error('%c║     ❌ JAVASCRIPT ERROR CAUGHT                               ║', 'color: #dc3545; font-weight: bold;');
        console.error('%c╚══════════════════════════════════════════════════════════════╝', 'color: #dc3545;');
        console.error('%c' + DEBUG_PREFIX + ' Error Message: ' + message, 'color: #dc3545; font-weight: bold;');
        console.error('%c' + DEBUG_PREFIX + ' Source: ' + source, 'color: #dc3545;');
        console.error('%c' + DEBUG_PREFIX + ' Line: ' + lineno + ', Column: ' + colno, 'color: #dc3545;');
        if (error && error.stack) {
            console.error('%c' + DEBUG_PREFIX + ' Stack Trace:', 'color: #dc3545;', error.stack);
        }
        return false; // Let the error propagate
    };

    window.addEventListener('unhandledrejection', function(event) {
        console.error('%c╔══════════════════════════════════════════════════════════════╗', 'color: #dc3545;');
        console.error('%c║     ❌ UNHANDLED PROMISE REJECTION                           ║', 'color: #dc3545; font-weight: bold;');
        console.error('%c╚══════════════════════════════════════════════════════════════╝', 'color: #dc3545;');
        console.error('%c' + DEBUG_PREFIX + ' Reason:', 'color: #dc3545; font-weight: bold;', event.reason);
    });
    
    var versionPadded = (THEME_NAME + ' v' + THEME_VERSION).padEnd(50);
    console.log('%c╔══════════════════════════════════════════════════════════════╗', 'color: #667eea;');
    console.log('%c║     🔧 ' + versionPadded + '  ║', 'color: #667eea; font-weight: bold; font-size: 14px;');
    console.log('%c║     Debug Mode Active                                        ║', 'color: #667eea;');
    console.log('%c╚══════════════════════════════════════════════════════════════╝', 'color: #667eea;');
    
    logInit('Theme script executing (v' + THEME_VERSION + ')...');
    logInfo('Page URL: ' + window.location.href);
    logInfo('Protocol: ' + window.location.protocol);
    logInfo('Hostname: ' + window.location.hostname);
    logInfo('Pathname: ' + window.location.pathname);
    logInfo('Document readyState: ' + document.readyState);
    logInfo('Document title: ' + document.title);
    logInfo('Viewport: ' + window.innerWidth + 'x' + window.innerHeight);
    logInfo('Device pixel ratio: ' + window.devicePixelRatio);
    logInfo('User Agent: ' + navigator.userAgent);
    logInfo('Language: ' + navigator.language);
    logInfo('Cookies enabled: ' + navigator.cookieEnabled);
    logInfo('Online: ' + navigator.onLine);
    
    // Check if we're in Ghost
    if (window.ghost || document.querySelector('meta[name="generator"][content*="Ghost"]')) {
        logSuccess('Ghost CMS detected');
    } else {
        logWarn('Ghost CMS not detected - may affect functionality');
    }

    // ========================================
    // CSS DIAGNOSTICS - Run immediately
    // ========================================
    
    function checkCSSLoading() {
        console.log('%c── CSS Loading Diagnostics ──', 'color: #764ba2; font-weight: bold; font-size: 12px;');
        
        var stylesheets = document.querySelectorAll('link[rel="stylesheet"]');
        logCSS('Total stylesheets found: ' + stylesheets.length);
        
        stylesheets.forEach(function(link, index) {
            var href = link.href || '(inline)';
            var filename = href.split('/').pop().split('?')[0];
            var loaded = link.sheet !== null;
            var disabled = link.disabled;
            
            if (loaded && !disabled) {
                logSuccess('CSS [' + index + '] ' + filename + ' - LOADED');
            } else if (disabled) {
                logWarn('CSS [' + index + '] ' + filename + ' - DISABLED');
            } else {
                logError('CSS [' + index + '] ' + filename + ' - FAILED TO LOAD');
            }
            
            // Extra details for screen.css
            if (href.includes('screen.css')) {
                logCSS('  └─ Full URL: ' + href);
                if (link.sheet) {
                    try {
                        var rulesCount = link.sheet.cssRules ? link.sheet.cssRules.length : 0;
                        logCSS('  └─ CSS Rules: ' + rulesCount);
                        if (rulesCount === 0) {
                            logError('  └─ WARNING: screen.css has 0 rules!');
                        } else if (rulesCount < 100) {
                            logWarn('  └─ WARNING: screen.css has only ' + rulesCount + ' rules (expected 500+)');
                        } else {
                            logSuccess('  └─ Rule count looks good: ' + rulesCount);
                        }
                    } catch(e) {
                        logWarn('  └─ Cannot read CSS rules (CORS restriction): ' + e.message);
                    }
                }
            }
        });
        
        // Check for inline styles that might override
        var inlineStyles = document.querySelectorAll('style');
        if (inlineStyles.length > 0) {
            logInfo('Inline <style> tags found: ' + inlineStyles.length);
        }
    }

    // ========================================
    // CSS VARIABLES CHECK
    // ========================================
    
    function checkCSSVariables() {
        console.log('%c── CSS Variables Check ──', 'color: #764ba2; font-weight: bold; font-size: 12px;');
        
        var root = document.documentElement;
        var styles = getComputedStyle(root);
        
        var varsToCheck = [
            '--color-primary',
            '--color-primary-dark',
            '--color-bg',
            '--color-bg-card',
            '--color-text',
            '--color-text-secondary',
            '--color-border',
            '--color-link',
            '--gradient-primary',
            '--gradient-hero',
            '--ghost-accent-color',
            '--transition-base',
            '--transition-fast',
            '--shadow-md',
            '--shadow-lg',
            '--header-height',
            '--content-max-width',
            '--font-sans'
        ];
        
        var found = 0;
        var notFound = 0;
        
        varsToCheck.forEach(function(varName) {
            var value = styles.getPropertyValue(varName).trim();
            if (value) {
                found++;
                // Truncate long values
                var displayValue = value.length > 60 ? value.substring(0, 60) + '...' : value;
                logCSS(varName + ': ' + displayValue);
            } else {
                notFound++;
                logWarn(varName + ': (not set)');
            }
        });
        
        logInfo('CSS Variables: ' + found + ' found, ' + notFound + ' not set');
        
        if (notFound > 10) {
            logError('Many CSS variables missing - theme CSS may not be loading correctly!');
        }
    }

    // ========================================
    // DOM STRUCTURE CHECK
    // ========================================
    
    function checkDOMStructure() {
        console.log('%c── DOM Structure Analysis ──', 'color: #20c997; font-weight: bold; font-size: 12px;');
        
        var checks = [
            { selector: 'html', name: 'HTML element', critical: true },
            { selector: 'body', name: 'Body element', critical: true },
            { selector: '.viewport', name: 'Viewport wrapper', critical: false },
            { selector: '.gh-head, .site-header', name: 'Header', critical: true },
            { selector: '.gh-head-logo', name: 'Logo', critical: false },
            { selector: '.gh-head-menu, .nav-menu', name: 'Navigation menu', critical: false },
            { selector: '.gh-burger', name: 'Mobile burger menu', critical: false },
            { selector: '.site-content, .gh-main', name: 'Main content area', critical: true },
            { selector: '.home-hero, .hero-section, .gh-cover', name: 'Hero section', critical: false },
            { selector: '.post-feed, .posts-grid', name: 'Post feed/grid', critical: false },
            { selector: '.post-card', name: 'Post cards', critical: false, count: true },
            { selector: '.gh-content, .post-full-content', name: 'Article content', critical: false },
            { selector: '.gh-foot, .site-footer', name: 'Footer', critical: true },
            { selector: '.back-to-top', name: 'Back to top button', critical: false },
            { selector: '.reading-progress-bar', name: 'Reading progress bar', critical: false },
            { selector: '.pagination', name: 'Pagination', critical: false }
        ];
        
        var foundCount = 0;
        var missingCritical = [];
        
        checks.forEach(function(check) {
            var elements = document.querySelectorAll(check.selector);
            var count = elements.length;
            
            if (count > 0) {
                foundCount++;
                if (check.count) {
                    logDOM(check.name + ': ' + count + ' found');
                } else {
                    logSuccess(check.name + ': Found');
                }
                
                // Log first element's classes for debugging
                if (elements[0] && elements[0].className) {
                    var classes = elements[0].className;
                    if (typeof classes === 'string' && classes.length > 0) {
                        logDOM('  └─ Classes: ' + classes.substring(0, 80));
                    }
                }
            } else {
                if (check.critical) {
                    missingCritical.push(check.name);
                    logError(check.name + ': MISSING (Critical!)');
                } else {
                    logWarn(check.name + ': Not found');
                }
            }
        });
        
        logInfo('DOM Elements: ' + foundCount + '/' + checks.length + ' found');
        
        if (missingCritical.length > 0) {
            logError('Missing critical elements: ' + missingCritical.join(', '));
            logError('Theme may not be properly activated!');
        }
        
        // Check body classes
        var bodyClasses = document.body.className;
        logDOM('Body classes: ' + bodyClasses);
        
        // Check for Ghost-specific classes
        if (bodyClasses.includes('home-template')) {
            logSuccess('Page type: Home page');
        } else if (bodyClasses.includes('post-template')) {
            logSuccess('Page type: Single post');
        } else if (bodyClasses.includes('page-template')) {
            logSuccess('Page type: Static page');
        } else if (bodyClasses.includes('tag-template')) {
            logSuccess('Page type: Tag archive');
        } else if (bodyClasses.includes('author-template')) {
            logSuccess('Page type: Author archive');
        } else {
            logInfo('Page type: Unknown');
        }
    }

    // ========================================
    // COMPUTED STYLES CHECK
    // ========================================
    
    function checkComputedStyles() {
        console.log('%c── Computed Styles on Key Elements ──', 'color: #764ba2; font-weight: bold; font-size: 12px;');
        
        var elementsToCheck = [
            { selector: 'body', props: ['backgroundColor', 'color', 'fontFamily', 'fontSize'] },
            { selector: '.gh-head, .site-header', props: ['backgroundColor', 'background', 'position', 'height'] },
            { selector: '.post-card', props: ['backgroundColor', 'borderRadius', 'boxShadow', 'display'] },
            { selector: '.post-card-title, .post-card h2', props: ['color', 'fontSize', 'fontWeight'] },
            { selector: '.gh-foot, .site-footer', props: ['backgroundColor', 'background', 'color'] }
        ];
        
        elementsToCheck.forEach(function(item) {
            var el = document.querySelector(item.selector);
            if (el) {
                var styles = getComputedStyle(el);
                logCSS('Element: ' + item.selector);
                item.props.forEach(function(prop) {
                    var value = styles[prop];
                    if (value) {
                        // Truncate long values
                        var displayValue = value.length > 50 ? value.substring(0, 50) + '...' : value;
                        logCSS('  └─ ' + prop + ': ' + displayValue);
                    }
                });
            } else {
                logWarn('Element not found: ' + item.selector);
            }
        });
    }

    // ========================================
    // JAVASCRIPT ENVIRONMENT CHECK
    // ========================================
    
    function checkJSEnvironment() {
        console.log('%c── JavaScript Environment ──', 'color: #17a2b8; font-weight: bold; font-size: 12px;');
        
        logInfo('jQuery available: ' + (typeof jQuery !== 'undefined' ? 'Yes (v' + jQuery.fn.jquery + ')' : 'No'));
        logInfo('Ghost API available: ' + (typeof ghost !== 'undefined' ? 'Yes' : 'No'));
        
        // Check for common libraries
        var libraries = [
            { name: 'Prism (syntax highlighting)', check: function() { return typeof Prism !== 'undefined'; } },
            { name: 'PhotoSwipe (lightbox)', check: function() { return typeof PhotoSwipe !== 'undefined'; } },
            { name: 'Owl Carousel', check: function() { return typeof jQuery !== 'undefined' && typeof jQuery.fn.owlCarousel !== 'undefined'; } },
            { name: 'Jarallax (parallax)', check: function() { return typeof jarallax !== 'undefined'; } }
        ];
        
        libraries.forEach(function(lib) {
            try {
                if (lib.check()) {
                    logSuccess(lib.name + ': Available');
                } else {
                    logInfo(lib.name + ': Not loaded');
                }
            } catch(e) {
                logInfo(lib.name + ': Not loaded');
            }
        });
    }

    // ========================================
    // ERROR LOGGING SETUP
    // ========================================
    
    function setupErrorLogging() {
        window.addEventListener('error', function(e) {
            logError('JavaScript Error: ' + e.message);
            logError('  └─ File: ' + e.filename);
            logError('  └─ Line: ' + e.lineno + ', Column: ' + e.colno);
        });
        
        window.addEventListener('unhandledrejection', function(e) {
            logError('Unhandled Promise Rejection: ' + e.reason);
        });
        
        logSuccess('Error logging initialized');
    }

    // ========================================
    // NETWORK LOGGING SETUP
    // ========================================
    
    function setupNetworkLogging() {
        var originalFetch = window.fetch;
        window.fetch = function() {
            var url = arguments[0];
            var urlStr = typeof url === 'string' ? url : (url.url || url.href || String(url));
            
            // Only log non-analytics requests to reduce noise
            if (!urlStr.includes('analytics')) {
                logInfo('Fetch: ' + urlStr.substring(0, 80));
            }
            
            return originalFetch.apply(this, arguments).then(function(response) {
                if (!response.ok) {
                    logWarn('Fetch failed [' + response.status + ']: ' + urlStr.substring(0, 60));
                }
                return response;
            }).catch(function(err) {
                logError('Fetch error: ' + err.message);
                throw err;
            });
        };
        
        logSuccess('Network logging initialized');
    }

    // ========================================
    // PERFORMANCE LOGGING
    // ========================================
    
    function logPerformanceMetrics() {
        console.log('%c── Performance Metrics ──', 'color: #fd7e14; font-weight: bold; font-size: 12px;');
        
        if (window.performance && window.performance.timing) {
            var timing = window.performance.timing;
            var loadTime = timing.loadEventEnd - timing.navigationStart;
            var domReady = timing.domContentLoadedEventEnd - timing.navigationStart;
            var firstByte = timing.responseStart - timing.navigationStart;
            var domInteractive = timing.domInteractive - timing.navigationStart;
            
            logPerf('Time to First Byte: ' + firstByte + 'ms');
            logPerf('DOM Interactive: ' + domInteractive + 'ms');
            logPerf('DOM Content Loaded: ' + domReady + 'ms');
            logPerf('Full Page Load: ' + loadTime + 'ms');
            
            // Analyze performance
            if (loadTime > 5000) {
                logWarn('Page load is slow (> 5 seconds)');
            } else if (loadTime > 3000) {
                logInfo('Page load is moderate (3-5 seconds)');
            } else {
                logSuccess('Page load is fast (< 3 seconds)');
            }
        }
        
        // Resource timing
        if (window.performance && window.performance.getEntriesByType) {
            var resources = window.performance.getEntriesByType('resource');
            
            // CSS resources
            var cssResources = resources.filter(function(r) { return r.name.includes('.css'); });
            if (cssResources.length > 0) {
                logInfo('CSS Resources:');
                cssResources.forEach(function(r) {
                    var filename = r.name.split('/').pop().split('?')[0];
                    logPerf('  └─ ' + filename + ': ' + Math.round(r.duration) + 'ms (' + Math.round(r.transferSize/1024) + 'KB)');
                });
            }
            
            // JS resources
            var jsResources = resources.filter(function(r) { return r.name.includes('.js'); });
            if (jsResources.length > 0) {
                logInfo('JS Resources:');
                jsResources.forEach(function(r) {
                    var filename = r.name.split('/').pop().split('?')[0];
                    logPerf('  └─ ' + filename + ': ' + Math.round(r.duration) + 'ms');
                });
            }
        }
    }

    // ========================================
    // RUN ALL DIAGNOSTICS
    // ========================================
    
    function runAllDiagnostics() {
        console.log('%c╔══════════════════════════════════════════════════════════════╗', 'color: #667eea;');
        console.log('%c║              Running Full Diagnostics Suite                  ║', 'color: #667eea; font-weight: bold;');
        console.log('%c╚══════════════════════════════════════════════════════════════╝', 'color: #667eea;');
        
        checkCSSLoading();
        checkCSSVariables();
        checkDOMStructure();
        checkComputedStyles();
        checkJSEnvironment();
        logPerformanceMetrics();
        
        var endTime = performance.now();
        logPerf('Diagnostics completed in ' + Math.round(endTime - startTime) + 'ms');
    }

    // Setup error and network logging immediately
    setupErrorLogging();
    setupNetworkLogging();

    // ========================================
    // UTILITY FUNCTIONS
    // ========================================
    
    function debounce(func, wait) {
        var timeout;
        return function() {
            var context = this, args = arguments;
            clearTimeout(timeout);
            timeout = setTimeout(function() {
                func.apply(context, args);
            }, wait);
        };
    }

    function rafThrottle(func) {
        var ticking = false;
        return function() {
            var context = this, args = arguments;
            if (!ticking) {
                requestAnimationFrame(function() {
                    func.apply(context, args);
                    ticking = false;
                });
                ticking = true;
            }
        };
    }

    // ========================================
    // FITVIDS - Responsive Video Embeds
    // ========================================
    
    function fitVids(container) {
        if (!container) return;
        var selectors = [
            'iframe[src*="player.vimeo.com"]',
            'iframe[src*="youtube.com"]',
            'iframe[src*="youtube-nocookie.com"]',
            'iframe[src*="kickstarter.com"][src*="video.html"]',
            'object',
            'embed'
        ];

        var elements = container.querySelectorAll(selectors.join(','));
        
        elements.forEach(function(el) {
            if (el.getAttribute('data-fitvids-processed')) return;
            if (el.tagName === 'EMBED' && el.parentNode.tagName === 'OBJECT') return;
            if (el.parentNode.classList.contains('fluid-width-video-wrapper')) return;

            var width = el.getAttribute('width') || el.offsetWidth;
            var height = el.getAttribute('height') || el.offsetHeight;
            
            if (!width || !height) return;
            
            var aspectRatio = (parseInt(height, 10) / parseInt(width, 10)) * 100;

            var wrapper = document.createElement('div');
            wrapper.className = 'fluid-width-video-wrapper';
            wrapper.style.cssText = 'position:relative;padding-bottom:' + aspectRatio + '%;height:0;overflow:hidden;';
            
            el.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;';
            el.removeAttribute('width');
            el.removeAttribute('height');
            el.setAttribute('data-fitvids-processed', 'true');
            
            el.parentNode.insertBefore(wrapper, el);
            wrapper.appendChild(el);
        });
    }

    // ========================================
    // MOBILE NAVIGATION
    // ========================================
    
    function initMobileNav() {
        var burger = document.querySelector('.gh-burger');
        var body = document.body;
        
        if (burger) {
            burger.addEventListener('click', function() {
                body.classList.toggle('gh-head-open');
            });
        }

        document.addEventListener('click', function(e) {
            if (body.classList.contains('gh-head-open')) {
                var head = document.querySelector('.gh-head');
                if (head && !head.contains(e.target)) {
                    body.classList.remove('gh-head-open');
                }
            }
        });

        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && body.classList.contains('gh-head-open')) {
                body.classList.remove('gh-head-open');
            }
        });
    }

    // ========================================
    // READING PROGRESS BAR
    // ========================================
    
    function initReadingProgress() {
        var progressBar = document.querySelector('.reading-progress-bar');
        var article = document.querySelector('.gh-content, .post-full-content, article');
        
        if (!progressBar || !article) return;

        var updateProgress = rafThrottle(function() {
            var articleRect = article.getBoundingClientRect();
            var articleTop = articleRect.top + window.scrollY;
            var articleHeight = article.offsetHeight;
            var windowHeight = window.innerHeight;
            var scrollY = window.scrollY;
            
            var start = articleTop - windowHeight;
            var end = articleTop + articleHeight;
            var current = scrollY;
            
            var progress = Math.max(0, Math.min(100, ((current - start) / (end - start)) * 100));
            progressBar.style.width = progress + '%';
        });

        window.addEventListener('scroll', updateProgress, { passive: true });
        updateProgress();
    }

    // ========================================
    // BACK TO TOP BUTTON
    // ========================================
    
    function initBackToTop() {
        var btn = document.querySelector('.back-to-top');
        if (!btn) return;

        var toggleVisibility = rafThrottle(function() {
            if (window.scrollY > 300) {
                btn.classList.add('visible');
            } else {
                btn.classList.remove('visible');
            }
        });

        window.addEventListener('scroll', toggleVisibility, { passive: true });
        
        btn.addEventListener('click', function() {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });

        toggleVisibility();
    }

    // ========================================
    // STICKY HEADER
    // ========================================
    
    function initStickyHeader() {
        var header = document.querySelector('.site-header, .gh-head');
        if (!header) return;

        var handleScroll = rafThrottle(function() {
            if (window.scrollY > 100) {
                header.classList.add('scrolled');
            } else {
                header.classList.remove('scrolled');
            }
        });

        window.addEventListener('scroll', handleScroll, { passive: true });
    }

    // ========================================
    // CODE BLOCK ENHANCEMENTS
    // ========================================
    
    function initCodeBlocks() {
        var codeBlocks = document.querySelectorAll('pre code');
        
        codeBlocks.forEach(function(code) {
            var pre = code.parentElement;
            if (!pre || pre.querySelector('.code-copy-btn')) return;
            
            pre.classList.add('line-numbers');
            pre.style.position = 'relative';
            
            var copyBtn = document.createElement('button');
            copyBtn.className = 'code-copy-btn';
            copyBtn.setAttribute('aria-label', 'Copy code to clipboard');
            copyBtn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>';
            
            copyBtn.addEventListener('click', function() {
                navigator.clipboard.writeText(code.textContent).then(function() {
                    copyBtn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>';
                    copyBtn.classList.add('copied');
                    
                    setTimeout(function() {
                        copyBtn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>';
                        copyBtn.classList.remove('copied');
                    }, 2000);
                }).catch(function(err) {
                    console.error('Failed to copy:', err);
                });
            });
            
            pre.appendChild(copyBtn);
        });
    }

    // ========================================
    // READING TIME CALCULATOR
    // ========================================
    
    function initReadingTime() {
        var readingTimeEl = document.querySelector('.reading-time');
        if (!readingTimeEl) return;
        
        var content = document.querySelector('.gh-content, .post-full-content');
        if (!content) return;
        
        var text = content.textContent || '';
        var wordsPerMinute = 200;
        var wordCount = text.trim().split(/\s+/).length;
        var readingTime = Math.ceil(wordCount / wordsPerMinute);
        
        readingTimeEl.textContent = readingTime + ' min read';
    }

    // ========================================
    // SMOOTH SCROLL FOR ANCHOR LINKS
    // ========================================
    
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(function(anchor) {
            anchor.addEventListener('click', function(e) {
                var href = this.getAttribute('href');
                
                if (!href || href === '#' || href.startsWith('#/portal') || href.startsWith('#/')) {
                    return;
                }
                
                try {
                    var target = document.querySelector(href);
                    if (target) {
                        e.preventDefault();
                        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                        history.pushState(null, null, href);
                    }
                } catch (err) { /* Invalid selector */ }
            });
        });
    }

    // ========================================
    // LAZY LOADING IMAGES
    // ========================================
    
    function initLazyLoad() {
        if ('loading' in HTMLImageElement.prototype) {
            var images = document.querySelectorAll('img[data-src]');
            images.forEach(function(img) {
                img.src = img.dataset.src;
                img.removeAttribute('data-src');
            });
        } else if ('IntersectionObserver' in window) {
            var imageObserver = new IntersectionObserver(function(entries) {
                entries.forEach(function(entry) {
                    if (entry.isIntersecting) {
                        var img = entry.target;
                        if (img.dataset.src) {
                            img.src = img.dataset.src;
                            img.removeAttribute('data-src');
                        }
                        imageObserver.unobserve(img);
                    }
                });
            });

            document.querySelectorAll('img[data-src]').forEach(function(img) {
                imageObserver.observe(img);
            });
        }
    }

    // ========================================
    // EXTERNAL LINK HANDLING
    // ========================================
    
    function initExternalLinks() {
        var links = document.querySelectorAll('a[href^="http"]');
        var currentHost = window.location.host;
        
        links.forEach(function(link) {
            if (link.host !== currentHost) {
                link.setAttribute('target', '_blank');
                link.setAttribute('rel', 'noopener noreferrer');
            }
        });
    }

    // ========================================
    // COPY TO CLIPBOARD UTILITY
    // ========================================
    
    window.copyToClipboard = function(text, event) {
        navigator.clipboard.writeText(text).then(function() {
            var btn = event && event.target ? event.target.closest('.copy-link, .share-copy, [data-copy]') : null;
            if (btn) {
                var originalHTML = btn.innerHTML;
                btn.innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>';
                btn.style.background = '#28a745';
                
                setTimeout(function() {
                    btn.innerHTML = originalHTML;
                    btn.style.background = '';
                }, 2000);
            }
        }).catch(function(err) {
            console.error('Failed to copy:', err);
        });
    };

    // ========================================
    // TABLE OF CONTENTS
    // ========================================
    
    function initTableOfContents() {
        var toc = document.querySelector('.table-of-contents, .toc');
        var article = document.querySelector('.gh-content, .post-full-content');
        
        if (!toc || !article) return;

        var headings = article.querySelectorAll('h2, h3');
        if (headings.length === 0) return;

        var tocList = document.createElement('ul');
        tocList.className = 'toc-list';

        headings.forEach(function(heading, index) {
            if (!heading.id) {
                heading.id = 'heading-' + index;
            }

            var li = document.createElement('li');
            li.className = 'toc-item toc-' + heading.tagName.toLowerCase();
            
            var link = document.createElement('a');
            link.href = '#' + heading.id;
            link.textContent = heading.textContent;
            
            li.appendChild(link);
            tocList.appendChild(li);
        });

        toc.appendChild(tocList);

        if ('IntersectionObserver' in window) {
            var observer = new IntersectionObserver(function(entries) {
                entries.forEach(function(entry) {
                    var id = entry.target.id;
                    var tocLink = toc.querySelector('a[href="#' + id + '"]');
                    if (tocLink) {
                        if (entry.isIntersecting) {
                            toc.querySelectorAll('a').forEach(function(a) { a.classList.remove('active'); });
                            tocLink.classList.add('active');
                        }
                    }
                });
            }, { rootMargin: '-20% 0px -80% 0px' });

            headings.forEach(function(heading) {
                observer.observe(heading);
            });
        }
    }

    // ========================================
    // INITIALIZE EVERYTHING
    // ========================================
    
    function init() {
        console.log('%c── Module Initialization ──', 'color: #17a2b8; font-weight: bold; font-size: 12px;');
        logInit('Running initialization functions...');
        
        var initFunctions = [
            { name: 'Mobile Navigation', fn: initMobileNav },
            { name: 'Sticky Header', fn: initStickyHeader },
            { name: 'Back to Top', fn: initBackToTop },
            { name: 'Smooth Scroll', fn: initSmoothScroll },
            { name: 'Lazy Load', fn: initLazyLoad },
            { name: 'External Links', fn: initExternalLinks },
            { name: 'Reading Progress', fn: initReadingProgress },
            { name: 'Reading Time', fn: initReadingTime },
            { name: 'Code Blocks', fn: initCodeBlocks },
            { name: 'Table of Contents', fn: initTableOfContents }
        ];
        
        var successCount = 0;
        var failCount = 0;
        
        initFunctions.forEach(function(item) {
            try {
                item.fn();
                successCount++;
                logSuccess(item.name + ' - OK');
            } catch (e) {
                failCount++;
                logError(item.name + ' - FAILED: ' + e.message);
            }
        });

        var content = document.querySelector('.gh-content, .post-full-content, .site-content');
        if (content) {
            fitVids(content);
            logSuccess('FitVids - OK');
        } else {
            logInfo('FitVids - No content container found');
        }
        
        logInfo('Modules initialized: ' + successCount + ' success, ' + failCount + ' failed');

        // Run all diagnostics
        console.log('%c── Running Full Diagnostics ──', 'color: #667eea; font-weight: bold; font-size: 12px;');
        checkCSSLoading();
        checkCSSVariables();
        checkDOMStructure();
        checkComputedStyles();
        checkJSEnvironment();
        
        var endTime = performance.now();
        logPerf('Theme initialization completed in ' + Math.round(endTime - startTime) + 'ms');
        
        // Log performance after page fully loads
        window.addEventListener('load', function() {
            setTimeout(logPerformanceMetrics, 100);
        });

        var readyMsg = ('🎉 ' + THEME_NAME + ' v' + THEME_VERSION + ' Ready!').padEnd(50);
        console.log('%c╔══════════════════════════════════════════════════════════════╗', 'color: #28a745;');
        console.log('%c║     ' + readyMsg + '      ║', 'color: #28a745; font-weight: bold; font-size: 14px;');
        console.log('%c╚══════════════════════════════════════════════════════════════╝', 'color: #28a745;');
        console.log('%cAuthor: ' + THEME_AUTHOR + ' | https://howtoosoftware.com', 'color: #667eea;');
        console.log('%cTheme Version: ' + THEME_VERSION, 'color: #999;');
        console.log('%cType HTSDebug.runDiagnostics() to re-run all diagnostics', 'color: #999; font-style: italic;');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();

// ========================================
// JQUERY PLUGINS (if jQuery is available)
// ========================================

(function() {
    'use strict';
    
    if (typeof jQuery === 'undefined') return;

    var $ = jQuery;

    $.fn.fitVids = function() {
        return this.each(function() {
            var selectors = [
                'iframe[src*="player.vimeo.com"]',
                'iframe[src*="youtube.com"]',
                'iframe[src*="youtube-nocookie.com"]',
                'iframe[src*="kickstarter.com"][src*="video.html"]',
                'object',
                'embed'
            ];

            var $this = $(this);
            var $elements = $this.find(selectors.join(','));
            
            $elements.each(function() {
                var $el = $(this);
                if ($el.parents('.fluid-width-video-wrapper').length) return;
                if ($el.parent().is('object')) return;
                
                var width = $el.attr('width') || $el.width();
                var height = $el.attr('height') || $el.height();
                
                if (!width || !height) return;
                
                var aspectRatio = (parseInt(height, 10) / parseInt(width, 10)) * 100;
                
                $el.removeAttr('width').removeAttr('height')
                   .wrap('<div class="fluid-width-video-wrapper" style="position:relative;padding-bottom:' + aspectRatio + '%;height:0;overflow:hidden;"></div>')
                   .css({ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%' });
            });
        });
    };

    $(document).ready(function() {
        if (typeof $.fn.owlCarousel !== 'undefined') {
            $('.featured-posts').owlCarousel({
                dots: false,
                margin: 30,
                nav: true,
                navText: [
                    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="18" height="18" fill="currentColor"><path d="M20.547 22.107L14.44 16l6.107-6.12L18.667 8l-8 8 8 8 1.88-1.893z"></path></svg>',
                    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="18" height="18" fill="currentColor"><path d="M11.453 22.107L17.56 16l-6.107-6.12L13.333 8l8 8-8 8-1.88-1.893z"></path></svg>'
                ],
                responsive: {
                    0: { items: 1, slideBy: 1 },
                    768: { items: 3, slideBy: 3 },
                    992: { items: 4, slideBy: 4 }
                }
            });
        }

        if (typeof $.fn.jarallax !== 'undefined') {
            var $parallax = $('.jarallax-img');
            if ($parallax.length && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
                $parallax.parent().jarallax({
                    speed: 0.1,
                    disableParallax: /iPad|iPhone|iPod|Android/,
                    disableVideo: /iPad|iPhone|iPod|Android/
                });
            }
        }

        // Initialize fitVids on content
        $('.gh-content').fitVids();
    });

})();