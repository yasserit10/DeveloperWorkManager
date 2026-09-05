(() => {
    if (window.__dwmPageTransitionsAttached || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    window.__dwmPageTransitionsAttached = true;
    const root = document.documentElement;
    let animationFrame;

    const playEnterAnimation = () => {
        root.classList.remove('is-page-changing', 'is-page-entering');
        window.cancelAnimationFrame(animationFrame);
        animationFrame = window.requestAnimationFrame(() => {
            root.classList.add('is-page-entering');
            window.setTimeout(() => root.classList.remove('is-page-entering'), 420);
        });
    };

    const isInternalPageLink = (event) => {
        if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return false;

        const link = event.target.closest('a[href]');
        if (!link || link.target || link.hasAttribute('download') || link.dataset.noPageTransition !== undefined) return false;

        const url = new URL(link.href, window.location.href);
        return url.origin === window.location.origin && url.href !== window.location.href && !url.hash;
    };

    // Blazor Enhanced Navigation handles internal links without a full reload.
    // These effects never prevent or delay navigation.
    document.addEventListener('click', (event) => {
        if (isInternalPageLink(event)) root.classList.add('is-page-changing');
    }, true);
    document.addEventListener('enhancednavigationstart', () => root.classList.add('is-page-changing'));
    document.addEventListener('enhancednavigationend', () => root.classList.remove('is-page-changing'));
    document.addEventListener('enhancedload', playEnterAnimation);
})();

window.dwm ??= {};
window.dwm.scrollToId = (id) => {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
};

window.dwm.highlightTask = (id, duration = 3000) => {
    const task = document.getElementById(id);
    if (!task) return;

    task.classList.remove('task-notification-highlight');
    task.scrollIntoView({
        behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth',
        block: 'center'
    });
    window.requestAnimationFrame(() => task.classList.add('task-notification-highlight'));
    window.setTimeout(() => task.classList.remove('task-notification-highlight'), duration);
};

window.dwm.openModalDialog = (dialog) => {
    if (dialog instanceof HTMLDialogElement && !dialog.open) {
        dialog.showModal();
    }
};
