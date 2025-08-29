
from fastapi import FastAPI, Depends
from fastapi.middleware.cors import CORSMiddleware
from .settings import settings
from .models import JobSpec
from redis import Redis
from rq import Queue
import os, json, pathlib

app = FastAPI(title="Estimator Automation API", version="0.1.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.allowed_origins.split(","),
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

def get_queue():
    redis = Redis.from_url(settings.redis_url)
    return Queue(settings.rq_queue, connection=redis)

@app.get("/health")
def health():
    return {"ok": True}

@app.get("/lookup/company")
def lookup_company(q: str, limit: int = 10):
    # TODO: Implement Maximizer AbEntry Read filter
    # For now, return a stubbed result
    return [{
        "name": "Acme Builders Ltd.",
        "city": "Toronto",
        "phone": "555-0100",
        "max_abentry_key": "AB-12345"
    }]

@app.get("/lookup/abentry/{key}")
def hydrate_abentry(key: str):
    # TODO: Fetch full AbEntry + UDFs from Maximizer
    return {
        "max_abentry_key": key,
        "company_name": "Acme Builders Ltd.",
        "primary_contact": {"name": "Jane Li", "email": "jane@acme.com"},
        "billing_address": {"line1": "1 Main St", "city": "Toronto"},
        "discount_policy": {"type":"tiered","tier":"Gold","percent":7.5},
        "udf": {"TaxExempt": False, "NSD_CustomerNo": "NSD-9087"}
    }

@app.post("/automate")
def automate(job: JobSpec, q: Queue = Depends(get_queue)):
    # Persist inputs for audit/idempotency
    base = pathlib.Path("artifacts") / f"job-{job.job_id}" / "inputs"
    base.mkdir(parents=True, exist_ok=True)
    (base / "jobspec.json").write_text(job.model_dump_json(indent=2), encoding="utf-8")

    # Enqueue pipeline
    job_meta = {"job_id": job.job_id}
    rq_job = q.enqueue("worker.worker.run_pipeline", job.model_dump(), job_meta=job_meta)
    return {"enqueued": True, "rq_job_id": rq_job.id}
