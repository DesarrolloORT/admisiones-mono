import type { Config } from '@docusaurus/types';
import { themes as prismThemes } from 'prism-react-renderer';

const siteOrigin = process.env.DOCS_SITE_ORIGIN || 'http://localhost';
const baseUrl = process.env.DOCS_BASE_URL || '/docs/admisiones/';
const sourceBranch = process.env.DOCS_SOURCE_BRANCH || 'v1.0.0/main';
const parsedOrigin = new URL(siteOrigin);

if (!['http:', 'https:'].includes(parsedOrigin.protocol) || parsedOrigin.pathname !== '/') {
  throw new Error('DOCS_SITE_ORIGIN must be an HTTP(S) origin without a path.');
}

if (!baseUrl.startsWith('/') || !baseUrl.endsWith('/')) {
  throw new Error('DOCS_BASE_URL must start and end with "/".');
}

const config: Config = {
  title: 'Admisiones',
  tagline: 'Documentacion tecnica y funcional de Admisiones',
  url: parsedOrigin.origin,
  baseUrl,
  trailingSlash: true,
  onBrokenLinks: 'throw',
  favicon: 'img/favicon.svg',
  headTags: [
    {
      tagName: 'meta',
      attributes: {
        name: 'robots',
        content: 'noindex,nofollow,noarchive',
      },
    },
  ],
  i18n: {
    defaultLocale: 'es',
    locales: ['es'],
  },
  markdown: {
    mermaid: true,
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },
  presets: [
    [
      'classic',
      {
        docs: {
          path: '../docs',
          routeBasePath: '/',
          sidebarPath: './sidebars.ts',
          editUrl: `https://github.com/DesarrolloORT/admisiones/edit/${encodeURIComponent(sourceBranch)}/docs/`,
        },
        blog: false,
        sitemap: false,
      },
    ],
  ],
  themes: ['@docusaurus/theme-mermaid'],
  themeConfig: {
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Admisiones',
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'admissionsSidebar',
          position: 'left',
          label: 'Documentacion',
        },
      ],
    },
    footer: {
      style: 'dark',
      copyright: `DesarrolloORT ${new Date().getFullYear()}`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  },
};

export default config;
