/**
 * Nuform automation script for creating a new estimate across
 * Maximizer, NSD, and Trello.  This script uses Playwright to
 * drive a headed Chromium instance.  When run it will prompt
 * the user to log in to each external system (Maximizer and NSD)
 * if no saved session exists.  Once authenticated the script
 * proceeds to create an opportunity in Maximizer, a BOM in NSD,
 * update the opportunity with the BOM number, and optionally
 * generate a Trello card and download the system estimate.
 *
 * Usage:
 *
 *   node create_estimate.js '{ "companyName": "...", "contactName": "...", ... }'
 *
 * Required fields in the job object:
 *   companyName       – Name of the customer/company in Maximizer and NSD.
 *   contactName       – Contact name associated with the company.
 *   objective         – Title of the opportunity (used as description in NSD).
 *   estimateNumber    – Unique estimate number provided by the user.
 *   salesPerson       – Name of the sales person (must exist in dropdowns).
 *   estimator         – Name of the estimator (must exist in dropdowns).
 *   opportunitySource – One of Maximizer's permanent fields (e.g. "Sales Agent").
 *   currency          – Either "CAD" or "USD".
 *   products          – Array of product identifiers (e.g. ["CF4", "CF6"]).
 *   categories        – Array of category identifiers.
 *   discounts         – Object mapping material type to discount percentage.
 *   shipTo            – Either "Pickup" or an object with address fields.
 *   trelloEnabled     – Optional boolean to create a Trello card.
 *   sofPath           – Optional path to a Signed Order Form PDF to upload.
 *
 * The script will save session state to the `./.data` directory so
 * subsequent runs can skip the login screens.  If you ever need
 * to re‑authenticate simply delete the files in `.data`.
 */

const fs = require('fs');
const path = require('path');
const { chromium, expect } = require('playwright');

async function ensureDir(dir) {
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
}

