import { HttpStatus } from '../models/HttpStatus';

/// Summary:
/// Maps HTTP outcomes to useful messages and safely reads validation problem details.
export function apiErrorMessage(status: number, body: unknown): string {
  const problem = body !== null && typeof body === 'object'
    ? body as Record<string, unknown> : {};
  const detail = typeof problem.detail === 'string' ? problem.detail : '';
  const errors = problem.errors !== null && typeof problem.errors === 'object'
    ? Object.values(problem.errors).flatMap(value => Array.isArray(value)
      ? value.filter((message): message is string => typeof message === 'string') : []) : [];

  switch (status) {
    case HttpStatus.BadRequest:
      return 'The request could not be read. Refresh the page and try again.';
    case HttpStatus.Unauthorized:
      return 'Your session is no longer authorized. Please sign in again.';
    case HttpStatus.Forbidden:
      return 'You do not have permission to access this game.';
    case HttpStatus.NotFound:
      return 'This game has ended or expired. Start a new game.';
    case HttpStatus.Conflict:
      return detail || 'This action is not available in the current game state. Refresh the game and try again.';
    case HttpStatus.UnprocessableEntity:
      return errors.length > 0 ? errors.join(' ') : detail || 'Check your input and try again.';
    case HttpStatus.TooManyRequests:
      return 'Too many requests. Please wait a moment before trying again.';
    case HttpStatus.BadGateway:
    case HttpStatus.ServiceUnavailable:
    case HttpStatus.GatewayTimeout:
      return 'The simulation service is temporarily unavailable. Please try again shortly.';
    default:
      // Never expose stack traces or arbitrary server exception details to the player.
      return status >= HttpStatus.InternalServerError
        ? 'The server could not complete this action. Please try again or start a new game.'
        : 'The request failed. Please try again.';
  }
}
