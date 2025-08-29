
import requests
from ..settings import settings

class TrelloClient:
    def __init__(self):
        self.key = settings.trello_key
        self.token = settings.trello_token
        self.list_id = settings.trello_list_id
        self.base = "https://api.trello.com/1"

    def create_card(self, name:str, desc:str="")->str:
        params = {"key": self.key, "token": self.token, "idList": self.list_id, "name": name, "desc": desc}
        r = requests.post(f"{self.base}/cards", params=params, timeout=30)
        r.raise_for_status()
        return r.json().get("id")

    def attach_url(self, card_id:str, url:str, name:str=None):
        params = {"key": self.key, "token": self.token, "url": url, "name": name or url}
        r = requests.post(f"{self.base}/cards/{card_id}/attachments", params=params, timeout=30)
        r.raise_for_status()
        return True
