// ─────────────────────────────────────────────────────────────────
// Lexical Editor — JS Interop for HowToSoftware Admin (Blazor Server)
// Imports Lexical from esm.sh CDN, exposes init/getContent/setContent/destroy
// ─────────────────────────────────────────────────────────────────

const LEXICAL_V = '0.21.0';
const CDN = 'https://esm.sh';
const deps = `?deps=lexical@${LEXICAL_V}`;

let L, RichText, List, Link, Selection, History, Utils, HtmlModule;
let editorInstance = null;
let cleanupFns = [];
let toolbarEl = null;
let dotNetRef = null;

// ─── Dynamic Imports ───────────────────────────────────────────
async function loadLexical() {
    if (L) return;
    [L, RichText, List, Link, Selection, History, Utils, HtmlModule] = await Promise.all([
        import(`${CDN}/lexical@${LEXICAL_V}`),
        import(`${CDN}/@lexical/rich-text@${LEXICAL_V}${deps}`),
        import(`${CDN}/@lexical/list@${LEXICAL_V}${deps}`),
        import(`${CDN}/@lexical/link@${LEXICAL_V}${deps}`),
        import(`${CDN}/@lexical/selection@${LEXICAL_V}${deps}`),
        import(`${CDN}/@lexical/history@${LEXICAL_V}${deps}`),
        import(`${CDN}/@lexical/utils@${LEXICAL_V}${deps}`),
        import(`${CDN}/@lexical/html@${LEXICAL_V}${deps}`),
    ]);
}

// ─── Custom Nodes ──────────────────────────────────────────────

function createImageNodeClass() {
    class ImageNode extends L.DecoratorNode {
        static getType() { return 'image'; }

        static clone(node) {
            return new ImageNode(node.__src, node.__alt, node.__width, node.__height, node.__caption, node.__cardWidth, node.__key);
        }

        constructor(src, alt, width, height, caption, cardWidth, key) {
            super(key);
            this.__src = src;
            this.__alt = alt || '';
            this.__width = width || 0;
            this.__height = height || 0;
            this.__caption = caption || '';
            this.__cardWidth = cardWidth || '';
        }

        createDOM(config) {
            const figure = document.createElement('figure');
            figure.className = 'le-image-card';
            figure.contentEditable = 'false';
            const img = document.createElement('img');
            img.src = this.__src;
            img.alt = this.__alt;
            if (this.__width) img.width = this.__width;
            if (this.__height) img.height = this.__height;
            img.style.maxWidth = '100%';
            img.style.height = 'auto';
            img.style.borderRadius = '4px';
            figure.appendChild(img);
            if (this.__caption) {
                const cap = document.createElement('figcaption');
                cap.textContent = this.__caption;
                cap.style.textAlign = 'center';
                cap.style.fontSize = '0.875rem';
                cap.style.color = '#6c757d';
                cap.style.marginTop = '0.5rem';
                figure.appendChild(cap);
            }
            return figure;
        }

        updateDOM() { return false; }

        exportJSON() {
            return {
                type: 'image',
                version: 1,
                src: this.__src,
                alt: this.__alt,
                width: this.__width,
                height: this.__height,
                caption: this.__caption,
                cardWidth: this.__cardWidth,
            };
        }

        static importJSON(json) {
            return new ImageNode(json.src, json.alt, json.width, json.height, json.caption, json.cardWidth);
        }

        decorate() { return null; }
        isInline() { return false; }
    }
    return ImageNode;
}

function createHorizontalRuleNodeClass() {
    class HorizontalRuleNode extends L.DecoratorNode {
        static getType() { return 'horizontalrule'; }

        static clone(node) {
            return new HorizontalRuleNode(node.__key);
        }

        constructor(key) {
            super(key);
        }

        createDOM() {
            const div = document.createElement('div');
            div.contentEditable = 'false';
            div.style.margin = '1.5rem 0';
            const hr = document.createElement('hr');
            hr.style.border = 'none';
            hr.style.borderTop = '1px solid #dee2e6';
            div.appendChild(hr);
            return div;
        }

        updateDOM() { return false; }

        exportJSON() {
            return { type: 'horizontalrule', version: 1 };
        }

        static importJSON() {
            return new HorizontalRuleNode();
        }

        decorate() { return null; }
        isInline() { return false; }
    }
    return HorizontalRuleNode;
}

