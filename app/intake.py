from __future__ import annotations

import json
import os
from datetime import datetime
from pathlib import Path

from dotenv import load_dotenv
from flask import Flask, redirect, render_template, request, url_for

from schema.job_schema import (
    Building,
    Footprint,
    JobBundle,
    JobInfo,
    Opening,
    Outputs,
    Scope,
    Segment,
)

# load environment variables
load_dotenv()
BASE_RUN_DIR = os.getenv("BASE_RUN_DIR", "./runs")

app = Flask(__name__)


def make_job_id(client: str) -> str:
    """Generate job id J-YYYYMMDD-### using a counter file."""
    today = datetime.now().strftime("%Y%m%d")
    counter_file = Path(BASE_RUN_DIR) / ".counter"
    counter_file.parent.mkdir(parents=True, exist_ok=True)
    try:
        counter = int(counter_file.read_text())
    except FileNotFoundError:
        counter = 0
    counter += 1
    counter_file.write_text(str(counter))
    return f"J-{today}-{counter:03d}"


@app.get("/")
def index():
    """Render the intake form."""
    return render_template("index.html")


@app.post("/save")
def save():
    form = request.form
    client = form.get("client", "").strip()
    site_address = form.get("site_address", "").strip()
    estimator = form.get("estimator", "").strip()
    estimate_type = form.get("estimate_type", "NSD")

    if not client or not site_address:
        return "Client and site address required", 400

    building_type = form.get("building_type", "")
    building_material = form.get("building_material", "")
    try:
        building_height = float(form.get("building_height", 0))
    except ValueError:
        return "Invalid building height", 400

    description = form.get("scope_description", "")
    exclusions_csv = form.get("exclusions", "")
    exclusions = [e.strip() for e in exclusions_csv.split(",") if e.strip()]

    segments_data = json.loads(form.get("segments_json", "[]"))
    openings_data = json.loads(form.get("openings_json", "[]"))

    segments = [Segment.model_validate(s) for s in segments_data]
    openings = [Opening.model_validate(o) for o in openings_data]

    job_id = make_job_id(client)
    run_dir = Path(BASE_RUN_DIR) / job_id

    bundle = JobBundle(
        job=JobInfo(
            id=job_id,
            client=client,
            site_address=site_address,
            estimator=estimator,
            estimate_type=estimate_type,
        ),
        building=Building(type=building_type, material=building_material, height_ft=building_height),
        footprint=Footprint(segments=segments, openings=openings),
        scope=Scope(description=description, exclusions=exclusions),
        outputs=Outputs(project_root=str(run_dir)),
    )

    path = bundle.to_json(str(run_dir / "job.json"))
    return redirect(url_for("done", job_id=job_id, path=path))


@app.get("/done/<job_id>")
def done(job_id: str):
    run_dir = Path(BASE_RUN_DIR) / job_id
    path = run_dir / "job.json"
    return render_template("done.html", job_id=job_id, path=str(path))


if __name__ == "__main__":
    app.run(debug=True)
