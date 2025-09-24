// Enhanced conveyor belt timing for ScoreBanner
// This provides more precise animation timing when available

export function getHighPrecisionTime() {
    return performance.now();
}

export function scheduleItemAnimation(itemId, delay) {
    const element = document.querySelector(`[data-item-id="${itemId}"]`);
    if (element) {
        // Apply any additional timing adjustments if needed
        if (delay > 0) {
            element.style.animationDelay = `${delay}ms`;
        } else {
            element.style.animationDelay = '0ms';
        }
    }
}

export function onAnimationComplete(itemId, callback) {
    const element = document.querySelector(`[data-item-id="${itemId}"]`);
    if (element) {
        element.addEventListener('animationend', () => {
            callback.invokeMethodAsync('OnItemAnimationComplete', itemId);
        }, { once: true });
    }
}