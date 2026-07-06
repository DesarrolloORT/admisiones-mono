import type { Config } from '@docusaurus/types';
import { themes as prismThemes } from 'prism-react-renderer';

const config: Config = {
  title: 'Admisiones',
  tagline: 'Documentacion tecnica y funcional de Admisiones',
  url: 'https://ort-docs.ort.edu.uy',
  baseUrl: '/admisiones/',
  trailingSlash: true,
  onBrokenLinks: 'warn',
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
      onBrokenMarkdownLinks: 'warn',
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
          editUrl: 'https://github.com/DesarrolloORT/admisiones/edit/main/docs/',
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
