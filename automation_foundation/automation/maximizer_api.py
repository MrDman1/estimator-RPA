
from api.clients.maximizer import MaximizerClient

def create_opportunity_api(job)->dict:
    # TODO: map JobSpec -> Maximizer Opportunity payload
    # Using a stub until you wire the Octopus API.
    return {"opportunity_key": "OPP-PLACEHOLDER", "abentry_key": job.customer.max_abentry_key}
