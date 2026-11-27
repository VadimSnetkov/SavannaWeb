import { useEffect, useState } from 'react';
import { useSimulation } from './hooks/useSimulation';
import type { DisplayMode } from './models/game';
import { AnimalDetails } from './UI/AnimalDetails';
import { AnimalSymbol } from './UI/AnimalSymbol';
import { GameField } from './UI/GameField';

/// Summary:
/// Provides the basic browser controls for the shared Savanna simulation.
export default function App() {
  const { game, busy, error, hasProgress, newGame, spawn, command } = useSimulation();
  const [mode, setMode] = useState<DisplayMode>('icons');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const selectedAnimal = game?.animals.find(animal => animal.id === selectedId);

  useEffect(() => {
    if (selectedId && !selectedAnimal) setSelectedId(null);
  }, [selectedId, selectedAnimal]);

  useEffect(() => {
    /// Summary:
    /// Supports configured species keys and Space to play or pause.
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement;
      if (!game || busy || event.repeat || event.ctrlKey || event.altKey || event.metaKey ||
          target.closest('input, textarea, select, [contenteditable="true"]')) return;
      const species = game.species.find(entry => entry.spawnKey.toLowerCase() === event.key.toLowerCase());
      if (species) {
        event.preventDefault();
        void spawn(species.name);
      } else if (event.code === 'Space' && !target.closest('button')) {
        event.preventDefault();
        void command(game.isRunning ? 'pause' : 'play');
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [game, busy, spawn, command]);

  /// Summary:
  /// Confirms intentional progress loss before replacing the current game.
  function startNewGame() {
    if (hasProgress && !window.confirm('Start a new game? Your current progress will be lost.')) return;
    setSelectedId(null);
    void newGame();
  }

  return <div className="app-shell">
    <header className="topbar">
      <a className="brand" href="#" onClick={event => event.preventDefault()} aria-label="Savanna home">
        <span className="brand-mark" aria-hidden="true">s.</span>
        <span>Savanna<small>A SMALL, LIVING WORLD</small></span>
      </a>
      <span className="version-label">WEB EDITION <span>01</span></span>
    </header>

    <main>
      <div className="page-heading">
        <div><p className="eyebrow">YOUR OWN PATCH OF WILDERNESS</p><h1>Let life happen.</h1>
          <p className="intro">Add animals. Watch them wander, hunt and grow.</p></div>
        <button className="button secondary" disabled={busy} onClick={startNewGame}>
          {game ? 'New game' : busy ? 'Starting…' : 'Start a game'}
        </button>
      </div>

      {error && <div className="error" role="alert">{error}</div>}
      {game?.warnings.map(warning => <div className="warning" role="status" key={warning}>{warning}</div>)}

      <div className="workspace">
        <section className="panel simulation" aria-label="Simulation">
          <div className="field-toolbar">
            <div className="field-title"><h2>The savanna</h2>
              <span className={`status ${game?.isRunning ? 'running' : ''}`}>
                <i />{game ? game.isRunning ? 'Running' : 'Paused' : 'Ready when you are'}
              </span>
            </div>
            <div className="display-toggle" aria-label="Animal display">
              <button aria-pressed={mode === 'icons'} onClick={() => setMode('icons')}>Icons</button>
              <button aria-pressed={mode === 'letters'} onClick={() => setMode('letters')}>Letters</button>
            </div>
          </div>

          {game ? <GameField game={game} mode={mode} selectedId={selectedId} onSelect={setSelectedId} /> :
            <div className="welcome-field">
              <span className="welcome-animals" aria-hidden="true">🦌 🦁 🦓</span>
              <h2>Every ecosystem starts somewhere.</h2>
              <p>Create a game, add your first animals, and press Play.</p>
              <button className="button primary" disabled={busy} onClick={startNewGame}>
                {busy ? 'Starting…' : 'Create game'}
              </button>
            </div>}

          <div className="game-controls">
            <div className="play-controls">
              <button className="button primary" disabled={!game || busy}
                onClick={() => void command(game?.isRunning ? 'pause' : 'play')}>
                <span aria-hidden="true">{game?.isRunning ? 'Ⅱ' : '▶'}</span> {game?.isRunning ? 'Pause' : 'Play'}
              </button>
              <button className="button secondary" disabled={!game || busy || game.isRunning}
                onClick={() => void command('step')}>Step +1</button>
            </div>
            <div className="counters">
              <div><span>ROUND</span><strong data-testid="round">{game?.round ?? 0}</strong></div>
              <div><span>LIVING ANIMALS</span><strong data-testid="animal-count">{game?.animals.length ?? 0}</strong></div>
            </div>
          </div>
          <p className="field-caption">{game ? `${game.width} × ${game.height} field · One round every ${game.roundDelayMilliseconds / 1000}s` : 'Your simulation runs in the .NET backend.'}
            <span>Space to play / pause</span></p>
        </section>

        <aside>
          <section className="panel species-panel">
            <p className="eyebrow">BUILD YOUR ECOSYSTEM</p><h2>Add an animal</h2>
            {game ? <div className="species-list">{game.species.map(species =>
              <button className="species-button" key={species.name} disabled={busy || game.animals.length >= game.width * game.height}
                aria-label={`Add ${species.name}`} onClick={() => void spawn(species.name)}>
                <span className={`species-icon ${species.isPredator ? 'warm' : ''}`}><AnimalSymbol species={species} mode={mode} /></span>
                <span className="species-name">{species.name}<small>{species.isPredator ? 'Predator' : 'Herbivore'} · {species.count} living</small></span>
                <kbd>{species.spawnKey}</kbd><span className="plus" aria-hidden="true">+</span>
              </button>)}</div> : <p className="muted">Start a game to see the available animals, including loaded plugins.</p>}
          </section>
          <AnimalDetails animal={selectedAnimal} />
          <div className="session-note"><span aria-hidden="true">◷</span><p><strong>Just for this session.</strong>
            Games are not saved yet. Leaving or refreshing the page loses your progress.</p></div>
        </aside>
      </div>
    </main>
    <footer><span>SAVANNA / VADIM SNETKOV</span><span>Small rules. A world of possibilities.</span></footer>
  </div>;
}
