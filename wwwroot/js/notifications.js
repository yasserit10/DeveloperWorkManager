(() => {
    const storageKey = "dwm-desktop-notifications";
    let timerId = null;

    const seenIds = () => {
        try { return new Set(JSON.parse(localStorage.getItem(storageKey) || "[]")); }
        catch { return new Set(); }
    };

    const saveSeenIds = (ids) => {
        localStorage.setItem(storageKey, JSON.stringify([...ids].slice(-200)));
    };

    const poll = async () => {
        if (!("Notification" in window) || Notification.permission !== "granted") return;
        try {
            const response = await fetch("/api/notifications/unread", { credentials: "same-origin", cache: "no-store" });
            if (!response.ok) return;
            const notifications = await response.json();
            const seen = seenIds();
            notifications.slice().reverse().forEach((item) => {
                if (seen.has(item.id)) return;
                const notification = new Notification(item.title, {
                    body: item.message,
                    icon: "/favicon.png",
                    tag: `dwm-${item.id}`,
                    renotify: false
                });
                notification.onclick = () => {
                    window.focus();
                    if (item.targetUrl) window.location.assign(item.targetUrl);
                    notification.close();
                };
                seen.add(item.id);
            });
            saveSeenIds(seen);
        } catch { /* Network errors should never interrupt the application UI. */ }
    };

    window.dwmNotifications = {
        isEnabled: () => "Notification" in window && Notification.permission === "granted",
        start: () => {
            if (timerId !== null) return;
            poll();
            timerId = window.setInterval(poll, 30000);
        },
        requestPermission: async () => {
            if (!("Notification" in window)) return false;
            const permission = await Notification.requestPermission();
            if (permission === "granted") {
                window.dwmNotifications.start();
                await poll();
                return true;
            }
            return false;
        }
    };
})();
