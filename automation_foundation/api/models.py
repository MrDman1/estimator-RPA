
from pydantic import BaseModel, Field
from typing import Optional, List, Literal, Dict, Any

class Contact(BaseModel):
    name: str
    email: Optional[str] = None
    phone: Optional[str] = None

class Attachment(BaseModel):
    kind: Literal["SOF","Plans","Photos"]
    path: str

class LineItem(BaseModel):
    room: str
    opening: str
    size: str
    qty: int = 1
    spec: str
    price: Optional[float] = None

class Customer(BaseModel):
    name: str
    email: Optional[str] = None
    phone: Optional[str] = None
    address: Optional[Dict[str, Any]] = None
    max_abentry_key: Optional[str] = None
    udm: Optional[Dict[str, Any]] = None  # user-defined metadata (discounts, etc.)

class Project(BaseModel):
    site: str
    due_date: Optional[str] = None
    source: Literal["manual","uploaded"] = "manual"

class JobSpec(BaseModel):
    job_id: str = Field(..., description="Idempotency key")
    customer: Customer
    project: Project
    line_items: List[LineItem]
    attachments: Optional[List[Attachment]] = None
    tags: Optional[List[str]] = None
