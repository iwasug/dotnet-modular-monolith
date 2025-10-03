// Custom JavaScript for Enhanced Swagger UI Functionality

(function() {
    'use strict';

    // Wait for Swagger UI to load
    window.addEventListener('load', function() {
        initializeCustomFeatures();
    });

    function initializeCustomFeatures() {
        // Add custom header with API information
        addCustomHeader();
        
        // Enhance authorization handling
        enhanceAuthorization();
        
        // Add language selector for localization
        addLanguageSelector();
        
        // Add request/response interceptors
        addInterceptors();
        
        // Add keyboard shortcuts
        addKeyboardShortcuts();
        
        // Add copy to clipboard functionality
        addCopyToClipboard();
        
        // Add request timing
        addRequestTiming();
    }

    function addCustomHeader() {
        const topbar = document.querySelector('.swagger-ui .topbar');
        if (topbar && !document.querySelector('.custom-api-info')) {
            const customInfo = document.createElement('div');
            customInfo.className = 'custom-api-info';
            customInfo.style.cssText = `
                position: absolute;
                right: 20px;
                top: 50%;
                transform: translateY(-50%);
                color: white;
                font-size: 0.9rem;
                display: flex;
                gap: 20px;
                align-items: center;
            `;
            
            customInfo.innerHTML = `
                <div>
                    <strong>Version:</strong> v1.0
                </div>
                <div>
                    <strong>Environment:</strong> ${getEnvironment()}
                </div>
                <div id="api-status" style="display: flex; align-items: center; gap: 5px;">
                    <div style="width: 8px; height: 8px; border-radius: 50%; background: #10b981;"></div>
                    <span>Online</span>
                </div>
            `;
            
            topbar.style.position = 'relative';
            topbar.appendChild(customInfo);
        }
    }

    function enhanceAuthorization() {
        // Auto-fill authorization from localStorage
        const savedToken = localStorage.getItem('swagger-auth-token');
        if (savedToken) {
            setTimeout(() => {
                const authInput = document.querySelector('input[placeholder*="Bearer"]');
                if (authInput && !authInput.value) {
                    authInput.value = savedToken;
                }
            }, 1000);
        }

        // Save token when authorization is set
        document.addEventListener('click', function(e) {
            if (e.target.textContent === 'Authorize' || e.target.closest('.btn.authorize')) {
                setTimeout(() => {
                    const authInput = document.querySelector('input[placeholder*="Bearer"]');
                    if (authInput && authInput.value) {
                        localStorage.setItem('swagger-auth-token', authInput.value);
                    }
                }, 500);
            }
        });

        // Clear token on logout
        document.addEventListener('click', function(e) {
            if (e.target.textContent === 'Logout' || e.target.closest('.btn.btn-done')) {
                localStorage.removeItem('swagger-auth-token');
            }
        });
    }

    function addLanguageSelector() {
        const info = document.querySelector('.swagger-ui .info');
        if (info && !document.querySelector('.language-selector')) {
            const languageSelector = document.createElement('div');
            languageSelector.className = 'language-selector';
            languageSelector.style.cssText = `
                margin-top: 1rem;
                padding: 1rem;
                background: #f8fafc;
                border-radius: 8px;
                border: 1px solid #e2e8f0;
            `;
            
            languageSelector.innerHTML = `
                <label style="font-weight: 600; color: #1e293b; margin-right: 10px;">
                    Language:
                </label>
                <select id="language-select" style="
                    padding: 0.5rem;
                    border: 1px solid #d1d5db;
                    border-radius: 4px;
                    background: white;
                    color: #374151;
                    font-size: 0.9rem;
                ">
                    <option value="en-US">English (US)</option>
                    <option value="es-ES">Español</option>
                    <option value="fr-FR">Français</option>
                    <option value="de-DE">Deutsch</option>
                </select>
                <small style="margin-left: 10px; color: #6b7280;">
                    Changes the language for API responses and error messages
                </small>
            `;
            
            info.appendChild(languageSelector);
            
            // Handle language change
            document.getElementById('language-select').addEventListener('change', function(e) {
                const selectedLanguage = e.target.value;
                localStorage.setItem('preferred-language', selectedLanguage);
                
                // Show notification
                showNotification(`Language changed to ${e.target.selectedOptions[0].text}`, 'success');
            });
            
            // Load saved language preference
            const savedLanguage = localStorage.getItem('preferred-language');
            if (savedLanguage) {
                document.getElementById('language-select').value = savedLanguage;
            }
        }
    }

    function addInterceptors() {
        // Intercept requests to add Accept-Language header
        const originalFetch = window.fetch;
        window.fetch = function(...args) {
            const [url, options = {}] = args;
            
            // Add Accept-Language header
            const preferredLanguage = localStorage.getItem('preferred-language') || 'en-US';
            options.headers = {
                ...options.headers,
                'Accept-Language': preferredLanguage
            };
            
            // Add correlation ID for request tracking
            options.headers['X-Correlation-ID'] = generateCorrelationId();
            
            return originalFetch(url, options);
        };
    }

    function addKeyboardShortcuts() {
        document.addEventListener('keydown', function(e) {
            // Ctrl/Cmd + K: Focus on search/filter
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                const filterInput = document.querySelector('.filter input');
                if (filterInput) {
                    filterInput.focus();
                }
            }
            
            // Ctrl/Cmd + Enter: Execute current operation
            if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
                e.preventDefault();
                const executeBtn = document.querySelector('.btn.execute:not([disabled])');
                if (executeBtn) {
                    executeBtn.click();
                }
            }
            
            // Escape: Close any open modals
            if (e.key === 'Escape') {
                const modal = document.querySelector('.swagger-ui .modal');
                if (modal) {
                    const closeBtn = modal.querySelector('.close-modal');
                    if (closeBtn) {
                        closeBtn.click();
                    }
                }
            }
        });
    }

    function addCopyToClipboard() {
        // Add copy buttons to code blocks
        document.addEventListener('click', function(e) {
            if (e.target.closest('.highlight-code')) {
                const codeBlock = e.target.closest('.highlight-code');
                if (!codeBlock.querySelector('.copy-btn')) {
                    const copyBtn = document.createElement('button');
                    copyBtn.className = 'copy-btn';
                    copyBtn.innerHTML = '📋';
                    copyBtn.style.cssText = `
                        position: absolute;
                        top: 10px;
                        right: 10px;
                        background: rgba(255, 255, 255, 0.1);
                        border: none;
                        color: white;
                        padding: 5px 8px;
                        border-radius: 4px;
                        cursor: pointer;
                        font-size: 12px;
                    `;
                    
                    copyBtn.addEventListener('click', function(e) {
                        e.stopPropagation();
                        const code = codeBlock.querySelector('pre').textContent;
                        navigator.clipboard.writeText(code).then(() => {
                            copyBtn.innerHTML = '✅';
                            setTimeout(() => {
                                copyBtn.innerHTML = '📋';
                            }, 2000);
                        });
                    });
                    
                    codeBlock.style.position = 'relative';
                    codeBlock.appendChild(copyBtn);
                }
            }
        });
    }

    function addRequestTiming() {
        // Track request timing
        const originalFetch = window.fetch;
        window.fetch = function(...args) {
            const startTime = performance.now();
            
            return originalFetch(...args).then(response => {
                const endTime = performance.now();
                const duration = Math.round(endTime - startTime);
                
                // Add timing info to response
                setTimeout(() => {
                    const responseSection = document.querySelector('.responses-wrapper .live-responses-table');
                    if (responseSection && !responseSection.querySelector('.request-timing')) {
                        const timingInfo = document.createElement('div');
                        timingInfo.className = 'request-timing';
                        timingInfo.style.cssText = `
                            padding: 0.5rem;
                            background: #f0f9ff;
                            border: 1px solid #0ea5e9;
                            border-radius: 4px;
                            margin-bottom: 1rem;
                            font-size: 0.9rem;
                            color: #0c4a6e;
                        `;
                        timingInfo.innerHTML = `
                            <strong>Request completed in ${duration}ms</strong>
                            <span style="margin-left: 10px; color: #64748b;">
                                Status: ${response.status} ${response.statusText}
                            </span>
                        `;
                        responseSection.insertBefore(timingInfo, responseSection.firstChild);
                    }
                }, 100);
                
                return response;
            });
        };
    }

    function getEnvironment() {
        const hostname = window.location.hostname;
        if (hostname === 'localhost' || hostname === '127.0.0.1') {
            return 'Development';
        } else if (hostname.includes('staging') || hostname.includes('test')) {
            return 'Staging';
        } else {
            return 'Production';
        }
    }

    function generateCorrelationId() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    function showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 1rem 1.5rem;
            background: ${type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#3b82f6'};
            color: white;
            border-radius: 8px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
            z-index: 10000;
            font-weight: 500;
            max-width: 300px;
            animation: slideIn 0.3s ease-out;
        `;
        
        notification.textContent = message;
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease-in';
            setTimeout(() => {
                document.body.removeChild(notification);
            }, 300);
        }, 3000);
    }

    // Add CSS animations
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);

})();