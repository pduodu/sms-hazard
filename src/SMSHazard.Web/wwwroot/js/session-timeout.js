(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var root = document.querySelector('[data-session-timeout]');
        if (!root) return;

        var timeoutSeconds = parseInt(root.dataset.sessionTimeoutSeconds || '1800', 10);
        var warningSeconds = parseInt(root.dataset.sessionWarningSeconds || '60', 10);
        var loginUrl = root.dataset.sessionLoginUrl || '/Account/Login';
        var keepAliveUrl = root.dataset.sessionKeepAliveUrl || '/Notifications/UnreadCount';
        var modal = document.querySelector('[data-session-modal]');
        var count = document.querySelector('[data-session-countdown]');
        var stay = document.querySelector('[data-session-stay]');
        var signOut = document.querySelector('[data-session-signout]');

        if (!modal || !count) return;

        var warningTimer;
        var redirectTimer;
        var countdownTimer;
        var remaining = warningSeconds;
        var visible = false;
        var ignoredEvents = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll'];

        function format(seconds) {
            var m = Math.floor(seconds / 60);
            var s = seconds % 60;
            return String(m).padStart(2, '0') + ':' + String(s).padStart(2, '0');
        }

        function hideModal() {
            visible = false;
            modal.classList.remove('is-open');
            modal.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('session-modal-open');
            window.clearInterval(countdownTimer);
            window.clearTimeout(redirectTimer);
        }

        function redirectToLogin() {
            window.location.href = loginUrl;
        }

        function showModal() {
            visible = true;
            remaining = warningSeconds;
            count.textContent = format(remaining);
            modal.classList.add('is-open');
            modal.setAttribute('aria-hidden', 'false');
            document.body.classList.add('session-modal-open');

            countdownTimer = window.setInterval(function () {
                remaining -= 1;
                count.textContent = format(Math.max(remaining, 0));
                if (remaining <= 0) redirectToLogin();
            }, 1000);

            redirectTimer = window.setTimeout(redirectToLogin, warningSeconds * 1000);
        }

        function schedule() {
            window.clearTimeout(warningTimer);
            window.clearInterval(countdownTimer);
            window.clearTimeout(redirectTimer);
            warningTimer = window.setTimeout(showModal, Math.max(timeoutSeconds - warningSeconds, 1) * 1000);
        }

        async function staySignedIn() {
            hideModal();
            try {
                var response = await fetch(keepAliveUrl, { headers: { 'Accept': 'application/json' }, cache: 'no-store' });
                if (response.redirected || response.url.indexOf('/Account/Login') >= 0 || response.status === 401 || response.status === 403) {
                    redirectToLogin();
                    return;
                }
            } catch (e) {
                redirectToLogin();
                return;
            }
            schedule();
        }

        ignoredEvents.forEach(function (eventName) {
            document.addEventListener(eventName, function () {
                if (!visible) schedule();
            }, { passive: true });
        });

        if (stay) stay.addEventListener('click', staySignedIn);
        if (signOut) signOut.addEventListener('click', redirectToLogin);

        schedule();
    });
})();
