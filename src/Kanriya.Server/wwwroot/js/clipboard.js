window.clipboardFunctions = {
    copyToClipboard: async function(text) {
        try {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                // Modern way using Clipboard API
                await navigator.clipboard.writeText(text);
                return true;
            } else {
                // Fallback for older browsers
                const textArea = document.createElement("textarea");
                textArea.value = text;
                textArea.style.position = "fixed";
                textArea.style.left = "-999999px";
                textArea.style.top = "-999999px";
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();
                
                try {
                    const successful = document.execCommand('copy');
                    document.body.removeChild(textArea);
                    return successful;
                } catch (err) {
                    document.body.removeChild(textArea);
                    console.error('Failed to copy text: ', err);
                    return false;
                }
            }
        } catch (err) {
            console.error('Failed to copy text: ', err);
            return false;
        }
    }
};