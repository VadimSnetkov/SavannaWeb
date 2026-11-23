import type { DisplayMode, Species } from '../models/game';

const icons: Record<string, string> = { Antelope: '🦌', Lion: '🦁', Zebra: '🦓' };

/// Summary:
/// Displays an icon for known species and falls back to the configured letter.
export function AnimalSymbol({ species, mode }: { species: Species; mode: DisplayMode }) {
  return <span aria-hidden="true">{mode === 'icons' ? icons[species.name] ?? species.symbol : species.symbol}</span>;
}
