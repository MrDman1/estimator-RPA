
from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    env: str = "dev"
    allowed_origins: str = "http://localhost:3000,http://localhost:8000"
    redis_url: str = "redis://localhost:6379/0"
    rq_queue: str = "automation"
    filestore_root: str = "./artifacts"
    # Maximizer
    max_base_url: str = "https://api.maximizer.com"
    max_auth_mode: str = "PAT"
    max_pat: str | None = None
    max_vendor_id: str | None = None
    max_app_key: str | None = None
    max_tenant: str | None = None
    # NSD
    nsd_url: str = "https://nsd.example.com/login"
    nsd_user: str | None = None
    nsd_pass: str | None = None
    # Trello
    trello_key: str | None = None
    trello_token: str | None = None
    trello_list_id: str | None = None
    # Email
    smtp_host: str | None = None
    smtp_port: int = 587
    smtp_user: str | None = None
    smtp_pass: str | None = None
    smtp_from: str = "Estimating <estimating@example.com>"

    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8",
                                      env_prefix="", case_sensitive=False)

settings = Settings()
