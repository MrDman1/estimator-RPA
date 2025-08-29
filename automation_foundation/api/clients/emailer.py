
import smtplib
from email.message import EmailMessage
from ..settings import settings
from jinja2 import Environment, FileSystemLoader

env = Environment(loader=FileSystemLoader("templates"))

def render_email(context:dict)->str:
    tmpl = env.get_template("email.html.j2")
    return tmpl.render(**context)

def send_email(to:str, subject:str, html:str, attachments:list[str]|None=None):
    msg = EmailMessage()
    msg["From"] = settings.smtp_from
    msg["To"] = to
    msg["Subject"] = subject
    msg.set_content("HTML email required. Please view in an HTML-capable client.")
    msg.add_alternative(html, subtype="html")
    for path in attachments or []:
        with open(path, "rb") as f:
            data = f.read()
        msg.add_attachment(data, maintype="application", subtype="pdf", filename=path.split("/")[-1])

    with smtplib.SMTP(settings.smtp_host, settings.smtp_port) as s:
        s.starttls()
        if settings.smtp_user:
            s.login(settings.smtp_user, settings.smtp_pass)
        s.send_message(msg)
