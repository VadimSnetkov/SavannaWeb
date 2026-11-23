import { expect, test } from '@playwright/test';

test('creates a game, adds plugin animals, selects statistics, switches display and steps', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Create game', exact: true }).click();
  await page.getByRole('button', { name: 'Add Antelope', exact: true }).click();
  await page.getByRole('button', { name: 'Add Zebra', exact: true }).click();
  await expect(page.getByTestId('animal-count')).toHaveText('2');
  const zebra = page.getByRole('button', { name: /^Zebra, row/ });
  await zebra.click();
  await expect(zebra).toHaveAttribute('aria-pressed', 'true');
  const statistics = page.getByRole('region', { name: 'Animal statistics' });
  await expect(statistics).toContainText('0 rounds');
  await page.getByRole('button', { name: 'Letters', exact: true }).click();
  await expect(zebra).toHaveText('z');
  await page.getByRole('button', { name: 'Step +1', exact: true }).click();
  await expect(page.getByTestId('round')).toHaveText('1');
  await expect(statistics).toContainText('1 round');
  await expect(zebra).toHaveAttribute('aria-pressed', 'true');
  await page.getByRole('button', { name: 'Icons', exact: true }).click();
  await expect(zebra).toHaveText('🦓');
});

test('runs and pauses on the server and supports keyboard spawning', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Create game', exact: true }).click();
  await page.getByRole('heading', { name: 'Let life happen.' }).click();
  await page.keyboard.press('a');
  await expect(page.getByTestId('animal-count')).toHaveText('1');
  await page.keyboard.press('l');
  await expect(page.getByTestId('animal-count')).toHaveText('2');
  await page.keyboard.press('z');
  await expect(page.getByTestId('animal-count')).toHaveText('3');
  await page.keyboard.press('Space');
  await expect(page.getByTestId('round')).not.toHaveText('0');
  await page.getByRole('button', { name: 'Pause', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Play', exact: true })).toBeEnabled();
  const round = await page.getByTestId('round').textContent();
  await page.waitForTimeout(1000);
  await expect(page.getByTestId('round')).toHaveText(round!);
});

test('keeps browser games separate and confirms resetting progress', async ({ page, context }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Create game', exact: true }).click();
  await page.getByRole('button', { name: 'Add Antelope', exact: true }).click();
  const second = await context.newPage();
  await second.goto('/');
  await second.getByRole('button', { name: 'Create game', exact: true }).click();
  await expect(second.getByTestId('animal-count')).toHaveText('0');
  await expect(page.getByTestId('animal-count')).toHaveText('1');
  page.once('dialog', dialog => dialog.dismiss());
  await page.getByRole('button', { name: 'New game', exact: true }).click();
  await expect(page.getByTestId('animal-count')).toHaveText('1');
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'New game', exact: true }).click();
  await expect(page.getByTestId('animal-count')).toHaveText('0');
  await expect(page.getByTestId('round')).toHaveText('0');
});

test('warns before reloading unsaved progress and does not restore it', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Create game', exact: true }).click();
  await page.getByRole('button', { name: 'Add Antelope', exact: true }).click();
  const dialogEvent = page.waitForEvent('dialog');
  const reloading = page.reload();
  const dialog = await dialogEvent;
  expect(dialog.type()).toBe('beforeunload');
  await dialog.accept();
  await reloading;
  await expect(page.getByRole('button', { name: 'Create game', exact: true })).toBeVisible();
  await expect(page.getByTestId('animal-count')).toHaveText('0');
});

test('API validates requests and enforces game lifecycle', async ({ request }) => {
  const created = await request.post('/api/games');
  expect(created.ok()).toBeTruthy();
  const game = await created.json();
  const root = `/api/games/${game.id}`;
  const invalid = await request.post(`${root}/animals`, { data: { species: 'Dragon' } });
  expect(invalid.status()).toBe(400);
  const malformed = await request.post(`${root}/animals`, { data: {} });
  expect(malformed.status()).toBe(400);
  expect((await request.post(`${root}/animals`, { data: { species: 'Zebra' } })).ok()).toBeTruthy();
  await request.post(`${root}/play`);
  expect((await request.post(`${root}/step`)).status()).toBe(409);
  await request.delete(root);
  expect((await request.get(root)).status()).toBe(404);
});

test('shows a useful message when the backend is unavailable', async ({ page }) => {
  await page.route('**/api/games', route => route.abort());
  await page.goto('/');
  await page.getByRole('button', { name: 'Create game', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Check that the .NET backend is running');
  await expect(page.getByRole('button', { name: 'Create game', exact: true })).toBeEnabled();
});

test('fits a small screen while keeping the field scrollable', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  await page.getByRole('button', { name: 'Create game', exact: true }).click();
  await page.getByRole('button', { name: 'Add Lion', exact: true }).click();
  await expect(page.getByTestId('animal-count')).toHaveText('1');
  const overflows = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  expect(overflows).toBeFalsy();
});
