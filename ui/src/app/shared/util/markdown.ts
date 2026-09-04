import { marked } from 'marked';

/**
 * Renders an assistant answer to a DocumentFragment.
 *
 * Two things happen beyond plain markdown:
 *
 * 1. The output is sanitised against an allowlist. `marked` passes raw HTML straight
 *    through, and the text being rendered came from a language model, so nothing here
 *    may be trusted.
 * 2. `[n]` citation markers become buttons. The masking rule matches
 *    `CitationExtractor` on the server — markers inside code are left alone — so the
 *    chips the reader sees are exactly the ones the API verified.
 */

const CITATION_MARKER = /\[(\d{1,3})\]/g;

/** Elements markdown can legitimately produce. Everything else is unwrapped. */
const ALLOWED_TAGS = new Set([
  'A',
  'BLOCKQUOTE',
  'BR',
  'CODE',
  'DD',
  'DEL',
  'DIV',
  'DL',
  'DT',
  'EM',
  'H1',
  'H2',
  'H3',
  'H4',
  'H5',
  'H6',
  'HR',
  'IMG',
  'LI',
  'OL',
  'P',
  'PRE',
  'S',
  'SPAN',
  'STRONG',
  'SUB',
  'SUP',
  'TABLE',
  'TBODY',
  'TD',
  'TH',
  'THEAD',
  'TR',
  'UL',
]);

const ALLOWED_ATTRIBUTES: Readonly<Record<string, readonly string[]>> = {
  A: ['href', 'title'],
  IMG: ['src', 'alt', 'title'],
  TD: ['colspan', 'rowspan'],
  TH: ['colspan', 'rowspan', 'scope'],
  OL: ['start'],
};

const SAFE_URL = /^(https?:|mailto:|tel:|#|\/|\.\/|\.\.\/)/i;

/** Text inside these never gets marker substitution, mirroring the server's code masking. */
const MARKER_FREE_ANCESTORS = new Set(['CODE', 'PRE', 'A']);

export interface RenderAnswerOptions {
  /** How many sources the turn actually had; markers outside 1..n stay plain text. */
  readonly sourceCount?: number;
  /** Appends a blinking caret, for an answer that is still streaming. */
  readonly streaming?: boolean;
}

export function renderAnswer(
  markdown: string,
  options: RenderAnswerOptions = {},
): DocumentFragment {
  const html = marked.parse(markdown, { async: false, gfm: true, breaks: true });

  const template = document.createElement('template');
  template.innerHTML = html;

  sanitize(template.content);
  linkifyMarkers(template.content, options.sourceCount ?? Number.MAX_SAFE_INTEGER);

  if (options.streaming) {
    const caret = document.createElement('span');
    caret.className = 'streaming-caret';
    caret.setAttribute('aria-hidden', 'true');
    template.content.append(caret);
  }

  return template.content;
}

function sanitize(root: ParentNode): void {
  // Snapshot first: unwrapping a node while iterating a live list skips siblings.
  for (const element of Array.from(root.querySelectorAll('*'))) {
    if (!ALLOWED_TAGS.has(element.tagName)) {
      element.replaceWith(...Array.from(element.childNodes));
      continue;
    }

    const allowed = ALLOWED_ATTRIBUTES[element.tagName] ?? [];

    for (const attribute of Array.from(element.attributes)) {
      if (!allowed.includes(attribute.name)) {
        element.removeAttribute(attribute.name);
        continue;
      }

      if (
        (attribute.name === 'href' || attribute.name === 'src') &&
        !SAFE_URL.test(attribute.value.trim())
      ) {
        element.removeAttribute(attribute.name);
      }
    }

    if (element.tagName === 'A') {
      element.setAttribute('target', '_blank');
      element.setAttribute('rel', 'noopener noreferrer');
    }

    if (element.tagName === 'IMG') {
      element.setAttribute('loading', 'lazy');
    }
  }
}

function linkifyMarkers(root: ParentNode, sourceCount: number): void {
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
  const targets: Text[] = [];

  while (walker.nextNode()) {
    const node = walker.currentNode as Text;

    if (node.data.includes('[') && !hasMarkerFreeAncestor(node, root)) {
      targets.push(node);
    }
  }

  for (const node of targets) {
    replaceMarkersIn(node, sourceCount);
  }
}

function hasMarkerFreeAncestor(node: Node, root: ParentNode): boolean {
  for (
    let current = node.parentElement;
    current && current !== root;
    current = current.parentElement
  ) {
    if (MARKER_FREE_ANCESTORS.has(current.tagName)) {
      return true;
    }
  }

  return false;
}

function replaceMarkersIn(node: Text, sourceCount: number): void {
  CITATION_MARKER.lastIndex = 0;

  const fragment = document.createDocumentFragment();
  let cursor = 0;
  let match: RegExpExecArray | null;

  while ((match = CITATION_MARKER.exec(node.data)) !== null) {
    const index = Number(match[1]);

    if (index < 1 || index > sourceCount) {
      continue; // The server would reject it too; leave the literal text alone.
    }

    if (match.index > cursor) {
      fragment.append(node.data.slice(cursor, match.index));
    }

    fragment.append(createMarkerButton(index));
    cursor = match.index + match[0].length;
  }

  if (cursor === 0) {
    return; // No valid marker in this node.
  }

  fragment.append(node.data.slice(cursor));
  node.replaceWith(fragment);
}

function createMarkerButton(index: number): HTMLButtonElement {
  const button = document.createElement('button');

  button.type = 'button';
  button.dataset['citationMarker'] = String(index);
  button.textContent = String(index);
  button.title = `Source ${index}`;
  button.setAttribute('aria-label', `Show source ${index}`);
  button.className =
    'mx-0.5 inline-flex h-[1.125rem] min-w-[1.125rem] translate-y-[-1px] items-center justify-center ' +
    'rounded-full border border-primary/25 bg-primary/10 px-1 align-middle text-[0.6875rem] font-medium ' +
    'leading-none text-primary transition-colors hover:bg-primary/20 focus-visible:outline-none ' +
    'focus-visible:ring-2 focus-visible:ring-ring/50';

  return button;
}