function createCodeBlockNodeClass() {
    class CodeBlockNode extends L.DecoratorNode {
        static getType() { return 'codeblock'; }

        static clone(node) {
            return new CodeBlockNode(node.__code, node.__language, node.__caption, node.__key);
        }

        constructor(code, language, caption, key) {
            super(key);
            this.__code = code || '';
            this.__language = language || '';
            this.__caption = caption || '';
        }

        createDOM() {
            const wrapper = document.createElement('div');
            wrapper.className = 'le-code-block';
            wrapper.contentEditable = 'false';

            const pre = document.createElement('pre');
            pre.style.background = '#1e1e2e';
            pre.style.color = '#cdd6f4';
            pre.style.padding = '1rem';
            pre.style.borderRadius = '6px';
            pre.style.overflow = 'auto';
            pre.style.fontSize = '0.875rem';
            pre.style.lineHeight = '1.5';

            const code = document.createElement('code');
            if (this.__language) code.className = `language-${this.__language}`;
            code.textContent = this.__code;
            pre.appendChild(code);
            wrapper.appendChild(pre);

            if (this.__language) {
                const badge = document.createElement('div');
                badge.textContent = this.__language;
                badge.style.cssText = 'position:absolute;top:4px;right:8px;font-size:0.75rem;color:#6c7086;text-transform:uppercase';
                wrapper.style.position = 'relative';
                wrapper.appendChild(badge);
            }
            return wrapper;
        }

        updateDOM() { return false; }

        exportJSON() {
            return {
                type: 'codeblock',
                version: 1,
                code: this.__code,
                language: this.__language,
                caption: this.__caption,
            };
        }

        static importJSON(json) {
            return new CodeBlockNode(json.code, json.language, json.caption);
        }

        decorate() { return null; }
        isInline() { return false; }
    }
    return CodeBlockNode;
}

// ─── Toolbar ───────────────────────────────────────────────────

function createToolbar(wrapperEl, editor, ImageNode, HorizontalRuleNode, CodeBlockNode) {
    toolbarEl = document.createElement('div');
    toolbarEl.className = 'le-toolbar';

    const groups = [
        // Text formatting
        [
            { icon: 'bi-type-bold', title: 'Bold (Ctrl+B)', action: () => editor.dispatchCommand(L.FORMAT_TEXT_COMMAND, 'bold'), key: 'bold' },
            { icon: 'bi-type-italic', title: 'Italic (Ctrl+I)', action: () => editor.dispatchCommand(L.FORMAT_TEXT_COMMAND, 'italic'), key: 'italic' },
            { icon: 'bi-type-underline', title: 'Underline (Ctrl+U)', action: () => editor.dispatchCommand(L.FORMAT_TEXT_COMMAND, 'underline'), key: 'underline' },
            { icon: 'bi-type-strikethrough', title: 'Strikethrough', action: () => editor.dispatchCommand(L.FORMAT_TEXT_COMMAND, 'strikethrough'), key: 'strikethrough' },
            { icon: 'bi-code', title: 'Inline code', action: () => editor.dispatchCommand(L.FORMAT_TEXT_COMMAND, 'code'), key: 'code' },
        ],
        // Block types
        [
            { icon: 'bi-type-h1', title: 'Heading 1', action: () => toggleBlock(editor, 'h1'), key: 'h1' },
            { icon: 'bi-type-h2', title: 'Heading 2', action: () => toggleBlock(editor, 'h2'), key: 'h2' },
            { icon: 'bi-type-h3', title: 'Heading 3', action: () => toggleBlock(editor, 'h3'), key: 'h3' },
            { icon: 'bi-quote', title: 'Blockquote', action: () => toggleBlock(editor, 'quote'), key: 'quote' },
        ],
        // Lists
        [
            { icon: 'bi-list-ul', title: 'Bullet list', action: () => toggleList(editor, 'bullet'), key: 'ul' },
            { icon: 'bi-list-ol', title: 'Numbered list', action: () => toggleList(editor, 'number'), key: 'ol' },
        ],
        // Insert
        [
            { icon: 'bi-link-45deg', title: 'Link (Ctrl+K)', action: () => insertLink(editor), key: 'link' },
            { icon: 'bi-image', title: 'Image', action: () => insertImage(editor, ImageNode), key: 'image' },
            { icon: 'bi-code-square', title: 'Code block', action: () => insertCodeBlock(editor, CodeBlockNode), key: 'codeblock' },
            { icon: 'bi-hr', title: 'Horizontal rule', action: () => insertHorizontalRule(editor, HorizontalRuleNode), key: 'hr' },
        ],
        // History
        [
            { icon: 'bi-arrow-counterclockwise', title: 'Undo (Ctrl+Z)', action: () => editor.dispatchCommand(L.UNDO_COMMAND, undefined), key: 'undo' },
            { icon: 'bi-arrow-clockwise', title: 'Redo (Ctrl+Y)', action: () => editor.dispatchCommand(L.REDO_COMMAND, undefined), key: 'redo' },
        ],
    ];

    const buttons = {};

    groups.forEach((group, gi) => {
        if (gi > 0) {
            const sep = document.createElement('div');
            sep.className = 'le-toolbar-sep';
            toolbarEl.appendChild(sep);
        }
        group.forEach(({ icon, title, action, key }) => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'le-toolbar-btn';
            btn.title = title;
            btn.innerHTML = `<i class="bi ${icon}"></i>`;
            btn.addEventListener('mousedown', e => {
                e.preventDefault(); // Prevent focus loss
                action();
            });
            toolbarEl.appendChild(btn);
            buttons[key] = btn;
        });
    });

    wrapperEl.insertBefore(toolbarEl, wrapperEl.firstChild);

    // Update active state on editor changes
    const unregister = editor.registerUpdateListener(({ editorState }) => {
        editorState.read(() => {
            updateToolbarState(buttons, editor);
        });
    });
    cleanupFns.push(unregister);
}

