import { test, expect } from '@playwright/test';
import { apiErrorMessage } from '../src/service/apiErrorMessage';
import { ApiError } from '../src/models/ApiError';
import { gameApi } from '../src/service/gameApi';

const statusCases: [number, string][] = [
  [400, 'could not be read'], [401, 'sign in again'], [403, 'permission'],
  [404, 'Start a new game'], [409, 'current game state'], [422, 'Check your input'],
  [429, 'wait a moment'], [500, 'server could not complete'],
  [502, 'temporarily unavailable'], [503, 'temporarily unavailable'], [504, 'temporarily unavailable'],
];

for (const [status, message] of statusCases) {
  test(`HTTP ${status} has an actionable fallback`, () => {
    expect(apiErrorMessage(status, null)).toContain(message);
  });
}

test('422 shows validation messages and tolerates unexpected response shapes', () => {
  expect(apiErrorMessage(422, { errors: { Species: ['Choose an available species.'] } }))
    .toBe('Choose an available species.');
  expect(apiErrorMessage(422, { errors: { Species: null, Other: [42] } })).toContain('Check your input');
});

test('409 explains the conflict while 500 does not expose exception details', () => {
  expect(apiErrorMessage(409, { detail: 'The field is full.' })).toBe('The field is full.');
  expect(apiErrorMessage(500, { detail: 'Private exception details.' })).not.toContain('Private');
});

test('the HTTP client preserves status and maps non-JSON failures', async () => {
  const originalFetch = globalThis.fetch;
  try {
    globalThis.fetch = async () => new Response('proxy unavailable', { status: 503 });
    await expect(gameApi.create()).rejects.toBeInstanceOf(ApiError);
    await expect(gameApi.create()).rejects.toMatchObject({ status: 503, message: expect.stringContaining('temporarily unavailable') });
  } finally {
    globalThis.fetch = originalFetch;
  }
});
