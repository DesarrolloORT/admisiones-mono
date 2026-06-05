import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { FlatCompat } from '@eslint/eslintrc';
import js from '@eslint/js';
import eslintConfigPrettier from 'eslint-config-prettier';
import simpleImportSort from 'eslint-plugin-simple-import-sort';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const compat = new FlatCompat({
  baseDirectory: __dirname,
  recommendedConfig: js.configs.recommended,
  allConfig: js.configs.all,
});

export default [
  {
    ignores: [
      '**/tests/',
      '**/api/',
      '**/coverage/',
      '**/dist/',
      '**/node_modules/',
      '**/.angular/',
      '**/index.html',
      '.*/',
    ],
  },
  {
    plugins: {
      'simple-import-sort': simpleImportSort,
    },

    rules: {
      'simple-import-sort/imports': 'error',
      'simple-import-sort/exports': 'error',
    },
  },
  ...compat
    .extends(
      'eslint:recommended',
      'plugin:@typescript-eslint/recommended',
      'plugin:@angular-eslint/recommended',
      'plugin:@angular-eslint/template/process-inline-templates'
    )
    .map(config => ({
      ...config,
      files: ['**/*.ts'],
    })),
  {
    files: ['src/app/**/*.ts'],

    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'app',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],
      '@angular-eslint/sort-lifecycle-methods': ['error'],
      '@angular-eslint/no-lifecycle-call': ['error'],
      '@angular-eslint/prefer-output-readonly': ['error'],
      '@angular-eslint/relative-url-prefix': ['error'],
      '@angular-eslint/use-component-selector': ['error'],
      '@angular-eslint/use-lifecycle-interface': ['error'],
      '@angular-eslint/use-injectable-provided-in': ['error'],
    },
  },
  {
    files: ['src/app/features/**/*.ts'],
    ignores: ['src/app/features/**/endpoints/**/*.ts'],

    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: 'src/app/shared/api/core/api-http-client',
              message:
                'Las features deben usar su adapter en endpoints/. ApiHttpClient vive detras de esa capa.',
            },
            {
              name: '@angular/common/http',
              message: 'Las features deben usar ApiHttpClient; HttpClient vive en shared/api/core.',
            },
            {
              name: 'src/environments/environment',
              message: 'Las features no deben resolver API_URL; usar ApiHttpClient.',
            },
          ],
          patterns: [
            {
              group: ['**/environments/environment'],
              message: 'Las features no deben resolver API_URL; usar ApiHttpClient.',
            },
            {
              group: [
                '**/shared/api/core/api-http-client',
                'src/app/shared/api/generated/**',
                '**/shared/api/generated/**',
                '**/api/generated/**',
                '**/api-models/**',
              ],
              message:
                'Solo los adapters en endpoints/ pueden importar contratos generados. Los services usan el adapter de feature.',
            },
          ],
        },
      ],
    },
  },
  {
    files: [
      'src/app/features/**/pages/**/*.ts',
      'src/app/features/**/components/**/*.ts',
      'src/app/features/**/store/**/*.ts',
    ],
    ignores: ['**/*.spec.ts'],

    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: '@angular/common/http',
              message:
                'Las pages, components y stores deben usar servicios de feature, no HttpClient.',
            },
            {
              name: 'src/app/shared/api/core/api-http-client',
              message:
                'Las pages, components y stores deben depender de services/, no de ApiHttpClient.',
            },
            {
              name: 'src/environments/environment',
              message: 'Las pages, components y stores no deben resolver endpoints ni API_URL.',
            },
          ],
          patterns: [
            {
              group: [
                '**/endpoints/**',
                '../endpoints/**',
                '../../endpoints/**',
                'src/app/features/**/endpoints/**',
                '**/shared/api/core/api-http-client',
                'src/app/shared/api/generated/**',
                '**/shared/api/generated/**',
                '**/api/generated/**',
                '**/environments/environment',
              ],
              message:
                'Las pages, components y stores deben depender de services/, no de endpoints/.',
            },
          ],
        },
      ],
    },
  },
  ...compat
    .extends(
      'plugin:@angular-eslint/template/recommended',
      'plugin:@angular-eslint/template/accessibility'
    )
    .map(config => ({
      ...config,
      files: ['**/*.html'],
    })),
  {
    files: ['**/*.html'],
    rules: {
      '@angular-eslint/template/prefer-control-flow': 'error',
      '@angular-eslint/template/alt-text': 'error',
      '@angular-eslint/template/attributes-order': ['error', { alphabetical: true }],
      '@angular-eslint/template/button-has-type': 'error',
      '@angular-eslint/template/click-events-have-key-events': 'error',
      '@angular-eslint/template/conditional-complexity': ['error', { maxComplexity: 6 }],
      '@angular-eslint/template/cyclomatic-complexity': ['error', { maxComplexity: 5 }],
      '@angular-eslint/template/elements-content': 'error',
      '@angular-eslint/template/interactive-supports-focus': 'error',
      '@angular-eslint/template/label-has-associated-control': 'error',
      '@angular-eslint/template/no-any': 'error',
      '@angular-eslint/template/no-autofocus': 'error',
      '@angular-eslint/template/no-distracting-elements': 'error',
      '@angular-eslint/template/no-inline-styles': [
        'error',
        { allowBindToStyle: true, allowNgStyle: true },
      ],
      '@angular-eslint/template/no-interpolation-in-attributes': 'error',
      '@angular-eslint/template/no-positive-tabindex': 'error',
      '@angular-eslint/template/role-has-required-aria': 'error',
      '@angular-eslint/template/table-scope': 'error',
      '@angular-eslint/template/valid-aria': 'error',
      '@angular-eslint/template/prefer-self-closing-tags': 'error',
      '@angular-eslint/template/no-duplicate-attributes': 'error',
    },
  },
  eslintConfigPrettier,
];
