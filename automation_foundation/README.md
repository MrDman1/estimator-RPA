
# Automation Foundation (App + Worker)

This repository is a **foundation** for your estimator automation. It provides:
- A **FastAPI** app for search/hydration from Maximizer and a single **/automate** entrypoint.
- A **Redis-backed RQ worker** that runs the multi-step pipeline.
- **Playwright**-based UI automation (for NSD) and API clients (for Maximizer, Trello, email).
- **Artifacts & audit logs** per job, with idempotent `job_id` handling.
- **Templates** for SOF PDF and customer email (Jinja2).

> This is a scaffold with working stubs. Fill in the TODOs noted in comments.
> Run locally with Docker (recommended) or directly via Python.

## Stack
- Python 3.11
- FastAPI + Uvicorn
- Redis + RQ (jobs queue)
- Playwright (Chromium) for NSD UI automation
- Jinja2 + WeasyPrint (HTML→PDF) for SOF
- Postgres (optional; disabled by default — you can enable later)
- Trello/email via simple clients

## Quick start (Docker)
1. Copy `.env.example` to `.env` and fill secrets.
2. Install Playwright deps (container does this) and start:
   ```bash
   docker compose up --build
   ```
3. Open API docs: http://localhost:8000/docs
4. Kick an automation:
   ```bash
   curl -X POST http://localhost:8000/automate -H "Content-Type: application/json" -d @sample-job.json
   ```

## Running without Docker (dev)
```bash
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
python -m playwright install
# Terminal 1: API
uvicorn api.main:app --reload --port 8000
# Terminal 2: Worker
rq worker -u redis://localhost:6379 automation
```

## Directory
- `api/` FastAPI app
- `worker/` RQ worker and pipeline
- `automation/` UI/API automation steps
- `templates/` Jinja templates (SOF, email)
- `artifacts/` Job outputs (created at runtime)

## What you must configure next
- Maximizer Octopus API (PAT/OAuth) in `api/clients/maximizer.py`
- NSD UI selectors & flow in `automation/nsd_ui.py`
- SOF template fields in `templates/sof.html.j2`
- Trello API keys and list IDs in `api/clients/trello.py`
- Email sender in `api/clients/emailer.py`
- File store root in `.env` (`FILESTORE_ROOT`)

See inline TODOs for specifics.
