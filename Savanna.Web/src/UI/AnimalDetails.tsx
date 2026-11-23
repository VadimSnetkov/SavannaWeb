import type { Animal } from '../models/game';

/// Summary:
/// Shows current statistics for the animal selected on the field.
export function AnimalDetails({ animal }: { animal?: Animal }) {
  return <section className="panel details" aria-label="Animal statistics">
    <p className="eyebrow">FIELD NOTES</p>
    <h2>{animal ? animal.species : 'Meet your animals'}</h2>
    {animal ? <dl className="statistics">
      <div><dt>Age</dt><dd>{animal.age} {animal.age === 1 ? 'round' : 'rounds'}</dd></div>
      <div><dt>Health</dt><dd>{animal.health.toFixed(1)}</dd></div>
      <div><dt>Offspring</dt><dd>{animal.offspringCount}</dd></div>
      <div><dt>Speed</dt><dd>{animal.speed} cells / round</dd></div>
      <div><dt>Vision</dt><dd>{animal.visionRange} cells</dd></div>
      <div><dt>Position</dt><dd>{animal.row + 1}, {animal.column + 1}</dd></div>
    </dl> : <p className="muted">Select an animal on the field to see its health, age and offspring.</p>}
  </section>;
}
