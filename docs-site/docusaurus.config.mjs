import { themes as prismThemes } from 'prism-react-renderer';

const config = {
  title: 'DesarrolloORT Frontend Docs',
  tagline: 'Documentación técnica y funcional de los proyectos frontend',
  url: process.env.DOCS_SITE_URL ?? 'http://localhost',
  baseUrl: '/',
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
        blog: false,
        docs: {
          path: '../docs',
          routeBasePath: 'proyectos/admisiones',
          include: ['*.md'],
          sidebarPath: './sidebars.js',
          editUrl: 'https://github.com/DesarrolloORT/admisiones/edit/main/',
        },
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
      title: 'Frontend Docs',
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'admissionsSidebar',
          position: 'left',
          label: 'Admisiones',
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
