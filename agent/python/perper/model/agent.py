from async_locals import AsyncLocals
from perper.protocol.cache_service_extensions import call_read_result, call_check_result

class Agent:
    def __init__(self, raw_agent):
        self.raw_agent = raw_agent

    async def call_function(self, delegate, parameters):
        call = AsyncLocals.get_cache_service().generate_name(delegate)
        AsyncLocals.get_cache_service().call_create(call, self.raw_agent, self.raw_agent.instance, delegate, AsyncLocals.get_agent(), AsyncLocals.get_instance(), parameters)

        (k, n) = await AsyncLocals.get_notification_service().get_call_result_notification(call)
        AsyncLocals.get_notification_service().consume_notification(k)
        result = call_read_result(AsyncLocals.get_cache_service(), call)
        return result

    async def call_action(self, delegate, parameters):
        call = AsyncLocals.get_cache_service().generate_name(delegate)
        AsyncLocals.get_cache_service().call_create(call, self.raw_agent, self.raw_agent.instance, delegate, AsyncLocals.get_agent(), AsyncLocals.get_instance(), parameters)

        (k, n) = await AsyncLocals.get_notification_service().get_call_result_notification(call)
        AsyncLocals.get_notification_service().consume_notification(k)
        call_check_result(AsyncLocals.get_cache_service(), call)
