// Collections drag-and-drop reorder interop using SortableJS
let dotNetRef = null;
let sortableInstance = null;

export function init(ref) {
    dotNetRef = ref;
    initSortable();
}

export function initSortable() {
    const container = document.getElementById('collection-posts-sortable');
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
            const { oldIndex, newIndex } = evt;
            
            if (oldIndex !== newIndex && dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnPostReordered', oldIndex, newIndex);
            }
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
