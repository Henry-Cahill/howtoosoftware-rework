// Page tree drag-and-drop interop for Blazor
let dotNetRef = null;
let draggedId = null;

export function init(ref) {
    dotNetRef = ref;
    bindDragEvents();
}

export function bindDragEvents() {
    document.querySelectorAll('[data-page-draggable]').forEach(el => {
        el.setAttribute('draggable', 'true');

        el.addEventListener('dragstart', e => {
            draggedId = el.dataset.pageId;
            el.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', draggedId);
        });

        el.addEventListener('dragend', () => {
            draggedId = null;
            el.classList.remove('dragging');
            document.querySelectorAll('.drop-above, .drop-below, .drop-child').forEach(
                d => d.classList.remove('drop-above', 'drop-below', 'drop-child')
            );
        });
    });

    document.querySelectorAll('[data-page-drop]').forEach(el => {
        el.addEventListener('dragover', e => {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';

            const targetId = el.dataset.pageId;
            if (targetId === draggedId) return;

            const rect = el.getBoundingClientRect();
            const y = e.clientY - rect.top;
            const height = rect.height;

            el.classList.remove('drop-above', 'drop-below', 'drop-child');

            if (y < height * 0.25) {
                el.classList.add('drop-above');
            } else if (y > height * 0.75) {
                el.classList.add('drop-below');
            } else {
                el.classList.add('drop-child');
            }
        });

        el.addEventListener('dragleave', () => {
            el.classList.remove('drop-above', 'drop-below', 'drop-child');
        });

        el.addEventListener('drop', e => {
            e.preventDefault();

            const targetId = el.dataset.pageId;
            if (!draggedId || targetId === draggedId) return;

            const rect = el.getBoundingClientRect();
            const y = e.clientY - rect.top;
            const height = rect.height;

            let position;
            if (y < height * 0.25) {
                position = 'above';
            } else if (y > height * 0.75) {
                position = 'below';
            } else {
                position = 'child';
            }

            el.classList.remove('drop-above', 'drop-below', 'drop-child');

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnPageDropped', draggedId, targetId, position);
            }
        });
    });
}

export function destroy() {
    dotNetRef = null;
    draggedId = null;
}
