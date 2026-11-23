import type { CSSProperties } from 'react';
import type { DisplayMode, Game } from '../models/game';
import { AnimalSymbol } from './AnimalSymbol';

interface Props {
  game: Game;
  mode: DisplayMode;
  selectedId: string | null;
  onSelect: (id: string) => void;
}

/// Summary:
/// Draws animals at their server positions and highlights the selected animal.
export function GameField({ game, mode, selectedId, onSelect }: Props) {
  const style = {
    '--columns': game.width,
    '--rows': game.height,
    aspectRatio: `${game.width} / ${game.height}`,
    minWidth: `${game.width * 22}px`,
  } as CSSProperties;
  return <div className="field-scroll" tabIndex={0} aria-label="Scrollable game field">
    <div className="field" style={style} data-testid="game-field" aria-label="Savanna field">
      {game.animals.length === 0 && <div className="field-empty">
        <span className="empty-mark" aria-hidden="true">✳</span>
        <strong>A little quiet out here.</strong>
        <span>Add a few animals to bring your savanna to life.</span>
      </div>}
      {game.animals.map(animal => {
        const species = game.species.find(entry => entry.name === animal.species)!;
        return <button key={animal.id}
          className={`animal ${species.isPredator ? 'predator' : 'herbivore'} ${mode}`}
          style={{ gridRow: animal.row + 1, gridColumn: animal.column + 1 }}
          aria-label={`${animal.species}, row ${animal.row + 1}, column ${animal.column + 1}`}
          aria-pressed={selectedId === animal.id}
          title={`${animal.species} · ${animal.health} health`}
          onClick={() => onSelect(animal.id)}>
          <AnimalSymbol species={species} mode={mode} />
        </button>;
      })}
    </div>
  </div>;
}
