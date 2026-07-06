window.countdownTimer = (function () {
    let intervalId = null;

    function pad(n) {
        return n < 10 ? '0' + n : '' + n;
    }

    function update() {
        var elements = document.querySelectorAll('[data-countdown]');
        var now = Date.now();
        elements.forEach(function (el) {
            var target = parseInt(el.getAttribute('data-countdown'), 10);
            var diff = target - now;
            if (diff <= 0) {
                el.textContent = 'Publishing now…';
                el.classList.add('text-success');
                return;
            }
            var days = Math.floor(diff / 86400000);
            var hours = Math.floor((diff % 86400000) / 3600000);
            var minutes = Math.floor((diff % 3600000) / 60000);
            var seconds = Math.floor((diff % 60000) / 1000);
            if (days > 0) {
                el.textContent = days + 'd ' + pad(hours) + 'h ' + pad(minutes) + 'm';
            } else if (hours > 0) {
                el.textContent = hours + 'h ' + pad(minutes) + 'm ' + pad(seconds) + 's';
            } else {
                el.textContent = minutes + 'm ' + pad(seconds) + 's';
            }
        });
    }

    function start() {
        stop();
        update();
        intervalId = setInterval(update, 1000);
    }

    function stop() {
        if (intervalId) {
            clearInterval(intervalId);
            intervalId = null;
        }
    }

    return { start: start, stop: stop };
})();
