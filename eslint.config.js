import js from '@eslint/js';
import angular from 'angular-eslint';
import eslintConfigPrettier from 'eslint-config-prettier';
import simpleImportSort from 'eslint-plugin-simple-import-sort';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  {
    ignores: [
      '**/tests/',
      '**/api/',
      '**/coverage/',
      '**/dist/',
      '**/docs-site/build/',
      '**/docs-site/.docusaurus/',
      '**/node_modules/',
      '**/.angular/',
      '**/index.html',
      '**/playwright-report/',
      'tmp/',
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
  {
    files: ['**/*.ts'],
    extends: [
      js.configs.recommended,
      ...tseslint.configs.recommended,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
  },
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
      '@angular-eslint/prefer-on-push-component-change-detection': 'off',
    },
  },
  {
    files: ['src/app/features/**/*.ts'],
    ignores: ['src/app/features/**/api/**/*.ts'],

    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: 'src/app/shared/api/core/api-http-client',
              message:
                'Las features deben usar su adapter en api/. ApiHttpClient vive detras de esa capa.',
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
                'Solo los adapters en api/ pueden importar contratos generados. Los services usan el adapter de feature.',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['src/app/features/**/pages/**/*.ts', 'src/app/features/**/components/**/*.ts'],
    ignores: ['**/*.spec.ts'],

    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: '@angular/common/http',
              message: 'Las pages y components deben usar el adapter de la feature, no HttpClient.',
            },
            {
              name: 'src/app/shared/api/core/api-http-client',
              message:
                'Las pages y components pueden inyectar el adapter de la feature (api/), pero no ApiHttpClient.',
            },
            {
              name: 'src/environments/environment',
              message: 'Las pages y components no deben resolver endpoints ni API_URL.',
            },
          ],
          patterns: [
            {
              group: [
                '**/shared/api/core/api-http-client',
                'src/app/shared/api/generated/**',
                '**/shared/api/generated/**',
                '**/api/generated/**',
                '**/environments/environment',
              ],
              message:
                'Las pages y components pueden inyectar el adapter de su feature (api/), pero solo el adapter puede importar contratos generados, ApiHttpClient o environment.',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [...angular.configs.templateRecommended, ...angular.configs.templateAccessibility],
    rules: {
      '@angular-eslint/template/prefer-control-flow': 'error',
      '@angular-eslint/template/alt-text': 'error',
      '@angular-eslint/template/attributes-order': ['error', { alphabetical: true }],
      '@angular-eslint/template/button-has-type': 'error',
      '@angular-eslint/template/click-events-have-key-events': 'error',
      '@angular-eslint/template/conditional-complexity': ['error', { maxComplexity: 6 }],
      '@angular-eslint/template/cyclomatic-complexity': ['error', { maxComplexity: 8 }],
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
  eslintConfigPrettier
);
