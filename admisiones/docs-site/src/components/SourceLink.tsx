import type { ReactNode } from 'react';

import { sourceRepositories, type SourceRepositoryId } from '../config/source-repositories';

interface SourceLinkProps {
  children?: ReactNode;
  line?: number;
  path: string;
  repo: SourceRepositoryId;
}

export default function SourceLink({ children, line, path, repo }: SourceLinkProps): ReactNode {
  const source = sourceRepositories[repo];
  const directory = path.endsWith('/');
  const cleanPath = path.replace(/^\/+|\/+$/g, '');
  const href = `${source.url}/${directory ? 'tree' : 'blob'}/${source.ref}/${cleanPath}${
    line ? `#L${line}` : ''
  }`;

  return <a href={href}>{children ?? cleanPath}</a>;
}
