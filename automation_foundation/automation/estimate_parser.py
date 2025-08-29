
import pdfplumber, re

def parse_estimate_pdf(path:str)->dict:
    with pdfplumber.open(path) as pdf:
        text = "\n".join(page.extract_text() or "" for page in pdf.pages)
    total = None
    m = re.search(r"Total\s*\$?([\d,]+\.\d{2})", text, re.I)
    if m: total = float(m.group(1).replace(",",""))
    # TODO: parse estimate number, taxes, line items as needed
    return {"estimate_no":"EST-PLACEHOLDER", "total": total, "raw_len": len(text)}
