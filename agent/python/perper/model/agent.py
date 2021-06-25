from async_locals import *
from perper.protocol.cache_service_extensions import call_read_result, call_check_result

class Agent:
    def __init__(self, raw_agent):
        self.raw_agent = raw_agent

    async def call_function(self, delegate, parameters):
        call = get_cache_service().generate_name(delegate)
        get_cache_service().call_create(call, self.raw_agent, self.raw_agent.instance, delegate, get_agent(), get_instance(), parameters)

        (k, n) = await get_notification_service().get_call_result_notification(call)
        get_notification_service().consume_notification(k)
        result = call_read_result(get_cache_service(), call)
        return result

    async def call_action(self, delegate, parameters):
        call = get_cache_service().generate_name(delegate)
        get_cache_service().call_create(call, self.raw_agent, self.raw_agent.instance, delegate, get_agent(), get_instance(), parameters)

        (k, n) = await get_notification_service().get_call_result_notification(call)
        get_notification_service().consume_notification(k)
        call_check_result(get_cache_service(), call)
