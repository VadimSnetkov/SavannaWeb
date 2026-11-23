import { useCallback, useEffect, useRef, useState } from 'react';
import type { Game, GameCommand } from '../models/game';
import { ApiError, gameApi } from '../service/gameApi';

/// Summary:
/// Coordinates commands and polling so older responses cannot overwrite newer state.
export function useSimulation() {
  const [game, setGame] = useState<Game | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const gameRef = useRef<Game | null>(null);
  const busyRef = useRef(false);
  const queue = useRef<Promise<unknown>>(Promise.resolve());

  /// Summary:
  /// Updates the state and the latest-game reference together.
  const accept = useCallback((next: Game | null) => {
    gameRef.current = next;
    setGame(next);
  }, []);

  /// Summary:
  /// Runs requests in order, including requests made by the refresh timer.
  const enqueue = useCallback((operation: () => Promise<void>) => {
    const pending = queue.current.then(operation);
    queue.current = pending.catch(() => undefined);
    return pending;
  }, []);

  /// Summary:
  /// Shows connection errors and lets the user replace expired games.
  const reportError = useCallback((failure: unknown) => {
    if (failure instanceof ApiError && failure.status === 404) accept(null);
    setError(failure instanceof Error ? failure.message : 'Something went wrong. Try again.');
  }, [accept]);

  /// Summary:
  /// Prevents duplicate commands while a previous button action is pending.
  const perform = useCallback(async (operation: () => Promise<void>) => {
    if (busyRef.current) return;
    busyRef.current = true;
    setBusy(true);
    setError('');
    try { await enqueue(operation); }
    catch (failure) { reportError(failure); }
    finally {
      busyRef.current = false;
      setBusy(false);
    }
  }, [enqueue, reportError]);

  /// Summary:
  /// Discards the previous game before creating a fresh, paused field.
  const newGame = useCallback(() => perform(async () => {
    if (gameRef.current) await gameApi.end(gameRef.current.id);
    accept(null);
    accept(await gameApi.create());
  }), [perform, accept]);

  /// Summary:
  /// Adds one animal without changing whether the game is running.
  const spawn = useCallback((species: string) => perform(async () => {
    if (gameRef.current) accept(await gameApi.spawn(gameRef.current.id, species));
  }), [perform, accept]);

  /// Summary:
  /// Controls the server clock or advances exactly one paused round.
  const command = useCallback((action: GameCommand) => perform(async () => {
    if (gameRef.current) accept(await gameApi.command(gameRef.current.id, action));
  }), [perform, accept]);

  const gameId = game?.id;
  const refreshMilliseconds = game?.roundDelayMilliseconds;
  useEffect(() => {
    if (!gameId || !refreshMilliseconds) return;
    let active = true;
    let pending = false;
    const timer = window.setInterval(() => {
      if (pending || busyRef.current) return;
      pending = true;
      void enqueue(async () => {
        if (!active || gameRef.current?.id !== gameId) return;
        try {
          const next = await gameApi.get(gameId);
          if (active && gameRef.current?.id === gameId) accept(next);
        } catch (failure) {
          if (active) reportError(failure);
        }
      }).finally(() => { pending = false; });
    }, refreshMilliseconds);
    return () => {
      active = false;
      window.clearInterval(timer);
    };
  }, [gameId, refreshMilliseconds, enqueue, accept, reportError]);

  const hasProgress = !!game && (game.round > 0 || game.animals.length > 0);
  useEffect(() => {
    if (!hasProgress) return;
    /// Summary:
    /// Requests the browser's standard warning before unsaved progress is lost.
    const warnBeforeLeaving = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = '';
    };
    window.addEventListener('beforeunload', warnBeforeLeaving);
    return () => window.removeEventListener('beforeunload', warnBeforeLeaving);
  }, [hasProgress]);

  return { game, busy, error, hasProgress, newGame, spawn, command };
}
