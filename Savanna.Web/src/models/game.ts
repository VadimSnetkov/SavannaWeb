export interface Animal {
  id: string;
  species: string;
  row: number;
  column: number;
  health: number;
  age: number;
  offspringCount: number;
  speed: number;
  visionRange: number;
}

export interface Species {
  name: string;
  symbol: string;
  spawnKey: string;
  isPredator: boolean;
  count: number;
}

export interface Game {
  id: string;
  width: number;
  height: number;
  round: number;
  isRunning: boolean;
  roundDelayMilliseconds: number;
  animals: Animal[];
  species: Species[];
  warnings: string[];
}

export type GameCommand = 'play' | 'pause' | 'step';
export type DisplayMode = 'icons' | 'letters';
