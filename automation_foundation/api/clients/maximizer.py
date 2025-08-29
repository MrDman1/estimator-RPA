
import os, requests
from ..settings import settings

class MaximizerClient:
    def __init__(self):
        self.base = settings.max_base_url.rstrip('/')
        self.session = requests.Session()
        mode = settings.max_auth_mode.upper()
        if mode == "PAT" and settings.max_pat:
            self.session.headers.update({"Authorization": f"Bearer {settings.max_pat}"})
        # TODO: Support OAuth / VendorId+AppKey if needed.

    def read_abentry(self, query:str, limit:int=10):
        # TODO: Build proper Octopus Read body for AbEntry filters.
        # Return normalized list for autocomplete.
        raise NotImplementedError("Implement Octopus AbEntry Read")

    def get_abentry(self, key:str):
        # TODO: Read a single AbEntry by key and include UDFs.
        raise NotImplementedError("Implement Octopus AbEntry ReadByKey/UDF")

    def create_opportunity(self, abentry_key:str, payload:dict)->dict:
        # TODO: POST Create Opportunity (Octopus API). Return opportunity key/url.
        raise NotImplementedError("Implement Opportunity Create")