function updateToolbarState(buttons, editor) {
    const selection = L.$getSelection();
    if (!L.$isRangeSelection(selection)) return;

    // Text format
    buttons.bold?.classList.toggle('active', selection.hasFormat('bold'));
    buttons.italic?.classList.toggle('active', selection.hasFormat('italic'));
    buttons.underline?.classList.toggle('active', selection.hasFormat('underline'));
    buttons.strikethrough?.classList.toggle('active', selection.hasFormat('strikethrough'));
    buttons.code?.classList.toggle('active', selection.hasFormat('code'));

    // Block type
    const anchorNode = selection.anchor.getNode();
    const element = anchorNode.getKey() === 'root'
        ? anchorNode
        : anchorNode.getTopLevelElementOrThrow();

    const isHeading = RichText.$isHeadingNode?.(element) || (typeof element.getTag === 'function');
    const isQuote = RichText.$isQuoteNode?.(element);
    const listNode = List.$isListNode?.(element) ? element
        : (element.getParent && List.$isListNode?.(element.getParent())) ? element.getParent() : null;

    const headingTag = isHeading && element.getTag ? element.getTag() : null;

    buttons.h1?.classList.toggle('active', headingTag === 'h1');
    buttons.h2?.classList.toggle('active', headingTag === 'h2');
    buttons.h3?.classList.toggle('active', headingTag === 'h3');
    buttons.quote?.classList.toggle('active', !!isQuote);
    buttons.ul?.classList.toggle('active', listNode?.getListType?.() === 'bullet');
    buttons.ol?.classList.toggle('active', listNode?.getListType?.() === 'number');

    // Link
    const parent = anchorNode.getParent();
    buttons.link?.classList.toggle('active', Link.$isLinkNode?.(parent));
}

// ─── Block/List Operations ─────────────────────────────────────

