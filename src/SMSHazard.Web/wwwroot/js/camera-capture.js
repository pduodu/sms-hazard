// HR-01 — in-page camera capture that feeds photos into an existing <input type="file" multiple>.
// Uses getUserMedia + canvas; captured photos are merged with any manually chosen files via DataTransfer.
(function () {
    function init(root) {
        var input = document.getElementById(root.getAttribute('data-target'));
        if (!input || !window.DataTransfer) return;

        var dt = new DataTransfer();
        var dropzone = input._smsDropzone || null;
        var video = root.querySelector('[data-cam-video]');
        var canvas = root.querySelector('[data-cam-canvas]');
        var preview = root.querySelector('[data-cam-preview]');
        var startBtn = root.querySelector('[data-cam-start]');
        var captureBtn = root.querySelector('[data-cam-capture]');
        var stopBtn = root.querySelector('[data-cam-stop]');
        var stage = root.querySelector('[data-cam-stage]');
        var stream = null;

        function syncInput(file) {
            if (dropzone && file) {
                dropzone.add([file]);
                return;
            }
            input.files = dt.files;
        }

        function renderPreviews() {
            preview.innerHTML = '';
            Array.prototype.forEach.call(dt.files, function (f, idx) {
                var wrap = document.createElement('div');
                wrap.className = 'relative';
                if (f.type.indexOf('image/') === 0) {
                    var img = document.createElement('img');
                    img.src = URL.createObjectURL(f);
                    img.className = 'w-16 h-16 object-cover rounded border border-slate-200';
                    wrap.appendChild(img);
                } else {
                    var box = document.createElement('div');
                    box.className = 'w-16 h-16 flex items-center justify-center rounded border border-slate-200 bg-slate-50 text-[0.6rem] text-slate-500 p-1 text-center break-all';
                    box.textContent = f.name;
                    wrap.appendChild(box);
                }
                var x = document.createElement('button');
                x.type = 'button';
                x.textContent = '×';
                x.className = 'absolute -top-2 -right-2 w-5 h-5 rounded-full bg-red-600 text-white text-xs leading-none';
                x.addEventListener('click', function () { removeAt(idx); });
                wrap.appendChild(x);
                preview.appendChild(wrap);
            });
        }

        function removeAt(idx) {
            var removed = dt.files[idx];
            var next = new DataTransfer();
            Array.prototype.forEach.call(dt.files, function (f, i) { if (i !== idx) next.items.add(f); });
            dt = next;
            if (dropzone && removed) dropzone.removeFile(removed);
            else syncInput();
            renderPreviews();
        }

        // Merge files the user picks through the normal dialog so captures aren't lost.
        input.addEventListener('change', function () {
            if (dropzone) return;
            if (input.files === dt.files) return; // our own programmatic set
            Array.prototype.forEach.call(input.files, function (f) { dt.items.add(f); });
            syncInput();
            renderPreviews();
        });

        function start() {
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                alert('This browser cannot access the camera.');
                return;
            }
            navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false })
                .then(function (s) {
                    stream = s;
                    video.srcObject = s;
                    stage.classList.remove('hidden');
                    startBtn.classList.add('hidden');
                })
                .catch(function (e) { alert('Could not access the camera: ' + e.message); });
        }

        function stop() {
            if (stream) { stream.getTracks().forEach(function (t) { t.stop(); }); stream = null; }
            stage.classList.add('hidden');
            startBtn.classList.remove('hidden');
        }

        function capture() {
            var w = video.videoWidth, h = video.videoHeight;
            if (!w) return;
            canvas.width = w; canvas.height = h;
            canvas.getContext('2d').drawImage(video, 0, 0, w, h);
            canvas.toBlob(function (blob) {
                if (!blob) return;
                var name = 'camera-' + new Date().getTime() + '.jpg';
                var file = new File([blob], name, { type: 'image/jpeg' });
                dt.items.add(file);
                syncInput(file);
                renderPreviews();
                stop();
            }, 'image/jpeg', 0.9);
        }

        if (startBtn) startBtn.addEventListener('click', start);
        if (captureBtn) captureBtn.addEventListener('click', capture);
        if (stopBtn) stopBtn.addEventListener('click', stop);
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-camera]').forEach(init);
    });
})();
