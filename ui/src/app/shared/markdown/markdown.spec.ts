import { describe, expect, it } from 'vitest';
import { renderAnswer } from './markdown';

function html(markdown: string, sourceCount?: number): string {
  const host = document.createElement('div');

  host.append(renderAnswer(markdown, sourceCount === undefined ? {} : { sourceCount }));

  return host.innerHTML;
}

describe('renderAnswer', () => {
  it('renders markdown structure', () => {
    expect(html('# Title\n\n- one\n- two')).toContain('<h1>Title</h1>');
    expect(html('# Title\n\n- one\n- two')).toContain('<li>one</li>');
  });

  it('turns valid markers into citation buttons', () => {
    const output = html('The limit is 30 requests [1].', 3);

    expect(output).toContain('data-citation-marker="1"');
  });

  it('leaves markers above the source count as plain text', () => {
    const output = html('See [9].', 3);

    expect(output).not.toContain('data-citation-marker');
    expect(output).toContain('See [9].');
  });

  it('does not touch markers inside code, matching the server extractor', () => {
    const output = html('Use `list[1]` here [1].', 2);

    expect(output).toContain('<code>list[1]</code>');
    expect(output.match(/data-citation-marker/g)).toHaveLength(1);
  });

  it('does not touch markers inside fenced blocks', () => {
    const output = html('```\nvalues[1]\n```', 2);

    expect(output).not.toContain('data-citation-marker');
  });

  it('strips script tags and event handler attributes', () => {
    const output = html('<img src="x" onerror="alert(1)">\n\n<script>alert(2)</script>');

    expect(output).not.toContain('onerror');
    expect(output).not.toContain('<script');
  });

  it('drops javascript: URLs but keeps ordinary links', () => {
    expect(html('[click](javascript:alert(1))')).not.toContain('href');
    expect(html('[docs](https://example.com)')).toContain('href="https://example.com"');
  });

  it('marks external links safe to open', () => {
    const output = html('[docs](https://example.com)');

    expect(output).toContain('rel="noopener noreferrer"');
  });

  it('appends a caret only while streaming', () => {
    const host = document.createElement('div');
    host.append(renderAnswer('partial', { streaming: true }));

    expect(host.innerHTML).toContain('streaming-caret');
    expect(html('partial')).not.toContain('streaming-caret');
  });
});
