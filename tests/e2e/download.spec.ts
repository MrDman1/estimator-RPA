import { test, expect } from '@playwright/test';
import path from 'path';
import fs from 'fs';

/**
 * Downloads the "System Estimate PDF" from NSD into the /runs/<timestamp>/ folder.
 * Requires you to run the NSD auth setup first to create storageState/nsd.json:
 *   npm run e2e:auth:nsd
 *
 * Env:
 *  - NSD_BASE_URL
 *  - RUNS_DIR (optional; defaults to "runs")
 */
function stampedDir(root: string): string {
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const dir = path.resolve(root, stamp);
  fs.mkdirSync(dir, { recursive: true });
  return dir;
}

test('NSD downloads System Estimate PDF', async ({ browser }) => {
  const base = process.env.NSD_BASE_URL;
  if (!base) test.skip(true, 'NSD_BASE_URL not set');
  const storage = path.resolve('storageState', 'nsd.json');
  if (!fs.existsSync(storage)) test.skip(true, 'storageState/nsd.json missing. Run npm run e2e:auth:nsd first.');

  const context = await browser.newContext({
    storageState: storage,
    acceptDownloads: true,
  });
  const page = await context.newPage();
  await page.goto(base);

  // TODO: Navigate to the estimate you want and click the control that triggers "System Estimate PDF" download.
  // Examples (replace with real selectors):
  // await page.getByRole('link', { name: /estimates/i }).click();
  // await page.getByRole('row', { name: /Your Job Name/i }).click();
  // await page.getByRole('button', { name: /System Estimate PDF/i }).click();

  const [ download ] = await Promise.all([
    page.waitForEvent('download'),
    // TODO: The action that triggers the PDF download:
    page.getByRole('button', { name: /download system estimate pdf/i }).click(),
  ]);

  const runsRoot = process.env.RUNS_DIR || 'runs';
  const outDir = stampedDir(runsRoot);
  const suggested = await download.suggestedFilename();
  const outPath = path.join(outDir, suggested || 'SystemEstimate.pdf');
  await download.saveAs(outPath);

  // Basic asserts: file should exist and be non-empty
  const stat = fs.statSync(outPath);
  expect(stat.size).toBeGreaterThan(0);
  await context.close();
});
