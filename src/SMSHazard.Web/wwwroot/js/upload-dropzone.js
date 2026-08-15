(function () {
    function ensureFallbackStyles() {
        if (document.getElementById('sms-dropzone-fallback-styles')) return;

        var style = document.createElement('style');
        style.id = 'sms-dropzone-fallback-styles';
        style.textContent = [
            '.dropzone-upload{position:relative;display:flex;align-items:center;gap:.85rem;width:100%;min-height:5.5rem;padding:1rem;border:1.5px dashed rgba(16,134,212,.42);border-radius:.85rem;background:linear-gradient(180deg,rgba(255,255,255,.96),rgba(248,251,254,.96));box-shadow:inset 0 0 0 1px rgba(207,230,247,.52),0 8px 20px rgba(1,49,97,.06);cursor:pointer;}',
            '.dropzone-icon{display:inline-flex;align-items:center;justify-content:center;width:2.75rem;height:2.75rem;border-radius:.85rem;color:#fff;background:linear-gradient(135deg,#1086d4 0%,#045c9d 65%,#013161 100%);flex:0 0 auto;}',
            '.dropzone-copy{display:flex;flex-direction:column;gap:.15rem;min-width:0;}',
            '.dropzone-copy strong{color:#013161;font-size:.95rem;}',
            '.dropzone-copy small{color:#64748b;font-size:.78rem;overflow-wrap:anywhere;}',
            '.dropzone-native-input{position:absolute!important;inset:0!important;width:100%!important;height:100%!important;opacity:0!important;cursor:pointer!important;color:transparent!important;}'
        ].join('');
        document.head.appendChild(style);
    }

    function formatSize(bytes) {
        if (!bytes) return '0 KB';
        var units = ['B', 'KB', 'MB', 'GB'];
        var size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.length - 1) {
            size = size / 1024;
            unit++;
        }
        return (unit === 0 ? size : size.toFixed(size >= 10 ? 0 : 1)) + ' ' + units[unit];
    }

    function fileKey(file) {
        return [file.name, file.size, file.lastModified || 0].join('|');
    }

    function filesFrom(list) {
        return Array.prototype.slice.call(list || []);
    }

    function init(input) {
        if (!window.DataTransfer || input.dataset.dropzoneBound === 'true' || input.dataset.dropzone === 'off') return;
        ensureFallbackStyles();
        input.dataset.dropzoneBound = 'true';

        var accepts = input.getAttribute('accept');
        var multiple = input.multiple;
        var originalClasses = input.className || '';
        var isCompact = originalClasses.indexOf('max-w-md') !== -1;
        var dt = new DataTransfer();
        var suppressNativeChange = false;

        var shell = document.createElement('div');
        shell.className = 'dropzone-upload' + (isCompact ? ' max-w-md' : '');
        shell.setAttribute('role', 'button');
        shell.setAttribute('tabindex', '0');
        shell.setAttribute('aria-label', multiple ? 'Add files' : 'Add file');

        var icon = document.createElement('span');
        icon.className = 'dropzone-icon';
        icon.innerHTML = '<i data-lucide="upload-cloud"></i>';

        var copy = document.createElement('span');
        copy.className = 'dropzone-copy';
        copy.innerHTML = '<strong>Drop files here</strong><small>or click to browse' + (accepts ? ' - ' + accepts.replace(/,/g, ', ') : '') + '</small>';

        var list = document.createElement('div');
        list.className = 'dropzone-list';
        list.setAttribute('aria-live', 'polite');

        input.classList.add('dropzone-native-input');
        input.removeAttribute('class');
        input.className = 'dropzone-native-input';
        input.style.position = 'absolute';
        input.style.inset = '0';
        input.style.width = '100%';
        input.style.height = '100%';
        input.style.opacity = '0';
        input.style.cursor = 'pointer';
        input.style.color = 'transparent';
        input.parentNode.insertBefore(shell, input);
        shell.appendChild(icon);
        shell.appendChild(copy);
        shell.appendChild(input);
        shell.parentNode.insertBefore(list, shell.nextSibling);

        function syncInput() {
            suppressNativeChange = true;
            input.files = dt.files;
            suppressNativeChange = false;
        }

        function render() {
            var files = filesFrom(dt.files);
            shell.classList.toggle('has-files', files.length > 0);
            list.innerHTML = '';

            if (!files.length) return;

            files.forEach(function (file, index) {
                var item = document.createElement('div');
                item.className = 'dropzone-file';

                var thumb = document.createElement('span');
                thumb.className = 'dropzone-file-thumb';

                if (file.type && file.type.indexOf('image/') === 0) {
                    var img = document.createElement('img');
                    img.alt = '';
                    img.src = URL.createObjectURL(file);
                    thumb.appendChild(img);
                } else {
                    thumb.innerHTML = '<i data-lucide="file-text"></i>';
                }

                var meta = document.createElement('span');
                meta.className = 'dropzone-file-meta';

                var name = document.createElement('span');
                name.className = 'dropzone-file-name';
                name.textContent = file.name;

                var size = document.createElement('small');
                size.textContent = formatSize(file.size);

                var remove = document.createElement('button');
                remove.type = 'button';
                remove.className = 'dropzone-remove';
                remove.setAttribute('aria-label', 'Remove ' + file.name);
                remove.innerHTML = '<i data-lucide="x"></i>';
                remove.addEventListener('click', function (event) {
                    event.preventDefault();
                    event.stopPropagation();
                    removeAt(index);
                });

                meta.appendChild(name);
                meta.appendChild(size);
                item.appendChild(thumb);
                item.appendChild(meta);
                item.appendChild(remove);
                list.appendChild(item);
            });

            if (window.lucide) lucide.createIcons();
        }

        function addFiles(fileList) {
            var incoming = filesFrom(fileList);
            if (!incoming.length) return;

            var next = multiple ? dt : new DataTransfer();
            var seen = {};
            filesFrom(next.files).forEach(function (file) {
                seen[fileKey(file)] = true;
            });

            incoming.forEach(function (file) {
                var key = fileKey(file);
                if (!seen[key]) {
                    next.items.add(file);
                    seen[key] = true;
                }
            });

            dt = next;
            syncInput();
            render();
        }

        function replaceFiles(fileList) {
            var next = new DataTransfer();
            filesFrom(fileList).forEach(function (file) {
                next.items.add(file);
            });
            dt = next;
            syncInput();
            render();
        }

        function removeAt(index) {
            var next = new DataTransfer();
            filesFrom(dt.files).forEach(function (file, fileIndex) {
                if (fileIndex !== index) next.items.add(file);
            });
            dt = next;
            syncInput();
            render();
        }

        function removeFile(target) {
            var key = fileKey(target);
            var next = new DataTransfer();
            filesFrom(dt.files).forEach(function (file) {
                if (fileKey(file) !== key) next.items.add(file);
            });
            dt = next;
            syncInput();
            render();
        }

        input._smsDropzone = {
            add: addFiles,
            replace: replaceFiles,
            removeFile: removeFile,
            render: render
        };

        input.addEventListener('change', function () {
            if (suppressNativeChange) return;
            addFiles(input.files);
        });

        shell.addEventListener('click', function (event) {
            if (event.target === input) return;
            input.click();
        });

        shell.addEventListener('keydown', function (event) {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                input.click();
            }
        });

        ['dragenter', 'dragover'].forEach(function (eventName) {
            shell.addEventListener(eventName, function (event) {
                event.preventDefault();
                shell.classList.add('is-dragging');
            });
        });

        ['dragleave', 'dragend', 'drop'].forEach(function (eventName) {
            shell.addEventListener(eventName, function () {
                shell.classList.remove('is-dragging');
            });
        });

        shell.addEventListener('drop', function (event) {
            event.preventDefault();
            addFiles(event.dataTransfer.files);
        });

        if (input.files && input.files.length) replaceFiles(input.files);
        if (window.lucide) lucide.createIcons();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('input[type="file"]:not([data-dropzone="off"])').forEach(init);
    });
})();
