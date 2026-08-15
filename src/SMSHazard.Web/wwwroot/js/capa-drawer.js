(function () {
    function initEnhancements(root) {
        if (window.lucide) {
            lucide.createIcons();
        }

        if (window.flatpickr) {
            root.querySelectorAll('input[type="date"]:not([data-flatpickr="off"])').forEach(function (el) {
                if (el._flatpickr) return;
                flatpickr(el, {
                    dateFormat: 'Y-m-d',
                    allowInput: true,
                    disableMobile: true
                });
            });
        }

        if (window.jQuery && jQuery.fn.select2) {
            jQuery(root).find('select:not([data-select2="off"])').not('.flatpickr-monthDropdown-months').each(function () {
                var $select = jQuery(this);
                if ($select.closest('.flatpickr-calendar').length) return;
                if ($select.hasClass('select2-hidden-accessible')) return;
                var placeholder = $select.find('option[value=""]').first().text() || $select.attr('placeholder') || 'Select an option';
                $select.select2({
                    width: '100%',
                    placeholder: placeholder,
                    allowClear: $select.find('option[value=""]').length > 0,
                    dropdownParent: jQuery(root.closest('[data-capa-drawer]') || document.body),
                    dropdownAutoWidth: false
                });
            });
        }

        if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
            jQuery.validator.unobtrusive.parse(root);
        }

        if (window.initRiskAssessment) {
            window.initRiskAssessment(root);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        var drawer = document.querySelector('[data-capa-drawer]');
        var backdrop = document.querySelector('[data-capa-drawer-backdrop]');
        var content = document.querySelector('[data-capa-drawer-content]');
        var title = document.querySelector('[data-capa-drawer-title]');
        var kicker = document.querySelector('[data-capa-drawer-kicker]');
        var closeTimer;

        if (!drawer || !backdrop || !content) return;

        function setLoading(text) {
            content.innerHTML = '<div class="capa-drawer-loading"><span class="spinner"></span><span>' + text + '</span></div>';
        }

        function openDrawer(nextTitle, nextKicker) {
            window.clearTimeout(closeTimer);
            if (title && nextTitle) title.textContent = nextTitle;
            if (kicker) kicker.textContent = nextKicker || 'Action';
            drawer.hidden = false;
            backdrop.hidden = false;
            drawer.offsetHeight;
            requestAnimationFrame(function () {
                requestAnimationFrame(function () {
                    drawer.classList.add('is-open');
                    backdrop.classList.add('is-open');
                });
            });
            drawer.setAttribute('aria-hidden', 'false');
            document.body.classList.add('drawer-open');
        }

        function closeDrawer(ev) {
            if (ev) ev.preventDefault();
            drawer.classList.remove('is-open');
            backdrop.classList.remove('is-open');
            drawer.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('drawer-open');
            closeTimer = window.setTimeout(function () {
                drawer.hidden = true;
                backdrop.hidden = true;
                content.innerHTML = '';
            }, 320);
        }

        async function loadDrawer(link) {
            openDrawer(link.getAttribute('data-capa-drawer-title') || 'Action', link.getAttribute('data-capa-drawer-kicker'));
            setLoading(link.getAttribute('data-capa-drawer-loading') || 'Loading action form...');

            var response = await fetch(link.getAttribute('data-capa-drawer-url'), {
                headers: { 'X-CAPA-Drawer': '1', 'Accept': 'text/html' }
            });

            if (response.redirected || response.url.indexOf('/Account/Login') >= 0) {
                window.location.href = response.url || '/Account/Login';
                return;
            }

            if (!response.ok) {
                content.innerHTML = '<div class="empty-panel"><i data-lucide="triangle-alert"></i><span>Unable to load this action form.</span></div>';
                initEnhancements(content);
                return;
            }

            var html = await response.text();
            if (html.indexOf('name="Password"') >= 0 && html.indexOf('Sign in') >= 0) {
                window.location.href = '/Account/Login';
                return;
            }
            content.innerHTML = html;
            initEnhancements(content);
            var firstField = content.querySelector('textarea, select, input:not([type="hidden"])');
            if (firstField) firstField.focus({ preventScroll: true });
        }

        document.addEventListener('click', function (ev) {
            var trigger = ev.target.closest('[data-capa-drawer-url]');
            if (trigger) {
                ev.preventDefault();
                loadDrawer(trigger).catch(function () {
                    content.innerHTML = '<div class="empty-panel"><i data-lucide="triangle-alert"></i><span>Unable to load this form.</span></div>';
                    initEnhancements(content);
                });
                return;
            }

            if (ev.target.closest('[data-capa-drawer-close]')) {
                closeDrawer(ev);
            }
        });

        content.addEventListener('submit', async function (ev) {
            var form = ev.target.closest('form[data-capa-drawer-form]');
            if (!form) return;
            ev.preventDefault();

            var submit = form.querySelector('[type="submit"]');
            if (submit) submit.disabled = true;

            try {
                var response = await fetch(form.action, {
                    method: form.method || 'POST',
                    body: new FormData(form),
                    headers: { 'X-CAPA-Drawer': '1', 'Accept': 'application/json, text/html' }
                });

                var type = response.headers.get('content-type') || '';
                if (response.redirected || response.url.indexOf('/Account/Login') >= 0 || response.status === 401 || response.status === 403) {
                    window.location.href = response.url || '/Account/Login';
                    return;
                }

                if (type.indexOf('application/json') >= 0) {
                    var data = await response.json();
                    window.location.href = data.redirectUrl || window.location.href;
                    return;
                }

                var html = await response.text();
                if (html.indexOf('name="Password"') >= 0 && html.indexOf('Sign in') >= 0) {
                    window.location.href = '/Account/Login';
                    return;
                }
                content.innerHTML = html;
                initEnhancements(content);
            } finally {
                submit = content.querySelector('[type="submit"]');
                if (submit) submit.disabled = false;
            }
        });

        backdrop.addEventListener('click', closeDrawer);
        document.addEventListener('keydown', function (ev) {
            if (ev.key === 'Escape' && drawer.classList.contains('is-open')) closeDrawer(ev);
        });
    });
})();
