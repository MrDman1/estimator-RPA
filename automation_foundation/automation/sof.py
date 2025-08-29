
from jinja2 import Environment, FileSystemLoader
from weasyprint import HTML

env = Environment(loader=FileSystemLoader("templates"))

def generate_sof_pdf(job, out_path:str):
    tmpl = env.get_template("sof.html.j2")
    html = tmpl.render(job=job.model_dump())
    HTML(string=html).write_pdf(out_path)
    return out_path
