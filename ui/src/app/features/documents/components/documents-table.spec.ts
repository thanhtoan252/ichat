import { describe, expect, it } from 'vitest';
import { formatFileSize } from './documents-table';

describe('formatFileSize', () => {
  it('keeps small files in bytes', () => {
    expect(formatFileSize(0)).toBe('0 B');
    expect(formatFileSize(1023)).toBe('1023 B');
  });

  it('switches to whole kilobytes', () => {
    expect(formatFileSize(1024)).toBe('1 KB');
    expect(formatFileSize(20 * 1024)).toBe('20 KB');
  });

  it('switches to megabytes with one decimal', () => {
    expect(formatFileSize(1024 * 1024)).toBe('1.0 MB');
    expect(formatFileSize(Math.round(2.5 * 1024 * 1024))).toBe('2.5 MB');
  });

  it('renders the API upload ceiling readably', () => {
    expect(formatFileSize(20 * 1024 * 1024)).toBe('20.0 MB');
  });
});
