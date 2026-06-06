var UnoAppManifest = {
    displayName: "Knitty Kitty"
}

(function configureInitialWindowSize() {
    const minimumWidth = 1280;
    const minimumHeight = 860;
    const preferredWidth = Math.min(1440, window.screen && window.screen.availWidth ? window.screen.availWidth : 1440);
    const preferredHeight = Math.min(960, window.screen && window.screen.availHeight ? window.screen.availHeight : 960);

    const applySize = () => {
        const minWidth = `${minimumWidth}px`;
        const minHeight = `${minimumHeight}px`;
        const sizedElements = [
            document.documentElement,
            document.body,
            document.getElementById("uno-body"),
            document.querySelector(".uno-body"),
            document.getElementById("uno-canvas")
        ].filter(Boolean);

        sizedElements.forEach(element => {
            element.style.minWidth = minWidth;
            element.style.minHeight = minHeight;
        });

        try {
            if (window.outerWidth < minimumWidth || window.outerHeight < minimumHeight) {
                const left = Math.max(0, Math.round(((window.screen && window.screen.availWidth) || preferredWidth) - preferredWidth) / 2);
                const top = Math.max(0, Math.round(((window.screen && window.screen.availHeight) || preferredHeight) - preferredHeight) / 2);
                window.moveTo(left, top);
                window.resizeTo(preferredWidth, preferredHeight);
            }
        } catch {
            // Browsers may reject programmatic resizing for normal tabs.
        }
    };

    if (document.readyState === "loading") {
        window.addEventListener("DOMContentLoaded", applySize, { once: true });
    } else {
        applySize();
    }

    window.addEventListener("load", applySize, { once: true });
    requestAnimationFrame(applySize);
    setTimeout(applySize, 250);
    setTimeout(applySize, 1000);
})();