function toggleBlock(editor, type) {
    editor.update(() => {
        const selection = L.$getSelection();
        if (!L.$isRangeSelection(selection)) return;

        if (type === 'quote') {
            const anchorNode = selection.anchor.getNode();
            const element = anchorNode.getKey() === 'root'
                ? anchorNode
                : anchorNode.getTopLevelElementOrThrow();
            if (RichText.$isQuoteNode?.(element)) {
                Selection.$setBlocksType(selection, () => L.$createParagraphNode());
            } else {
                Selection.$setBlocksType(selection, () => RichText.$createQuoteNode());
            }
        } else {
            // Headings h1–h6
            const anchorNode = selection.anchor.getNode();
            const element = anchorNode.getKey() === 'root'
                ? anchorNode
                : anchorNode.getTopLevelElementOrThrow();
            const isCurrentType = (RichText.$isHeadingNode?.(element) || typeof element.getTag === 'function')
                && element.getTag?.() === type;
            if (isCurrentType) {
                Selection.$setBlocksType(selection, () => L.$createParagraphNode());
            } else {
                Selection.$setBlocksType(selection, () => RichText.$createHeadingNode(type));
            }
        }
    });
}

function toggleList(editor, listType) {
    editor.update(() => {
        const selection = L.$getSelection();
        if (!L.$isRangeSelection(selection)) return;

        const anchorNode = selection.anchor.getNode();
        const element = Utils.$getNearestBlockElementAncestorOrThrow
            ? Utils.$getNearestBlockElementAncestorOrThrow(anchorNode)
            : anchorNode.getParent();
        const listNode = List.$isListNode?.(element) ? element
            : List.$isListNode?.(element?.getParent?.()) ? element.getParent() : null;

        if (listNode && listNode.getListType?.() === listType) {
            // Remove list — convert back to paragraphs
            List.removeList(editor);
        } else {
            if (listType === 'number') {
                List.insertList(editor, 'number');
            } else {
                List.insertList(editor, 'bullet');
            }
        }
    });
}

function insertLink(editor) {
    editor.update(() => {
        const selection = L.$getSelection();
        if (!L.$isRangeSelection(selection)) return;

        const node = selection.anchor.getNode();
        const parent = node.getParent();
        if (Link.$isLinkNode?.(parent)) {
            // Remove link
            Link.toggleLink(null);
            return;
        }

        const url = prompt('Enter URL:');
        if (url) {
            Link.toggleLink(url);
        }
    });
}

const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml'];
const MAX_IMAGE_SIZE = 10 * 1024 * 1024; // 10 MB

async function uploadAndInsertImage(editor, ImageNode, file) {
    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
        alert('Unsupported image type. Use JPEG, PNG, GIF, WebP, or SVG.');
        return;
    }
    if (file.size > MAX_IMAGE_SIZE) {
        alert('Image must be under 10 MB.');
        return;
    }

    const formData = new FormData();
    formData.append('file', file);

    try {
        const res = await fetch('/api/upload/image', { method: 'POST', body: formData });
        if (!res.ok) {
            const err = await res.text();
            alert('Upload failed: ' + err);
            return;
        }
        const result = await res.json();
        editor.update(() => {
            const imageNode = new ImageNode(result.url, file.name, result.width, result.height, '', '');
            const selection = L.$getSelection();
            if (L.$isRangeSelection(selection)) {
                selection.insertNodes([imageNode]);
            } else {
                L.$getRoot().append(imageNode);
            }
            // Insert paragraph after image for continued typing
            const paragraph = L.$createParagraphNode();
            imageNode.insertAfter(paragraph);
            paragraph.selectStart();
        });
    } catch (e) {
        alert('Upload failed: ' + e.message);
    }
}

function insertImage(editor, ImageNode) {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = ALLOWED_IMAGE_TYPES.join(',');
    input.onchange = async () => {
        const file = input.files?.[0];
        if (!file) return;
        await uploadAndInsertImage(editor, ImageNode, file);
    };
    input.click();
}

// ─── Drag-and-Drop Image Upload ────────────────────────────────

