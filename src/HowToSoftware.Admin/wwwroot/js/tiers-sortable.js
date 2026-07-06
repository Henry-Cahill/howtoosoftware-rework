// Tiers drag-and-drop reorder interop using SortableJS.
// Returns the full ordered list of product ids so the server doesn't have
// to maintain client-side index state.
let dotNetRef = null;
let sortableInstance = null;

export function init(ref) {
    dotNetRef = ref;
    initSortable();
}

export function initSortable() {
    const container = document.getElementById('tiers-sortable');
    if (!container) return;

    if (sortableInstance) {
        sortableInstance.destroy();
    }

    sortableInstance = Sortable.create(container, {
        animation: 150,
        ghostClass: 'sortable-ghost',
        dragClass: 'sortable-drag',
        handle: '[data-sortable-handle]',
        onEnd: async (evt) => {
            if (!dotNetRef) return;
            if (evt.oldIndex === evt.newIndex) return;

            const ids = Array.from(container.querySelectorAll('[data-tier-id]'))
                .map(el => el.getAttribute('data-tier-id'))
                .filter(id => !!id);

            await dotNetRef.invokeMethodAsync('OnTiersReordered', ids);
        }
    });
}

export function destroy() {
    if (sortableInstance) {
        sortableInstance.destroy();
        sortableInstance = null;
    }
    dotNetRef = null;
}
