
import os, shutil, json, pathlib
from ..settings import settings
from slugify import slugify

def job_dir(job_id:str)->pathlib.Path:
    base = pathlib.Path("artifacts") / f"job-{job_id}"
    base.mkdir(parents=True, exist_ok=True)
    return base

def canonical_paths(job_id:str, company:str, estimate_no:str|None=None)->dict:
    year = "YYYY"  # TODO: derive from today
    root = pathlib.Path(settings.filestore_root) / year / f"{slugify(company)}-{job_id}"
    sof = root / f"SOF-{job_id}.pdf"
    est = root / (f"Estimate-{estimate_no}.pdf" if estimate_no else f"Estimate-{job_id}.pdf")
    return {"root": root, "sof": sof, "estimate": est}

def save_artifact(src:str, dest:pathlib.Path):
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dest)
    return str(dest)