function registerDragAndDrop(editorDiv, editor, ImageNode) {
    let dragCounter = 0;

    const onDragEnter = (e) => {
        e.preventDefault();
        if (!hasImageFiles(e.dataTransfer)) return;
        dragCounter++;
        editorDiv.classList.add('le-drag-over');
    };

    const onDragOver = (e) => {
        e.preventDefault();
        if (hasImageFiles(e.dataTransfer)) {
            e.dataTransfer.dropEffect = 'copy';
        }
    };

    const onDragLeave = (e) => {
        e.preventDefault();
        dragCounter--;
        if (dragCounter <= 0) {
            dragCounter = 0;
            editorDiv.classList.remove('le-drag-over');
        }
    };

    const onDrop = async (e) => {
        e.preventDefault();
        dragCounter = 0;
        editorDiv.classList.remove('le-drag-over');

        const files = Array.from(e.dataTransfer?.files || [])
            .filter(f => ALLOWED_IMAGE_TYPES.includes(f.type));
        for (const file of files) {
            await uploadAndInsertImage(editor, ImageNode, file);
        }
    };

    editorDiv.addEventListener('dragenter', onDragEnter);
    editorDiv.addEventListener('dragover', onDragOver);
    editorDiv.addEventListener('dragleave', onDragLeave);
    editorDiv.addEventListener('drop', onDrop);

    cleanupFns.push(() => {
        editorDiv.removeEventListener('dragenter', onDragEnter);
        editorDiv.removeEventListener('dragover', onDragOver);
        editorDiv.removeEventListener('dragleave', onDragLeave);
        editorDiv.removeEventListener('drop', onDrop);
    });
}

function hasImageFiles(dataTransfer) {
    if (!dataTransfer?.types) return false;
    if (!dataTransfer.types.includes('Files')) return false;
    // During dragenter/dragover, items may be available for type checking
    if (dataTransfer.items) {
        return Array.from(dataTransfer.items).some(item =>
            item.kind === 'file' && ALLOWED_IMAGE_TYPES.includes(item.type)
        );
    }
    return true;
}

function insertCodeBlock(editor, CodeBlockNode) {
    // Create a small modal for code input
    const overlay = document.createElement('div');
    overlay.className = 'le-modal-overlay';
    overlay.innerHTML = `
        <div class="le-modal">
            <div class="le-modal-header">
                <h5>Insert Code Block</h5>
                <button type="button" class="le-modal-close">&times;</button>
            </div>
            <div class="le-modal-body">
                <div style="margin-bottom:0.75rem">
                    <label style="display:block;margin-bottom:0.25rem;font-size:0.875rem">Language</label>
                    <input type="text" class="le-input" id="le-code-lang" placeholder="e.g. javascript, python, csharp" />
                </div>
                <div>
                    <label style="display:block;margin-bottom:0.25rem;font-size:0.875rem">Code</label>
                    <textarea class="le-textarea" id="le-code-text" rows="10" placeholder="Paste your code here…"></textarea>
                </div>
            </div>
            <div class="le-modal-footer">
                <button type="button" class="le-btn le-btn-secondary le-modal-cancel">Cancel</button>
                <button type="button" class="le-btn le-btn-primary le-modal-insert">Insert</button>
            </div>
        </div>
    `;
    document.body.appendChild(overlay);

    const close = () => overlay.remove();
    overlay.querySelector('.le-modal-close').onclick = close;
    overlay.querySelector('.le-modal-cancel').onclick = close;
    overlay.addEventListener('mousedown', e => { if (e.target === overlay) close(); });

    overlay.querySelector('.le-modal-insert').onclick = () => {
        const lang = overlay.querySelector('#le-code-lang').value.trim();
        const code = overlay.querySelector('#le-code-text').value;
        if (!code) { close(); return; }

        editor.update(() => {
            const codeNode = new CodeBlockNode(code, lang, '');
            const selection = L.$getSelection();
            if (L.$isRangeSelection(selection)) {
                selection.insertNodes([codeNode]);
            } else {
                L.$getRoot().append(codeNode);
            }
            const paragraph = L.$createParagraphNode();
            codeNode.insertAfter(paragraph);
            paragraph.selectStart();
        });
        close();
    };

    setTimeout(() => overlay.querySelector('#le-code-lang')?.focus(), 100);
}

function insertHorizontalRule(editor, HorizontalRuleNode) {
    editor.update(() => {
        const hrNode = new HorizontalRuleNode();
        const selection = L.$getSelection();
        if (L.$isRangeSelection(selection)) {
            selection.insertNodes([hrNode]);
        } else {
            L.$getRoot().append(hrNode);
        }
        const paragraph = L.$createParagraphNode();
        hrNode.insertAfter(paragraph);
        paragraph.selectStart();
    });
}

