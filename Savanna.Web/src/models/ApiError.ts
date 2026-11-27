export class ApiError extends Error {
  /// Summary:
  /// Keeps the HTTP status alongside the message shown by the frontend.
  constructor(message: string, public readonly status: number) {
    super(message);
  }
}
