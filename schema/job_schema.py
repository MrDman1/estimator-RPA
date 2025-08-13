from __future__ import annotations
from typing import Literal
from pydantic import BaseModel, Field
import orjson, os, pathlib

def _dumps(v, *, default):
    return orjson.dumps(v, default=default)

def _loads(b):
    return orjson.loads(b)

class OrjsonModel(BaseModel):
    model_config = dict(ser_json_dumps=_dumps, ser_json_loads=_loads)

class Opening(OrjsonModel):
    type: Literal["door","dock","window","custom"]
    width_ft: float
    height_ft: float
    wall_index: int
    offset_ft: float

class Segment(OrjsonModel):
    length_ft: float
    angle_deg: float

class JobInfo(OrjsonModel):
    id: str
    client: str
    site_address: str
    estimator: str
    estimate_type: Literal["NSD","Excel"]

class Building(OrjsonModel):
    type: str
    material: str
    height_ft: float

class Footprint(OrjsonModel):
    origin: tuple[float, float] = (0.0, 0.0)
    segments: list[Segment] = Field(default_factory=list)
    openings: list[Opening] = Field(default_factory=list)

class Scope(OrjsonModel):
    description: str
    exclusions: list[str] = Field(default_factory=list)
    notes: str | None = None

class Pricing(OrjsonModel):
    labor_rate: float | None = None
    markup_pct: float | None = None
    tax_pct: float | None = None

class Outputs(OrjsonModel):
    project_root: str
    nsd_profile: str | None = None
    excel_template: str | None = None

class JobBundle(OrjsonModel):
    job: JobInfo
    building: Building
    footprint: Footprint
    scope: Scope
    pricing: Pricing | None = None
    outputs: Outputs

    def to_json(self, path: str) -> str:
        p = pathlib.Path(path)
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(self.model_dump_json().encode("utf-8"))
        return str(p)

    @classmethod
    def from_json(cls, path: str) -> "JobBundle":
        p = pathlib.Path(path)
        data = orjson.loads(p.read_bytes())
        return cls.model_validate(data)

if __name__ == "__main__":
    demo = JobBundle(
        job=JobInfo(id="J-DEMO", client="Acme", site_address="123 Road", estimator="Damian", estimate_type="NSD"),
        building=Building(type="Warehouse", material="Precast", height_ft=26),
        footprint=Footprint(),
        scope=Scope(description="Demo", exclusions=["Permits"]),
        outputs=Outputs(project_root="./runs/J-DEMO")
    )
    out = demo.to_json("./runs/J-DEMO/job.json")
    print("Wrote", out)
