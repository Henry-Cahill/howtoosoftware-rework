/**
 * HowToSoftware Analytics — Lightweight tracking script
 * Captures: page views, sessions, referrer, UTM, device/browser/OS, member UUID
 */
(function () {
    'use strict';

    var ENDPOINT = '/api/analytics/event';
    var SESSION_KEY = 'hts_sid';
    var SESSION_TTL = 30 * 60 * 1000; // 30 minutes

    // ── Session management ──────────────────────────────────────
    function getOrCreateSession() {
        var now = Date.now();
        try {
            var raw = sessionStorage.getItem(SESSION_KEY);
            if (raw) {
                var s = JSON.parse(raw);
                if (s.id && s.ts && now - s.ts < SESSION_TTL) {
                    s.ts = now;
                    sessionStorage.setItem(SESSION_KEY, JSON.stringify(s));
                    return s.id;
                }
            }
        } catch (_) { /* storage unavailable */ }
        var id = crypto.randomUUID ? crypto.randomUUID() : generateId();
        try {
            sessionStorage.setItem(SESSION_KEY, JSON.stringify({ id: id, ts: now }));
        } catch (_) { /* ignore */ }
        return id;
    }

    function generateId() {
        var chars = 'abcdef0123456789';
        var parts = [8, 4, 4, 4, 12];
        var totalLen = parts.reduce(function (a, b) { return a + b; }, 0);
        var rnd = new Uint8Array(totalLen);
        crypto.getRandomValues(rnd);
        var idx = 0;
        return parts.map(function (len) {
            var s = '';
            for (var i = 0; i < len; i++) {
                // chars.length (16) divides 256 evenly, so no modulo bias
                s += chars[rnd[idx++] % chars.length];
            }
            return s;
        }).join('-');
    }

    // ── UTM extraction ──────────────────────────────────────────
    function getUtmParams() {
        var params = {};
        try {
            var search = new URLSearchParams(window.location.search);
            ['utm_source', 'utm_medium', 'utm_campaign', 'utm_content', 'utm_term'].forEach(function (key) {
                var val = search.get(key);
                if (val) params[key] = val;
            });
        } catch (_) { /* IE fallback: skip UTM */ }
        return params;
    }

    // ── User-Agent parsing ──────────────────────────────────────
    function parseUA() {
        var ua = navigator.userAgent || '';
        return {
            device: detectDevice(ua),
            browser: detectBrowser(ua),
            os: detectOS(ua)
        };
    }

    function detectDevice(ua) {
        if (/Mobi|Android.*Mobile|iPhone|iPod/i.test(ua)) return 'Mobile';
        if (/iPad|Android(?!.*Mobile)|Tablet/i.test(ua)) return 'Tablet';
        return 'Desktop';
    }

    function detectBrowser(ua) {
        if (/Edg\//i.test(ua)) return 'Edge';
        if (/OPR\/|Opera/i.test(ua)) return 'Opera';
        if (/Chrome\/(?!.*Edg)/i.test(ua)) return 'Chrome';
        if (/Safari\/(?!.*Chrome)/i.test(ua)) return 'Safari';
        if (/Firefox\//i.test(ua)) return 'Firefox';
        if (/Trident|MSIE/i.test(ua)) return 'IE';
        return 'Other';
    }

    function detectOS(ua) {
        if (/Windows/i.test(ua)) return 'Windows';
        if (/Mac OS X|Macintosh/i.test(ua)) return 'macOS';
        if (/iPhone|iPad|iPod/i.test(ua)) return 'iOS';
        if (/Android/i.test(ua)) return 'Android';
        if (/Linux/i.test(ua)) return 'Linux';
        if (/CrOS/i.test(ua)) return 'ChromeOS';
        return 'Other';
    }

    // ── Referrer cleanup ────────────────────────────────────────
    function cleanReferrer() {
        var ref = document.referrer;
        if (!ref) return null;
        try {
            var refHost = new URL(ref).hostname;
            if (refHost === window.location.hostname) return null; // internal
            return ref;
        } catch (_) {
            return ref;
        }
    }

    // ── Send event ──────────────────────────────────────────────
    function sendEvent() {
        var uaInfo = parseUA();
        var utm = getUtmParams();

        var payload = {
            session_id: getOrCreateSession(),
            action: 'page_view',
            page_url: window.location.href,
            page_url_path: window.location.pathname,
            referrer: cleanReferrer(),
            device: uaInfo.device,
            browser: uaInfo.browser,
            os: uaInfo.os,
            member_uuid: getMemberUuid(),
            timestamp: new Date().toISOString()
        };

        if (Object.keys(utm).length > 0) {
            payload.utm = utm;
        }

        var blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
        if (navigator.sendBeacon) {
            navigator.sendBeacon(ENDPOINT, blob);
        } else {
            var xhr = new XMLHttpRequest();
            xhr.open('POST', ENDPOINT, true);
            xhr.setRequestHeader('Content-Type', 'application/json');
            xhr.send(JSON.stringify(payload));
        }
    }

    // ── Member UUID ─────────────────────────────────────────────
    function getMemberUuid() {
        var el = document.querySelector('meta[name="hts:member-uuid"]');
        return el ? el.getAttribute('content') : null;
    }

    // ── Init ────────────────────────────────────────────────────
    // Respect Do Not Track (optional, but good practice)
    if (navigator.doNotTrack === '1' || window.doNotTrack === '1') {
        return;
    }

    // Fire on page load
    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        sendEvent();
    } else {
        document.addEventListener('DOMContentLoaded', sendEvent);
    }
})();
