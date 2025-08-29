
from playwright.sync_api import sync_playwright
from api.settings import settings

def run_nsd_flow(job, sof_pdf_path:str, out_pdf_path:str)->dict:
    # TODO: Implement the real NSD UI sequence.
    # This stub logs in and pretends to generate an estimate PDF.
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        ctx = browser.new_context(accept_downloads=True)
        page = ctx.new_page()
        page.goto(settings.nsd_url)
        # page.fill("#username", settings.nsd_user)
        # page.fill("#password", settings.nsd_pass)
        # page.click("button[type=submit]")
        # page.wait_for_selector("text=Dashboard")

        # Upload SOF, navigate to print, intercept download...
        # For now, just copy the SOF as a placeholder for output to prove the pipeline.
        import shutil
        shutil.copy2(sof_pdf_path, out_pdf_path)

        ctx.close(); browser.close()
    return {"estimate_no": "EST-PLACEHOLDER"}