// ─── Keyboard Shortcuts ────────────────────────────────────────

function registerKeyboardShortcuts(editor) {
    const rootEl = editor.getRootElement();
    if (!rootEl) return;

    const handler = (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            insertLink(editor);
        }
    };
    rootEl.addEventListener('keydown', handler);
    cleanupFns.push(() => rootEl.removeEventListener('keydown', handler));
}

// ─── Public API (exported for Blazor interop) ──────────────────

export async function init(wrapperElement, blazorRef, initialContent) {
    await loadLexical();
    dotNetRef = blazorRef;

    const ImageNode = createImageNodeClass();
    const HorizontalRuleNode = createHorizontalRuleNodeClass();
    const CodeBlockNode = createCodeBlockNodeClass();

    const theme = {
        paragraph: 'le-paragraph',
        heading: {
            h1: 'le-h1', h2: 'le-h2', h3: 'le-h3',
            h4: 'le-h4', h5: 'le-h5', h6: 'le-h6',
        },
        text: {
            bold: 'le-bold',
            italic: 'le-italic',
            underline: 'le-underline',
            strikethrough: 'le-strikethrough',
            code: 'le-inline-code',
        },
        list: {
            ol: 'le-ol',
            ul: 'le-ul',
            listitem: 'le-li',
            nested: { listitem: 'le-nested-li' },
        },
        quote: 'le-quote',
        link: 'le-link',
    };

    const editor = L.createEditor({
        namespace: 'HowToSoftwareEditor',
        nodes: [
            RichText.HeadingNode,
            RichText.QuoteNode,
            List.ListNode,
            List.ListItemNode,
            Link.LinkNode,
            Link.AutoLinkNode,
            ImageNode,
            HorizontalRuleNode,
            CodeBlockNode,
        ],
        theme,
        onError: (error) => console.error('[LexicalEditor]', error),
    });

    // Create contenteditable element
    const editorDiv = document.createElement('div');
    editorDiv.contentEditable = 'true';
    editorDiv.className = 'le-content';
    editorDiv.setAttribute('spellcheck', 'true');
    editorDiv.setAttribute('data-gramm', 'true');
    editorDiv.setAttribute('data-gramm_editor', 'true');
    editorDiv.setAttribute('data-enable-grammarly', 'true');
    wrapperElement.appendChild(editorDiv);

    editor.setRootElement(editorDiv);
    editorInstance = editor;

    // Enable rich text input handling (typing, selections, copy/paste, etc.)
    cleanupFns.push(RichText.registerRichText(editor));

    // Register list commands
    cleanupFns.push(editor.registerCommand(
        List.INSERT_ORDERED_LIST_COMMAND,
        () => { List.insertList(editor, 'number'); return true; },
        L.COMMAND_PRIORITY_LOW
    ));
    cleanupFns.push(editor.registerCommand(
        List.INSERT_UNORDERED_LIST_COMMAND,
        () => { List.insertList(editor, 'bullet'); return true; },
        L.COMMAND_PRIORITY_LOW
    ));
    cleanupFns.push(editor.registerCommand(
        List.REMOVE_LIST_COMMAND,
        () => { List.removeList(editor); return true; },
        L.COMMAND_PRIORITY_LOW
    ));

    // Register link command
    cleanupFns.push(editor.registerCommand(
        Link.TOGGLE_LINK_COMMAND,
        (payload) => { Link.toggleLink(payload); return true; },
        L.COMMAND_PRIORITY_LOW
    ));

    // Register history (undo/redo)
    cleanupFns.push(History.registerHistory(editor, History.createEmptyHistoryState(), 300));

    // Create toolbar
    createToolbar(wrapperElement, editor, ImageNode, HorizontalRuleNode, CodeBlockNode);

    // Register keyboard shortcuts
    registerKeyboardShortcuts(editor);

    // Register drag-and-drop image upload
    registerDragAndDrop(editorDiv, editor, ImageNode);

    // Notify Blazor on content changes (debounced for dirty tracking)
    let dirtyTimer = null;
    cleanupFns.push(editor.registerUpdateListener(({ dirtyElements, dirtyLeaves }) => {
        if (dirtyElements.size === 0 && dirtyLeaves.size === 0) return;
        clearTimeout(dirtyTimer);
        dirtyTimer = setTimeout(() => {
            dotNetRef?.invokeMethodAsync('OnEditorContentChanged');
        }, 500);
    }));

    // Load initial content if provided
    if (initialContent) {
        try {
            const normalized = initialContent.replace(/"type"\s*:\s*"extended-(text|heading|quote|codeblock)"/g, '"type":"$1"');
            const parsed = JSON.parse(normalized);
            const state = editor.parseEditorState(parsed);
            editor.setEditorState(state);
        } catch (e) {
            console.warn('[LexicalEditor] Failed to load initial content:', e);
        }
    }
}

