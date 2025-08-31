import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// Kanriya Event System
class KanriyaEventBus {
    constructor() {
        this.events = {};
        this.avaloniaReady = false;
    }

    on(event, callback) {
        if (!this.events[event]) {
            this.events[event] = [];
        }
        this.events[event].push(callback);
    }

    off(event, callback) {
        if (this.events[event]) {
            this.events[event] = this.events[event].filter(cb => cb !== callback);
        }
    }

    emit(event, data) {
        console.log(`[Kanriya Event] ${event}:`, data);
        if (this.events[event]) {
            this.events[event].forEach(callback => callback(data));
        }
        
        // Also dispatch as custom DOM event for broader compatibility
        window.dispatchEvent(new CustomEvent(`kanriya:${event}`, { detail: data }));
    }
}

// Create global event bus
const kanriyaEvents = new KanriyaEventBus();
window.kanriyaEvents = kanriyaEvents;

// Function to close the splash screen
function closeSplashScreen() {
    const splashScreen = document.querySelector('.kanriya-splash');
    if (splashScreen) {
        splashScreen.classList.add('splash-close');
        setTimeout(() => {
            splashScreen.style.display = 'none';
            kanriyaEvents.emit('splashClosed', { timestamp: Date.now() });
        }, 300);
    }
}

// Function to update loading progress
function updateLoadingProgress(percentage, message) {
    const progressBar = document.querySelector('.loading-progress');
    const loadingText = document.querySelector('.loading-text');
    
    if (progressBar) {
        progressBar.style.width = `${percentage}%`;
    }
    if (loadingText) {
        loadingText.textContent = message || 'Loading application...';
    }
    
    kanriyaEvents.emit('loadingProgress', { percentage, message });
}

// Function to show notifications
function showNotification(title, message, type = 'info') {
    // You can implement toast notifications here
    console.log(`[${type.toUpperCase()}] ${title}: ${message}`);
    
    kanriyaEvents.emit('notification', { title, message, type });
    
    // Example: Create a simple toast notification
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `<strong>${title}</strong><br>${message}`;
    toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${type === 'error' ? '#ef4444' : type === 'success' ? '#10b981' : '#3b82f6'};
        color: white;
        padding: 12px 20px;
        border-radius: 8px;
        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        z-index: 10000;
        animation: slideIn 0.3s ease-out;
    `;
    document.body.appendChild(toast);
    
    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease-out';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Function to receive events from C#
function receiveFromJS(eventName, data) {
    kanriyaEvents.emit(eventName, data);
}

// Export functions for C# interop
globalThis.closeSplashScreen = closeSplashScreen;
globalThis.updateLoadingProgress = updateLoadingProgress;
globalThis.showNotification = showNotification;
globalThis.receiveFromJS = receiveFromJS;

// Set up event listeners
kanriyaEvents.on('avaloniaReady', () => {
    console.log('Avalonia application is ready!');
    kanriyaEvents.avaloniaReady = true;
    closeSplashScreen();
});

kanriyaEvents.on('languageChanged', (data) => {
    console.log('Language changed to:', data);
    // You can handle language changes in JavaScript UI if needed
});

// Initialize dotnet runtime
updateLoadingProgress(10, 'Initializing runtime...');

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

updateLoadingProgress(30, 'Loading assemblies...');

const config = dotnetRuntime.getConfig();

// Export the assembly for C# interop
await dotnetRuntime.setModuleImports('main.js', {
    closeSplashScreen,
    updateLoadingProgress,
    showNotification,
    receiveFromJS
});

updateLoadingProgress(60, 'Starting application...');

// Run the main assembly
await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

updateLoadingProgress(100, 'Application loaded!');

// Close splash screen after a short delay to ensure the app is rendered
setTimeout(() => {
    if (!kanriyaEvents.avaloniaReady) {
        closeSplashScreen();
    }
}, 1000);

// Add CSS animations
if (!document.querySelector('#kanriya-animations')) {
    const style = document.createElement('style');
    style.id = 'kanriya-animations';
    style.textContent = `
        @keyframes slideIn {
            from { transform: translateX(100%); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
        @keyframes slideOut {
            from { transform: translateX(0); opacity: 1; }
            to { transform: translateX(100%); opacity: 0; }
        }
    `;
    document.head.appendChild(style);
}
