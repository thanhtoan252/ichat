// @ts-check
//
// KNOWN LIMITATION: boundaries/dependencies below is fully written and does correctly
// classify every file into its architectural element (verified via
// ESLINT_PLUGIN_BOUNDARIES_DEBUG=1) — but eslint-plugin-boundaries v7 resolves the
// *target* of a local import through eslint-module-utils, a legacy (pre-flat-config)
// resolution path. In this project's setup (ESLint 10 flat config + typescript-eslint),
// that path fails to resolve relative imports and @app/* aliases to a real file, so
// cross-feature-internals imports are not actually caught yet — tried no resolver,
// eslint-import-resolver-typescript v4, v3, and the bundled eslint-import-resolver-node
// with .ts/.js extensions; none closed the gap. The rule stays enabled (it's correct
// and will start working the moment this resolution gap is fixed upstream or worked
// around) rather than removed, but treat "boundaries/dependencies violations fail CI"
// as aspirational until this is resolved — see ARCHITECTURE.md.
const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const boundaries = require('eslint-plugin-boundaries');

/**
 * Feature-internal element types: everything inside features/<name>/{pages,
 * components,data,state}. Grouped in one array because the boundary that matters is
 * "same feature or not", not which internal layer talks to which — nothing in the
 * brief restricts e.g. components/ from importing data/ within one feature.
 */
const FEATURE_INTERNAL_TYPES = [
  'feature-pages',
  'feature-components',
  'feature-data',
  'feature-state',
];

const SHARED_TYPES = ['shared-ui', 'shared-util', 'core'];

module.exports = tseslint.config(
  {
    ignores: [
      'dist/**',
      '.angular/**',
      'coverage/**',
      'node_modules/**',
      // The vendored spartan/ui component library (see components.json). Not code we
      // maintain or want held to this app's conventions (selector prefix, etc).
      'src/app/shared/ui/*/src/**',
    ],
  },
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    plugins: { boundaries },
    settings: {
      'boundaries/root-path': 'src/app',
      'boundaries/elements': [
        // chat is the one feature with an index.ts — layout/app-shell.ts needs its
        // conversation-history state and can't reach it any other way, since the
        // sidebar it feeds is visible on every route, not just /chat.
        { type: 'feature-index', pattern: 'features/(*)/index.ts', capture: ['feature'] },
        { type: 'feature-pages', pattern: 'features/(*)/pages/**', capture: ['feature'] },
        {
          type: 'feature-components',
          pattern: 'features/(*)/components/**',
          capture: ['feature'],
        },
        { type: 'feature-data', pattern: 'features/(*)/data/**', capture: ['feature'] },
        { type: 'feature-state', pattern: 'features/(*)/state/**', capture: ['feature'] },
        { type: 'shared-ui', pattern: 'shared/ui/**' },
        { type: 'shared-util', pattern: 'shared/util/**' },
        { type: 'core', pattern: 'core/**' },
        { type: 'layout', pattern: 'layout/**' },
        { type: 'app-root', pattern: '*.ts' },
      ],
    },
    rules: {
      '@typescript-eslint/no-explicit-any': 'error',
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'app', style: 'kebab-case' },
      ],
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'app', style: 'camelCase' },
      ],
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          policies: [
            // A feature's internal layers may freely import each other, but only
            // within the same feature — this is the rule that keeps one feature
            // from reaching into another's internals.
            {
              from: { element: { type: FEATURE_INTERNAL_TYPES } },
              allow: [
                {
                  to: {
                    element: {
                      type: FEATURE_INTERNAL_TYPES,
                      capture: { feature: '{{from.feature}}' },
                    },
                  },
                },
                { to: { element: { type: SHARED_TYPES } } },
              ],
            },
            // chat's index.ts barrel assembles its public API from its own
            // internals, plus whatever shared/core it needs to re-export.
            {
              from: { element: { type: 'feature-index' } },
              allow: [
                {
                  to: {
                    element: {
                      type: FEATURE_INTERNAL_TYPES,
                      capture: { feature: '{{from.feature}}' },
                    },
                  },
                },
                { to: { element: { type: SHARED_TYPES } } },
              ],
            },
            // The app shell and the app root (routes/config) are not features, but
            // the same boundary applies to them: a feature is reached only through
            // its index.ts (chat) or its routes file (loadChildren, checked by the
            // TS compiler already), never a feature's internals directly.
            {
              from: { element: { type: ['layout', 'app-root'] } },
              allow: [{ to: { element: { type: [...SHARED_TYPES, 'layout', 'feature-index'] } } }],
            },
            // shared/ui may depend on shared/util (e.g. answer-content uses the
            // pure markdown renderer).
            {
              from: { element: { type: 'shared-ui' } },
              allow: { to: { element: { type: 'shared-util' } } },
            },
            // core is the foundation: no policy grants it access to shared/*,
            // features/*, or layout — the default disallow covers it.
          ],
        },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [...angular.configs.templateRecommended],
    rules: {},
  },
);