export function getContent() {
    if (!editorInstance) return null;
    const state = editorInstance.getEditorState();
    return JSON.stringify(state.toJSON());
}

export function setContent(json) {
    if (!editorInstance || !json) return;
    try {
        // Normalize Ghost's extended node types to base Lexical types
        const normalized = typeof json === 'string'
            ? json.replace(/"type"\s*:\s*"extended-(text|heading|quote|codeblock)"/g, '"type":"$1"')
            : json;
        const parsed = typeof normalized === 'string' ? JSON.parse(normalized) : normalized;
        const state = editorInstance.parseEditorState(parsed);
        editorInstance.setEditorState(state);
    } catch (e) {
        console.warn('[LexicalEditor] Failed to set content:', e);
    }
}

export function importHtml(html) {
    if (!editorInstance || !html) return;
    try {
        const parser = new DOMParser();
        const dom = parser.parseFromString(html, 'text/html');
        editorInstance.update(() => {
            const nodes = HtmlModule.$generateNodesFromDOM(editorInstance, dom);
            const root = L.$getRoot();
            root.clear();
            for (const node of nodes) {
                if (L.$isElementNode(node) || L.$isDecoratorNode(node)) {
                    root.append(node);
                } else if (L.$isTextNode(node)) {
                    // Wrap bare text in a paragraph
                    const p = L.$createParagraphNode();
                    p.append(node);
                    root.append(p);
                }
            }
        });
    } catch (e) {
        console.warn('[LexicalEditor] Failed to import HTML:', e);
    }
}

export function getWordCount() {
    if (!editorInstance) return 0;
    let text = '';
    editorInstance.getEditorState().read(() => {
        text = L.$getRoot().getTextContent();
    });
    return text.trim() ? text.trim().split(/\s+/).length : 0;
}

export function getCharCount() {
    if (!editorInstance) return 0;
    let text = '';
    editorInstance.getEditorState().read(() => {
        text = L.$getRoot().getTextContent();
    });
    return text.length;
}

export function focus() {
    editorInstance?.focus();
}

// ─── Global Keyboard Shortcuts (Blazor callbacks) ─────────────

let globalShortcutHandler = null;

export function registerEditorShortcuts(blazorRef) {
    unregisterEditorShortcuts();
    globalShortcutHandler = (e) => {
        const ctrl = e.ctrlKey || e.metaKey;

        // Ctrl+S — save/update
        if (ctrl && !e.shiftKey && e.key === 's') {
            e.preventDefault();
            blazorRef.invokeMethodAsync('OnSaveShortcut');
            return;
        }

        // Ctrl+Shift+P — publish
        if (ctrl && e.shiftKey && e.key === 'P') {
            e.preventDefault();
            blazorRef.invokeMethodAsync('OnPublishShortcut');
            return;
        }

        // Escape — close settings sidebar
        if (e.key === 'Escape') {
            blazorRef.invokeMethodAsync('OnEscapeShortcut');
            return;
        }
    };
    document.addEventListener('keydown', globalShortcutHandler);
}

export function unregisterEditorShortcuts() {
    if (globalShortcutHandler) {
        document.removeEventListener('keydown', globalShortcutHandler);
        globalShortcutHandler = null;
    }
}

export function destroy() {
    unregisterEditorShortcuts();
    cleanupFns.forEach(fn => fn());
    cleanupFns = [];
    if (toolbarEl) {
        toolbarEl.remove();
        toolbarEl = null;
    }
    editorInstance = null;
    dotNetRef = null;
}
