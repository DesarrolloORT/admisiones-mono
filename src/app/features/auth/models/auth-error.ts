export type AuthRequestOperation = 'login' | 'register';

export class AuthRequestError extends Error {
  public constructor(
    public readonly operation: AuthRequestOperation,
    public readonly status: number | null
  ) {
    super(operation);
  }
}
