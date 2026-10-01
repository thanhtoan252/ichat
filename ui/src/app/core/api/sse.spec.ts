import { describe, expect, it } from 'vitest';
import { SseTransportError, readSse } from './sse';

function streamOf(...chunks: string[]): Response {
  const body = new ReadableStream<Uint8Array>({
    start(controller) {
      const encoder = new TextEncoder();

      for (const chunk of chunks) {
        controller.enqueue(encoder.encode(chunk));
      }

      controller.close();
    },
  });

  return new Response(body, { status: 200, headers: { 'Content-Type': 'text/event-stream' } });
}

async function collect(response: Response) {
  const original = globalThis.fetch;
  globalThis.fetch = async () => response;

  try {
    const frames = [];

    for await (const frame of readSse('/stream')) {
      frames.push(frame);
    }

    return frames;
  } finally {
    globalThis.fetch = original;
  }
}

describe('readSse', () => {
  it('parses event and data pairs', async () => {
    const frames = await collect(
      streamOf(
        'event: status\ndata: {"stage":"rewriting"}\n\nevent: delta\ndata: {"text":"hi"}\n\n',
      ),
    );

    expect(frames).toEqual([
      { event: 'status', data: '{"stage":"rewriting"}' },
      { event: 'delta', data: '{"text":"hi"}' },
    ]);
  });

  it('reassembles a frame split across network chunks', async () => {
    const frames = await collect(streamOf('event: del', 'ta\ndata: {"text":"', 'partial"}\n\n'));

    expect(frames).toEqual([{ event: 'delta', data: '{"text":"partial"}' }]);
  });

  it('handles CRLF line endings', async () => {
    const frames = await collect(streamOf('event: done\r\ndata: {"ok":true}\r\n\r\n'));

    expect(frames).toEqual([{ event: 'done', data: '{"ok":true}' }]);
  });

  it('emits a trailing frame that has no closing blank line', async () => {
    const frames = await collect(streamOf('event: done\ndata: {"ok":true}\n'));

    expect(frames).toEqual([{ event: 'done', data: '{"ok":true}' }]);
  });

  it('joins multi-line data and skips comments', async () => {
    const frames = await collect(streamOf(': keep-alive\nevent: delta\ndata: one\ndata: two\n\n'));

    expect(frames).toEqual([{ event: 'delta', data: 'one\ntwo' }]);
  });

  it('defaults the event name when only data is sent', async () => {
    const frames = await collect(streamOf('data: bare\n\n'));

    expect(frames).toEqual([{ event: 'message', data: 'bare' }]);
  });

  it('reports a transport failure with the problem detail', async () => {
    const response = new Response(JSON.stringify({ detail: 'Rate limit exceeded.' }), {
      status: 429,
    });

    await expect(collect(response)).rejects.toThrowError(
      expect.objectContaining({ name: 'SseTransportError', status: 429 }),
    );
  });

  it('carries the status code on the error', async () => {
    const error = new SseTransportError('nope', 503);

    expect(error.status).toBe(503);
  });
});
