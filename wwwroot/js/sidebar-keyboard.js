(() => {
    if (window.__dwmSidebarKeyboardAttached) return;

    window.__dwmSidebarKeyboardAttached = true;

    const isVisible = (element) => element.getClientRects().length > 0 && !element.hasAttribute('disabled');

    document.addEventListener('keydown', (event) => {
        if (!['ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) return;
        if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;

        const target = event.target instanceof Element ? event.target : null;
        if (!target || target.matches('input, textarea, select, [contenteditable="true"]')) return;

        const navigation = target.closest('[data-sidebar-navigation]');
        if (!navigation) return;

        const links = [...navigation.querySelectorAll('a.nav-link[href]')].filter(isVisible);
        if (!links.length) return;

        const currentLink = target.closest('a.nav-link[href]');
        let currentIndex = links.indexOf(currentLink);

        if (currentIndex < 0) {
            currentIndex = links.findIndex((link) => link.classList.contains('active'));
        }

        let nextIndex;
        if (event.key === 'Home') nextIndex = 0;
        else if (event.key === 'End') nextIndex = links.length - 1;
        else if (event.key === 'ArrowDown') nextIndex = (currentIndex + 1 + links.length) % links.length;
        else nextIndex = (currentIndex - 1 + links.length) % links.length;

        const nextLink = links[nextIndex];
        if (!nextLink) return;

        event.preventDefault();
        nextLink.focus({ preventScroll: true });
        nextLink.scrollIntoView({ block: 'nearest' });
        nextLink.click();
    });
})();
