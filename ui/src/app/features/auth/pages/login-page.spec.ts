import { describe, expect, it } from 'vitest';
import { safeReturnUrl } from './login-page';

describe('safeReturnUrl', () => {
  it.each(['/chat', '/documents', '/chat/01a0-abc', '/retrieval?q=x'])(
    'keeps the in-app path %s',
    (url) => {
      expect(safeReturnUrl(url)).toBe(url);
    },
  );

  it('falls back to /chat when there is no destination', () => {
    expect(safeReturnUrl(undefined)).toBe('/chat');
    expect(safeReturnUrl('')).toBe('/chat');
  });

  it.each(['https://evil.example/steal', '//evil.example/steal', 'javascript:alert(1)', 'chat'])(
    'refuses %s',
    (url) => {
      // returnUrl comes from the query string, so a crafted link must not be able to
      // turn the login screen into a redirector onto someone else's site.
      expect(safeReturnUrl(url)).toBe('/chat');
    },
  );
});
