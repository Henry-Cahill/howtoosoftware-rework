// Tag list drag-to-reorder interop for Blazor
let dotNetRef = null;
let draggedId = null;

export function init(ref) {
    dotNetRef = ref;
    bindDragEvents();
}

export function bindDragEvents() {
    document.querySelectorAll('[data-tag-draggable]').forEach(el => {
        el.setAttribute('draggable', 'true');

        el.addEventListener('dragstart', e => {
            draggedId = el.dataset.tagId;
            el.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', draggedId);
        });

        el.addEventListener('dragend', () => {
            draggedId = null;
            el.classList.remove('dragging');
            document.querySelectorAll('.drop-above, .drop-below').forEach(
                d => d.classList.remove('drop-above', 'drop-below')
            );
        });
    });

    document.querySelectorAll('[data-tag-drop]').forEach(el => {
        el.addEventListener('dragover', e => {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';

            const targetId = el.dataset.tagId;
            if (targetId === draggedId) return;

            const rect = el.getBoundingClientRect();
            const y = e.clientY - rect.top;
            const midpoint = rect.height / 2;

            el.classList.remove('drop-above', 'drop-below');
            if (y < midpoint) {
                el.classList.add('drop-above');
            } else {
                el.classList.add('drop-below');
            }
        });

        el.addEventListener('dragleave', () => {
            el.classList.remove('drop-above', 'drop-below');
        });

        el.addEventListener('drop', e => {
            e.preventDefault();

            const targetId = el.dataset.tagId;
            if (!draggedId || targetId === draggedId) return;

            const rect = el.getBoundingClientRect();
            const y = e.clientY - rect.top;
            const midpoint = rect.height / 2;
            const position = y < midpoint ? 'above' : 'below';

            el.classList.remove('drop-above', 'drop-below');

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnTagDropped', draggedId, targetId, position);
            }
        });
    });
}

export function destroy() {
    dotNetRef = null;
    draggedId = null;
}
