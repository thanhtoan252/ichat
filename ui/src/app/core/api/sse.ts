/**
 * Minimal Server-Sent Events reader over `fetch`.
 *
 * `EventSource` cannot be used here: the chat endpoint is a POST (the question travels
 * in the body) and EventSource only ever issues GET. Parsing the wire format by hand is
 * the price of that, so this file implements exactly the subset the API emits —
 * `event:` and `data:` lines, frames separated by a blank line.
 */

export interface SseFrame {
  readonly event: string;
  readonly data: string;
}

export interface SseRequestInit {
  readonly method?: string;
  readonly body?: BodyInit | null;
  readonly headers?: Record<string, string>;
  readonly signal?: AbortSignal;
}

/** Thrown when the transport itself fails, as opposed to an `error` frame in the stream. */
export class SseTransportError extends Error {
  constructor(
    override readonly message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = 'SseTransportError';
  }
}

export async function* readSse(
  url: string,
  init: SseRequestInit = {},
): AsyncGenerator<SseFrame, void, undefined> {
  const response = await fetch(url, {
    method: init.method ?? 'POST',
    body: init.body ?? null,
    signal: init.signal ?? null,
    headers: { Accept: 'text/event-stream', ...init.headers },
  });

  if (!response.ok || !response.body) {
    throw new SseTransportError(await describeFailure(response), response.status);
  }

  const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();

      if (done) {
        break;
      }

      // Normalise CRLF up front so the frame split only has to handle one form.
      buffer += value.replace(/\r\n/g, '\n');

      let boundary = buffer.indexOf('\n\n');

      while (boundary !== -1) {
        const frame = parseFrame(buffer.slice(0, boundary));
        buffer = buffer.slice(boundary + 2);

        if (frame) {
          yield frame;
        }

        boundary = buffer.indexOf('\n\n');
      }
    }

    // A stream that ends without its trailing blank line still carries a full frame.
    const tail = parseFrame(buffer);

    if (tail) {
      yield tail;
    }
  } finally {
    // Releasing the lock lets the abort propagate instead of leaving the body pinned.
    reader.cancel().catch(() => undefined);
  }
}

function parseFrame(raw: string): SseFrame | null {
  const trimmed = raw.trim();

  if (trimmed.length === 0) {
    return null;
  }

  let event = 'message';
  const dataLines: string[] = [];

  for (const line of trimmed.split('\n')) {
    if (line.startsWith(':')) {
      continue; // comment / keep-alive
    }

    const separator = line.indexOf(':');
    const field = separator === -1 ? line : line.slice(0, separator);
    // "data: x" carries one optional space after the colon, per the spec.
    const rest = separator === -1 ? '' : line.slice(separator + 1).replace(/^ /, '');

    if (field === 'event') {
      event = rest;
    } else if (field === 'data') {
      dataLines.push(rest);
    }
  }

  if (dataLines.length === 0) {
    return null;
  }

  return { event, data: dataLines.join('\n') };
}

async function describeFailure(response: Response): Promise<string> {
  try {
    const text = await response.text();

    if (text) {
      try {
        const problem = JSON.parse(text) as { title?: string; detail?: string };
        const message = problem.detail ?? problem.title;

        if (message) {
          return message;
        }
      } catch {
        return text.slice(0, 500);
      }
    }
  } catch {
    // Falls through to the generic message below.
  }

  return `Request failed with status ${response.status}.`;
}
