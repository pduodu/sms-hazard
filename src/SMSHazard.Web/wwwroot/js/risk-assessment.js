(function () {
    function bandOf(score) {
        if (score <= 4) return { name: 'Low', tone: 'risk-tone-low' };
        if (score <= 9) return { name: 'Medium', tone: 'risk-tone-medium' };
        if (score <= 15) return { name: 'High', tone: 'risk-tone-high' };
        return { name: 'Extreme', tone: 'risk-tone-extreme' };
    }

    function initPanel(panel) {
        if (panel.dataset.riskReady === '1') return;
        panel.dataset.riskReady = '1';

        var likelihood = panel.querySelector('[data-risk-likelihood]');
        var severity = panel.querySelector('[data-risk-severity]');
        var box = panel.querySelector('[data-risk-box]');
        var scoreEl = panel.querySelector('[data-risk-score]');
        var levelEl = panel.querySelector('[data-risk-level]');
        var matrix = panel.querySelector('[data-risk-matrix]');

        function update() {
            if (!likelihood || !severity || !box || !scoreEl || !levelEl) return;
            var l = parseInt(likelihood.value || '0', 10);
            var s = parseInt(severity.value || '0', 10);
            var score = l * s;
            var band = bandOf(score);
            box.classList.remove('risk-tone-low', 'risk-tone-medium', 'risk-tone-high', 'risk-tone-extreme');
            box.classList.add(band.tone);
            scoreEl.textContent = score;
            levelEl.textContent = band.name;

            panel.querySelectorAll('[data-risk-cell]').forEach(function (cell) {
                cell.classList.toggle('is-selected', parseInt(cell.dataset.l, 10) === l && parseInt(cell.dataset.s, 10) === s);
            });
        }

        if (matrix && !matrix.children.length) {
            for (var l = 5; l >= 1; l--) {
                var tr = document.createElement('tr');
                var th = document.createElement('th');
                th.textContent = l;
                tr.appendChild(th);
                for (var s = 1; s <= 5; s++) {
                    var td = document.createElement('td');
                    var score = l * s;
                    var band = bandOf(score);
                    td.textContent = score;
                    td.dataset.riskCell = '1';
                    td.dataset.l = String(l);
                    td.dataset.s = String(s);
                    td.className = band.tone;
                    tr.appendChild(td);
                }
                matrix.appendChild(tr);
            }
        }

        [likelihood, severity].forEach(function (el) {
            if (!el) return;
            el.addEventListener('change', update);
            if (window.jQuery) jQuery(el).on('select2:select select2:clear', update);
        });

        update();
    }

    window.initRiskAssessment = function (root) {
        (root || document).querySelectorAll('[data-risk-assessment]').forEach(initPanel);
    };

    document.addEventListener('DOMContentLoaded', function () {
        window.initRiskAssessment(document);
    });
})();
