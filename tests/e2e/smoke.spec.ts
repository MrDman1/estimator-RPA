import { test, expect } from '@playwright/test';

test('Maximizer login page loads', async ({ page }) => {
  await page.goto('http://crm.nuformdirect.com/MaximizerWebAccess/Default.aspx');
  await expect(page).toHaveTitle(/Maximizer/i);
});