async function run(job) {
  // Directory to store Playwright storage states.  Each site gets its own file.
  const dataDir = path.join(__dirname, '.data');
  await ensureDir(dataDir);
  const maxState = path.join(dataDir, 'maximizer.json');
  const nsdState = path.join(dataDir, 'nsd.json');

  const browser = await chromium.launch({ headless: false });

  // Helper to create a context for a given storage state file.
  async function newContext(statePath) {
    let opts = { viewport: { width: 1280, height: 900 } };
    if (fs.existsSync(statePath)) {
      opts.storageState = statePath;
    } else {
      opts.storageState = undefined;
    }
    return await browser.newContext({ ...opts, acceptDownloads: true });
  }

  /** Login to Maximizer if no saved session exists. */
  async function loginMaximizer(page) {
    await page.goto('http://crm.nuformdirect.com/MaximizerWebAccess/Default.aspx');
    // Wait for login form.
    await page.waitForSelector('input[name="User ID"], input[name="userID"], input[type="text"]', { timeout: 60000 });
    console.log('🛑 Please log in to Maximizer in the opened browser. After completing login, press Enter in the terminal.');
    await new Promise(resolve => process.stdin.once('data', resolve));
    // Save session
    await page.context().storageState({ path: maxState });
  }

  /** Login to NSD if no saved session exists. */
  async function loginNSD(page) {
    await page.goto('http://nsd.nuformdirect.com/login');
    await page.waitForSelector('input[type="password"]', { timeout: 60000 });
    console.log('🛑 Please log in to NSD in the opened browser. After completing login, press Enter in the terminal.');
    await new Promise(resolve => process.stdin.once('data', resolve));
    await page.context().storageState({ path: nsdState });
  }

  /**
   * Find and open a company in Maximizer.
   * Navigates to the Address Book, uses the search bar to locate the
   * company by name, and opens the row to reveal the bottom panel
   * containing the Opportunities tab.
   */
  async function openCompany(page, companyName) {
    // Navigate to Address Book.
    await page.goto('http://crm.nuformdirect.com/MaximizerWebAccess/Address%20Book');
    await page.waitForSelector('input[placeholder*=Search]', { timeout: 30000 });
    const searchBox = await page.locator('input[placeholder*=Search]');
    await searchBox.fill(companyName);
    await searchBox.press('Enter');
    // Wait for row to appear.
    await page.waitForSelector(`tr:has(td:text-matches("${companyName}", "i"))`, { timeout: 30000 });
    // Click the row.
    await page.click(`tr:has(td:text-matches("${companyName}", "i"))`);
    // Wait for bottom panel Opportunities tab.
    await page.waitForSelector('button[title="Opportunities"]', { timeout: 20000 });
    // Select it.
    await page.click('button[title="Opportunities"]');
  }

  /** Create a new opportunity in Maximizer using the job details. */
  async function createOpportunity(page, job) {
    // Wait for the Add icon and click it.
    await page.waitForSelector('button[title="Add"]', { timeout: 20000 });
    await page.click('button[title="Add"]');
    // Fill out the form fields.
    await page.waitForSelector('input[name="Objective"]', { timeout: 20000 });
    await page.fill('input[name="Objective"]', job.objective);
    // Description (optional)
    if (job.description) {
      await page.fill('textarea[name="Description"]', job.description);
    }
    // Contact
    await page.click('input[name="Contact"]');
    await page.fill('input[name="Contact"]', job.contactName);
    await page.keyboard.press('Enter');
    // Products/Services multi‑select
    for (const prod of job.products || []) {
      await page.click('div[role="combobox"]:has(label:text-matches("Products", "i"))');
      await page.fill('div[role="combobox"] input', prod);
      await page.keyboard.press('Enter');
    }
    // Categories
    for (const cat of job.categories || []) {
      await page.click('div[role="combobox"]:has(label:text-matches("Categories", "i"))');
      await page.fill('div[role="combobox"] input', cat);
      await page.keyboard.press('Enter');
    }
    // Sales person dropdown
    await page.selectOption('select[name="Sales Person"]', { label: job.salesPerson });
    // Estimator dropdown
    await page.selectOption('select[name="Estimator"]', { label: job.estimator });
    // Opportunity source
    await page.selectOption('select[name="Opportunity Source"]', { label: job.opportunitySource });
    // Estimate number and currency
    await page.fill('input[name="Estimate #"]', job.estimateNumber);
    await page.selectOption('select[name="Currency"]', { label: job.currency });
    // Shipping
    if (job.shipTo === 'Pickup') {
      await page.selectOption('select[name="Ship To"]', { label: 'Pickup' });
    } else if (typeof job.shipTo === 'object') {
      await page.selectOption('select[name="Ship To"]', { label: 'Address' });
      await page.fill('input[name="Ship To - Street"]', job.shipTo.street || '');
      await page.fill('input[name="Ship To - City"]', job.shipTo.city || '');
      await page.fill('input[name="Ship To - State or Province"]', job.shipTo.state || '');
    }
    // Discounts: iterate over keys and fill.
    for (const [material, pct] of Object.entries(job.discounts || {})) {
      const fieldSel = `input[name^="${material}"]`;
      if (await page.$(fieldSel)) {
        await page.fill(fieldSel, String(pct));
      }
    }
    // Save opportunity
    await page.click('button:has-text("SAVE")');
    // Wait for success toast or navigation
    await page.waitForTimeout(5000);
  }

  /** Create a BOM in NSD using job details.  Returns BOM number. */
  async function createBOM(page, job) {
    // Navigate to BOMs page
    await page.goto('http://nsd.nuformdirect.com/bom-search');
    // Wait for + NEW BOM button
    await page.waitForSelector('button:has-text("+ NEW BOM")', { timeout: 20000 });
    await page.click('button:has-text("+ NEW BOM")');
    // Wait for modal
    await page.waitForSelector('div[role="dialog"]', { timeout: 20000 });
    // Customer dropdown
    await page.click('div[role="dialog"] label:has-text("Customer*") + div [role="button"]');
    await page.fill('div.v-select-list input', job.companyName);
    await page.keyboard.press('Enter');
    // Customer contact
    await page.click('div[role="dialog"] label:has-text("Customer Contact*") + div [role="button"]');
    await page.fill('div.v-select-list input', job.contactName);
    await page.keyboard.press('Enter');
    // Sales Person
    await page.click('div[role="dialog"] label:has-text("Sales Person*") + div [role="button"]');
    await page.fill('div.v-select-list input', job.salesPerson);
    await page.keyboard.press('Enter');
    // Estimator
    await page.click('div[role="dialog"] label:has-text("Estimator*") + div [role="button"]');
    await page.fill('div.v-select-list input', job.estimator);
    await page.keyboard.press('Enter');
    // Description
    await page.fill('div[role="dialog"] label:has-text("Description*") + div textarea', job.objective);
    // Estimate #
    await page.fill('div[role="dialog"] label:has-text("Estimate #") + div input', job.estimateNumber);
    // Discounts: fill each discount column (Renu Panel, Reline Panel etc.)
    for (const [material, pct] of Object.entries(job.discounts || {})) {
      const row = {
        conform: ['Conform', 'CONFORM'],
        renu: ['Renu Panel', 'RENU'],
        reline: ['Reline Panel', 'RELINE'],
        other: ['Other (non PVC)', 'OTHER']
      }[material.toLowerCase()];
      if (row) {
        const label = row[1];
        await page.fill(`div[role="dialog"] label:has-text("${label}") ~ div input[placeholder*="Discount"]`, String(pct));
      }
    }
    // Material type (open modal)
    await page.click('div[role="dialog"] label:has-text("Material Type*") + div .v-icon');
    await page.waitForSelector('div[role="dialog"] .v-list', { timeout: 10000 });
    // Check each product from job.materialTypes; fallback to derived from products.
    const materials = job.materialTypes || job.products || [];
    for (const m of materials) {
      // Convert to NSD material label mapping
      const label = {
        CF2: 'CF2',
        CF4: 'CF4',
        CF6: 'CF6',
        CF8: 'CF8',
        CF8i: 'CF8i',
        Reline: 'Reline',
        Renu: 'Renu',
        Specialty: 'Specialty',
        'Non-PVC': 'Non-PVC',
        Other: 'Other'
      }[m] || m;
      await page.click(`div[role="dialog"] .v-list-item:has-text("${label}") .v-input--selection-controls__ripple`);
    }
    await page.click('div[role="dialog"] button:has-text("SUBMIT")');
    // Wait for BOM to appear in table; capture BOM number from toast or row.
    await page.waitForSelector('div.v-snackbar', { timeout: 30000 });
    const toast = await page.textContent('div.v-snackbar');
    const bomMatch = toast && toast.match(/(\d{5}\d+-[A-Z0-9]{2})/);
    const bomNumber = bomMatch ? bomMatch[1] : null;
    return bomNumber;
  }

  /** Update the opportunity with the generated BOM number. */
  async function addBomToOpportunity(page, bomNumber) {
    // Navigate back to opportunity (assuming still on details page)
    // Find BOM # field and set value
    await page.fill('input[name="BOM Number"]', bomNumber);
    await page.click('button:has-text("SAVE")');
    await page.waitForTimeout(3000);
  }

  const maxContext = await newContext(maxState);
  const maxPage = await maxContext.newPage();
  if (!fs.existsSync(maxState)) {
    await loginMaximizer(maxPage);
  }
  await openCompany(maxPage, job.companyName);
  await createOpportunity(maxPage, job);
  await maxContext.storageState({ path: maxState });

  const nsdContext = await newContext(nsdState);
  const nsdPage = await nsdContext.newPage();
  if (!fs.existsSync(nsdState)) {
    await loginNSD(nsdPage);
  }
  const bomNumber = await createBOM(nsdPage, job);
  if (bomNumber) {
    console.log('BOM created:', bomNumber);
  } else {
    console.warn('Failed to capture BOM number');
  }

  // Update Maximizer with BOM #
  // Reopen opportunity if necessary; we assume the same page is open.
  await addBomToOpportunity(maxPage, bomNumber);

  await nsdContext.storageState({ path: nsdState });
  await maxContext.storageState({ path: maxState });

  // TODO: Implement Trello integration by launching a new tab and
  // creating a card on the "Nuform" board if job.trelloEnabled is true.

  // TODO: Download System Estimate PDF from NSD and save to
  // Desktop/job.estimateNumber folder.

  await browser.close();
}

if (require.main === module) {
  if (process.argv.length < 3) {
    console.error('Usage: node create_estimate.js <job-json>');
    process.exit(1);
  }
  const job = JSON.parse(process.argv[2]);
  run(job).catch(err => {
    console.error(err);
    process.exit(1);
  });
}