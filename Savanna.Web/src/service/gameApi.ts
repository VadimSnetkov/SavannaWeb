import type { Game, GameCommand } from '../models/game';

const requestTimeoutMilliseconds = 10_000;

export class ApiError extends Error {
  /// Summary:
  /// Keeps the HTTP status so an expired game can be handled separately.
  constructor(message: string, public readonly status: number) {
    super(message);
  }
}

/// Summary:
/// Calls the backend and converts problem responses into readable messages.
async function request<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`/api/games${path}`, {
      method,
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
      signal: AbortSignal.timeout(requestTimeoutMilliseconds),
    });
  } catch {
    throw new Error('Cannot reach the simulation. Check that the .NET backend is running.');
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    const validationErrors = problem.errors as Record<string, string[]> | undefined;
    const message = validationErrors ? Object.values(validationErrors).flat().join(' ') : problem.detail;
    throw new ApiError(message || 'The request failed. Please try again.', response.status);
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}

export const gameApi = {
  /// Summary:
  /// Creates an empty game on the server.
  create: () => request<Game>('', 'POST'),
  /// Summary:
  /// Reads a fresh snapshot without advancing the simulation.
  get: (id: string) => request<Game>(`/${id}`),
  /// Summary:
  /// Sends a play, pause or single-round command.
  command: (id: string, command: GameCommand) => request<Game>(`/${id}/${command}`, 'POST'),
  /// Summary:
  /// Adds an animal of an available species.
  spawn: (id: string, species: string) => request<Game>(`/${id}/animals`, 'POST', { species }),
  /// Summary:
  /// Releases a game and discards its progress.
  end: (id: string) => request<void>(`/${id}`, 'DELETE'),
};
