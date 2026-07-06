(function () {
    'use strict';

    const section = document.getElementById('comments');
    if (!section) return;

    const postId = section.dataset.postId;
    const listEl = document.getElementById('comments-list');
    const countEl = document.getElementById('comment-count');
    const paginationEl = document.getElementById('comments-pagination');
    const form = document.getElementById('comment-form');

    let currentPage = 1;
    const pageSize = 20;

    // ── Load comments ──────────────────────────────────

    async function loadComments(page) {
        currentPage = page;
        try {
            const res = await fetch(`/api/comments/post/${encodeURIComponent(postId)}?page=${page}&limit=${pageSize}`);
            if (!res.ok) return;
            const data = await res.json();
            renderComments(data.comments);
            renderPagination(data.meta.pagination);
        } catch { /* silently fail */ }
    }

    async function loadCount() {
        try {
            const res = await fetch(`/api/comments/post/${encodeURIComponent(postId)}/count`);
            if (!res.ok) return;
            const data = await res.json();
            countEl.textContent = data.count;
        } catch { /* silently fail */ }
    }

    // ── Render ──────────────────────────────────────────

    function renderComments(comments) {
        listEl.innerHTML = '';
        if (comments.length === 0 && currentPage === 1) {
            listEl.innerHTML = '<p class="comment-empty">No comments yet. Be the first to share your thoughts!</p>';
            return;
        }
        comments.forEach(function (c) {
            listEl.appendChild(buildCommentEl(c, false));
        });
    }

    function buildCommentEl(c, isReply) {
        const el = document.createElement('div');
        el.className = 'comment' + (isReply ? ' comment-reply' : '');
        el.dataset.id = c.id;

        const memberName = c.member ? (c.member.name || 'Anonymous') : 'Deleted member';
        const expertise = c.member && c.member.expertise ? c.member.expertise : '';
        const timeAgo = formatTimeAgo(c.createdAt);
        const edited = c.editedAt ? ' (edited)' : '';

        el.innerHTML =
            '<div class="comment-header">' +
                '<span class="comment-author">' + escapeHtml(memberName) + '</span>' +
                (expertise ? '<span class="comment-expertise">' + escapeHtml(expertise) + '</span>' : '') +
                '<time class="comment-time" datetime="' + c.createdAt + '">' + timeAgo + edited + '</time>' +
            '</div>' +
            '<div class="comment-body">' + c.html + '</div>' +
            '<div class="comment-actions">' +
                '<button class="comment-action-btn comment-like-btn' + (c.liked ? ' liked' : '') + '" data-id="' + c.id + '">' +
                    '<svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor"><path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 ' +
                    '2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/></svg> ' +
                    '<span class="like-count">' + c.likeCount + '</span>' +
                '</button>' +
                (!isReply ? '<button class="comment-action-btn comment-reply-btn" data-id="' + c.id + '">Reply</button>' : '') +
                '<button class="comment-action-btn comment-report-btn" data-id="' + c.id + '">Report</button>' +
            '</div>';

        // Render replies
        if (c.replies && c.replies.length > 0) {
            var repliesContainer = document.createElement('div');
            repliesContainer.className = 'comment-replies';
            c.replies.forEach(function (r) {
                repliesContainer.appendChild(buildCommentEl(r, true));
            });
            el.appendChild(repliesContainer);
        }

        return el;
    }

    function renderPagination(pagination) {
        paginationEl.innerHTML = '';
        if (pagination.pages <= 1) return;

        if (pagination.page > 1) {
            var prev = document.createElement('button');
            prev.className = 'comment-page-btn';
            prev.textContent = '\u2190 Newer';
            prev.addEventListener('click', function () { loadComments(pagination.page - 1); });
            paginationEl.appendChild(prev);
        }
        if (pagination.page < pagination.pages) {
            var next = document.createElement('button');
            next.className = 'comment-page-btn';
            next.textContent = 'Older \u2192';
            next.addEventListener('click', function () { loadComments(pagination.page + 1); });
            paginationEl.appendChild(next);
        }
    }

    // ── Submit new comment ──────────────────────────────

    if (form) {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            var input = document.getElementById('comment-input');
            var html = escapeHtml(input.value.trim());
            if (!html) return;

            // Wrap plain text in a paragraph
            html = '<p>' + html.replace(/\n{2,}/g, '</p><p>').replace(/\n/g, '<br>') + '</p>';

            var parentId = form.dataset.replyTo || null;

            var btn = form.querySelector('.comment-submit-btn');
            btn.disabled = true;

            try {
                var res = await fetch('/api/comments', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ postId: postId, html: html, parentId: parentId })
                });

                if (res.ok) {
                    input.value = '';
                    delete form.dataset.replyTo;
                    form.classList.remove('replying');
                    var cancelBtn = form.querySelector('.comment-cancel-reply');
                    if (cancelBtn) cancelBtn.remove();
                    await loadComments(1);
                    await loadCount();
                }
            } finally {
                btn.disabled = false;
            }
        });
    }

    // ── Delegated event handlers ────────────────────────

    listEl.addEventListener('click', async function (e) {
        var target = e.target.closest('.comment-action-btn');
        if (!target) return;

        var commentId = target.dataset.id;

        // Like / unlike
        if (target.classList.contains('comment-like-btn')) {
            var isLiked = target.classList.contains('liked');
            var method = isLiked ? 'DELETE' : 'POST';
            var res = await fetch('/api/comments/' + encodeURIComponent(commentId) + '/like', { method: method });
            if (res.ok) {
                target.classList.toggle('liked');
                var countSpan = target.querySelector('.like-count');
                var n = parseInt(countSpan.textContent, 10) || 0;
                countSpan.textContent = isLiked ? Math.max(0, n - 1) : n + 1;
            } else if (res.status === 401) {
                window.location.href = '/signin/';
            }
            return;
        }

        // Reply
        if (target.classList.contains('comment-reply-btn') && form) {
            form.dataset.replyTo = commentId;
            form.classList.add('replying');
            // Add cancel button if not present
            if (!form.querySelector('.comment-cancel-reply')) {
                var cancel = document.createElement('button');
                cancel.type = 'button';
                cancel.className = 'comment-cancel-reply';
                cancel.textContent = 'Cancel reply';
                cancel.addEventListener('click', function () {
                    delete form.dataset.replyTo;
                    form.classList.remove('replying');
                    cancel.remove();
                });
                form.appendChild(cancel);
            }
            form.querySelector('textarea').focus();
            form.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return;
        }

        // Report
        if (target.classList.contains('comment-report-btn')) {
            if (!confirm('Report this comment?')) return;
            var res = await fetch('/api/comments/' + encodeURIComponent(commentId) + '/report', { method: 'POST' });
            if (res.ok) {
                target.textContent = 'Reported';
                target.disabled = true;
            } else if (res.status === 401) {
                window.location.href = '/signin/';
            }
        }
    });

    // ── Helpers ──────────────────────────────────────────

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    function formatTimeAgo(isoString) {
        var date = new Date(isoString);
        var seconds = Math.floor((Date.now() - date.getTime()) / 1000);
        if (seconds < 60) return 'just now';
        var minutes = Math.floor(seconds / 60);
        if (minutes < 60) return minutes + 'm ago';
        var hours = Math.floor(minutes / 60);
        if (hours < 24) return hours + 'h ago';
        var days = Math.floor(hours / 24);
        if (days < 30) return days + 'd ago';
        return date.toLocaleDateString();
    }

    // ── Init ────────────────────────────────────────────

    loadComments(1);
    loadCount();
})();
