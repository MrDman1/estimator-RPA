import { test, expect } from '@playwright/test';
import fs from 'fs';
import path from 'path';

/**
 * Saves authenticated storage states for Maximizer and NSD.
 * Reads credentials from environment variables:
 *  - MAX_BASE_URL, MAX_USER, MAX_PASS
 *  - NSD_BASE_URL, NSD_USER, NSD_PASS
 *
 * Usage:
 *   npm run e2e:auth:max
 *   npm run e2e:auth:nsd
 */
const storageDir = path.resolve('storageState');
const ensureDir = () => { if (!fs.existsSync(storageDir)) fs.mkdirSync(storageDir, { recursive: true }); };

test('Maximizer auth', async ({ page }) => {
  const base = process.env.MAX_BASE_URL;
  const user = process.env.MAX_USER;
  const pass = process.env.MAX_PASS;
  if (!base || !user || !pass) test.skip(true, 'Maximizer env vars not set');
  ensureDir();

  await page.goto(base);
  // TODO: Replace selectors below with the real login form fields.
  await page.getByLabel(/user(name)?/i).fill(user);
  await page.getByLabel(/pass(word)?/i).fill(pass);
  await page.getByRole('button', { name: /log.?in/i }).click();

  // TODO: Replace with a reliable post-login assertion (e.g., a user menu appears)
  await expect(page).toHaveTitle(/Maximizer/i);

  await page.context().storageState({ path: path.join(storageDir, 'maximizer.json') });
});

test('NSD auth', async ({ page }) => {
  const base = process.env.NSD_BASE_URL;
  const user = process.env.NSD_USER;
  const pass = process.env.NSD_PASS;
  if (!base || !user || !pass) test.skip(true, 'NSD env vars not set');
  ensureDir();

  await page.goto(base);
  // TODO: Replace selectors below with the real login form fields.
  await page.getByLabel(/email|user(name)?/i).fill(user);
  await page.getByLabel(/pass(word)?/i).fill(pass);
  await page.getByRole('button', { name: /log.?in|sign.?in/i }).click();

  // TODO: Replace with a reliable post-login assertion
  await expect(page).toHaveURL(/dashboard|home/i);

  await page.context().storageState({ path: path.join(storageDir, 'nsd.json') });
});
