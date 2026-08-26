window.raceTimerTimeline = {
    scrollToCurrent: function (containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const current = container.querySelector('.timing-timeline-now');
        if (current) {
            const containerBounds = container.getBoundingClientRect();
            const currentBounds = current.getBoundingClientRect();
            container.scrollLeft += currentBounds.left - containerBounds.left - container.clientWidth / 2 + currentBounds.width / 2;
        }
    },
    scrollToElement: function (containerId, elementId) {
        const container = document.getElementById(containerId);
        const element = document.getElementById(elementId);
        if (!container || !element) return;

        const containerBounds = container.getBoundingClientRect();
        const elementBounds = element.getBoundingClientRect();
        container.scrollBy({
            left: elementBounds.left - containerBounds.left - container.clientWidth / 2 + elementBounds.width / 2,
            behavior: 'smooth'
        });
    }
};
