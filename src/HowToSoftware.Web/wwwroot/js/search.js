/**
 * HowToSoftware — Search modal
 * Client-side search using the Content API /api/content/search endpoint.
 */
(function () {
    'use strict';

    var modal = document.getElementById('search-modal');
    var input = document.getElementById('search-input');
    var resultsContainer = document.getElementById('search-results');
    var overlay = modal ? modal.querySelector('.search-modal-overlay') : null;
    var closeBtn = modal ? modal.querySelector('.search-close') : null;
    var triggers = document.querySelectorAll('.gh-search');

    var DEBOUNCE_MS = 300;
    var debounceTimer = null;

    function openModal() {
        if (!modal) return;
        modal.classList.add('is-active');
        document.body.classList.add('search-open');
        input.value = '';
        resultsContainer.innerHTML = '';
        // Small delay so CSS transition finishes before focusing
        setTimeout(function () { input.focus(); }, 100);
    }

    function closeModal() {
        if (!modal) return;
        modal.classList.remove('is-active');
        document.body.classList.remove('search-open');
        resultsContainer.innerHTML = '';
    }

    // Wire up open triggers
    for (var i = 0; i < triggers.length; i++) {
        triggers[i].addEventListener('click', function (e) {
            e.preventDefault();
            openModal();
        });
    }

    // Close on overlay click, close button, Escape
    if (overlay) overlay.addEventListener('click', closeModal);
    if (closeBtn) closeBtn.addEventListener('click', closeModal);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && modal && modal.classList.contains('is-active')) {
            closeModal();
        }
        // Ctrl/Cmd+K to open search
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            if (modal && modal.classList.contains('is-active')) {
                closeModal();
            } else {
                openModal();
            }
        }
    });

    // Live search on input
    if (input) {
        input.addEventListener('input', function () {
            clearTimeout(debounceTimer);
            var query = input.value.trim();
            if (query.length < 2) {
                resultsContainer.innerHTML = '';
                return;
            }
            debounceTimer = setTimeout(function () {
                performSearch(query);
            }, DEBOUNCE_MS);
        });
    }

    function performSearch(query) {
        var url = '/api/content/search/?q=' + encodeURIComponent(query)
            + '&include=tags,authors&limit=10&fields=title,slug,feature_image,custom_excerpt,published_at';

        fetch(url)
            .then(function (res) { return res.json(); })
            .then(function (data) {
                renderResults(data, query);
            })
            .catch(function () {
                resultsContainer.innerHTML = '<p class="search-no-results">Search unavailable.</p>';
            });
    }

    function renderResults(data, query) {
        if (!data || !data.posts || data.posts.length === 0) {
            resultsContainer.innerHTML = '<p class="search-no-results">No results found for \u201C' + escapeHtml(query) + '\u201D</p>';
            return;
        }

        var html = '<ul class="search-results-list">';
        for (var i = 0; i < data.posts.length; i++) {
            var post = data.posts[i];
            html += '<li class="search-result-item">';
            html += '<a href="/' + escapeHtml(post.slug) + '/">';
            if (post.feature_image) {
                html += '<img class="search-result-image" src="' + escapeHtml(post.feature_image) + '?w=100" alt="" loading="lazy" />';
            }
            html += '<div class="search-result-text">';
            html += '<h4 class="search-result-title">' + escapeHtml(post.title) + '</h4>';
            if (post.custom_excerpt) {
                html += '<p class="search-result-excerpt">' + escapeHtml(truncate(post.custom_excerpt, 120)) + '</p>';
            }
            if (post.published_at) {
                html += '<time class="search-result-date">' + formatDate(post.published_at) + '</time>';
            }
            html += '</div></a></li>';
        }
        html += '</ul>';

        if (data.meta && data.meta.pagination && data.meta.pagination.total > data.posts.length) {
            html += '<p class="search-total">Showing ' + data.posts.length + ' of ' + data.meta.pagination.total + ' results</p>';
        }

        resultsContainer.innerHTML = html;
    }

    function escapeHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    function truncate(str, max) {
        if (!str || str.length <= max) return str;
        return str.substring(0, max).trimEnd() + '\u2026';
    }

    function formatDate(iso) {
        try {
            var d = new Date(iso);
            return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
        } catch (e) {
            return '';
        }
    }
})();
