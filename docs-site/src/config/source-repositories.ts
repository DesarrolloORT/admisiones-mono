export const sourceRepositories = {
  frontend: {
    url: 'https://github.com/DesarrolloORT/admisiones',
    ref: 'v1.0.0/main',
  },
  backend: {
    url: 'https://github.com/DesarrolloORT/api-admisiones',
    ref: 'develop',
  },
} as const;

export type SourceRepositoryId = keyof typeof sourceRepositories;
