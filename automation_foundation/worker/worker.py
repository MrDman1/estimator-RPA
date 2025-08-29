
from api.models import JobSpec
from automation.nsd_ui import run_nsd_flow
from automation.maximizer_api import create_opportunity_api
from automation.sof import generate_sof_pdf
from automation.estimate_parser import parse_estimate_pdf
from api.clients.storage import job_dir, canonical_paths, save_artifact
from api.clients.trello import TrelloClient
from api.clients.emailer import render_email, send_email
import json, pathlib

def run_pipeline(job_dict: dict):
    job = JobSpec(**job_dict)
    base = job_dir(job.job_id)
    logs = base / "logs"
    outputs = base / "outputs"
    logs.mkdir(parents=True, exist_ok=True)
    outputs.mkdir(parents=True, exist_ok=True)

    # 1) Create Opportunity (API-first)
    opp = create_opportunity_api(job)
    (logs / "opportunity.json").write_text(json.dumps(opp, indent=2), encoding="utf-8")

    # 2) Generate SOF PDF
    sof_pdf = outputs / "SOF.pdf"
    generate_sof_pdf(job, str(sof_pdf))

    # 3) NSD UI: upload and print estimate -> outputs/Estimate.pdf
    estimate_pdf = outputs / "Estimate.pdf"
    nsd_meta = run_nsd_flow(job, str(sof_pdf), str(estimate_pdf))
    (logs / "nsd_meta.json").write_text(json.dumps(nsd_meta, indent=2), encoding="utf-8")

    # 4) Parse estimate; collect totals
    parsed = parse_estimate_pdf(str(estimate_pdf))
    (outputs / "estimate_meta.json").write_text(json.dumps(parsed, indent=2), encoding="utf-8")

    # 5) Store files canonically (optional — adjust paths)
    paths = canonical_paths(job.job_id, job.customer.name, parsed.get("estimate_no",""))
    save_artifact(str(sof_pdf), paths["sof"])
    save_artifact(str(estimate_pdf), paths["estimate"])

    # 6) Trello (optional)
    # trello = TrelloClient()
    # card_id = trello.create_card(f"Estimate {parsed.get('estimate_no','')} — {job.customer.name}", "Automated estimate")
    # (logs / "trello_card.txt").write_text(card_id, encoding="utf-8")

    # 7) Email draft/send (optional, set to draft logic if you use Gmail API)
    html = render_email({
        "customer": job.customer.name,
        "total": parsed.get("total"),
        "estimate_no": parsed.get("estimate_no",""),
    })
    # send_email(job.customer.email or "ops@example.com", f"Estimate {parsed.get('estimate_no','')}", html, [str(estimate_pdf)])

    return {"ok": True, "opportunity": opp, "estimate": parsed}
