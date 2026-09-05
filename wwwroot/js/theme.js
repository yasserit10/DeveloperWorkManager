(() => {
    const storageKey = 'dwm-theme';
    const cookieKey = 'dwm-theme';
    const root = document.documentElement;

    const readCookieTheme = () => document.cookie.split('; ')
        .find((entry) => entry.startsWith(`${cookieKey}=`))
        ?.split('=')[1];

    const resolveTheme = () => {
        try {
            const saved = localStorage.getItem(storageKey);
            if (saved === 'light' || saved === 'dark') return saved;
        } catch { }
        const cookieTheme = readCookieTheme();
        if (cookieTheme === 'light' || cookieTheme === 'dark') return cookieTheme;
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    };

    const updateToggleUi = (theme) => {
        const dark = theme === 'dark';
        document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
            button.setAttribute('aria-pressed', String(dark));
            const label = dark ? 'الوضع المضيء' : 'الوضع الداكن';
            button.setAttribute('aria-label', `تفعيل ${label}`);
            button.setAttribute('title', `تفعيل ${label}`);
            button.querySelector('[data-theme-label]')?.replaceChildren(label);
            const icon = button.querySelector('[data-theme-icon]');
            if (icon) icon.className = `fa-solid ${dark ? 'fa-sun' : 'fa-moon'}`;
        });
    };

    const applyTheme = (theme) => {
        root.dataset.theme = theme;
        root.style.colorScheme = theme;
        updateToggleUi(theme);
    };

    const persistTheme = (theme) => {
        try { localStorage.setItem(storageKey, theme); } catch { }
        document.cookie = `${cookieKey}=${theme}; path=/; max-age=31536000; SameSite=Lax`;
    };

    window.dwmTheme = {
        toggle: () => {
            const next = root.dataset.theme === 'dark' ? 'light' : 'dark';
            persistTheme(next);
            applyTheme(next);
        },
        apply: applyTheme
    };

    const synchronizeTheme = () => applyTheme(resolveTheme());
    applyTheme(resolveTheme());

    // Enhanced navigation can replace the document element attributes. Restore
    // the persisted user choice after every Blazor navigation lifecycle event.
    document.addEventListener('enhancedload', synchronizeTheme);
    document.addEventListener('enhancednavigationend', synchronizeTheme);
    window.addEventListener('pageshow', synchronizeTheme);

    new MutationObserver(() => {
        const storedTheme = resolveTheme();
        if (root.dataset.theme !== storedTheme) applyTheme(storedTheme);
    }).observe(root, { attributes: true, attributeFilter: ['data-theme'] });
})();
