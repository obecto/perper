import asyncio
import os
import pyignite

from perper.services.fabric_service import FabricService
from perper.utils.perper_thin_client import PerperThinClient
from perper.services.perper_config import PerperConfig
from perper.cache import CallData
from perper.cache.notifications import NotificationKeyLong, NotificationKeyString, StreamItemNotification, StreamTriggerNotification, \
                                       CallResultNotification, CallTriggerNotification

test_agent_delegate = "Application2"
os.environ["PERPER_AGENT_NAME"] = test_agent_delegate
os.environ["ROOT_PERPER_AGENT"] = test_agent_delegate

ignite = PerperThinClient()

ignite.compact_footer = True
ignite.connect('localhost', 10800)

def test_stream_creation():
    streams_cache = ignite.get_cache("streams")
    # put StreamData into the streams_cache

async def test_call_notification(_agent_name, _agent_delegate, _call_delegate, _parameters):
    '''
        Sends the CallData correctly, however needs streams implemented
        in order to be tested correctly
    '''
    caller_instance_name = "test_instance" # This is from PerperInstanceData.Instance

    config = PerperConfig()
    fs = FabricService(ignite, config)
    fs.start()

    calls_cache = ignite.get_cache("calls")
    call_name = "TestStream--UUID" # GenerateStreamName from Context is actually called here.

    # TODO:
    # The stream has to be created beforehand
    instance_cache = ignite.get_cache("streams")
    instance_data = instance_cache.get(caller_instance_name)
    # somehow extract the parameters binary from here and
    # use it in the call data as "parameters" if needed

    for arg in [call_name, _agent_name, test_agent_delegate, _agent_delegate, _call_delegate, fs.agent_delegate, caller_instance_name]:
        print(f"{arg} \n")

    calls_cache.put(call_name, CallData(
        agent = test_agent_delegate,
        agentdelegate = test_agent_delegate,
        delegate = test_agent_delegate,
        calleragentdelegate = fs.agent_delegate,
        caller = caller_instance_name,
        finished = True, # Has to be set to True by the thing that computes stuff
        localtodata = True,
        error = "",
        parameters = None
    ))

    print(test_agent_delegate, fs.agent_delegate, call_name)

    key, notification = await fs.get_call_notification(call_name)
    print(notification)
    fs.consume_notification(key)

    call_result = calls_cache.get_and_remove(notification.call)

    if call_result.error != "":
        raise Exception("An error has occurred " + call_result.error)

    print("Done")


async def test_consumption():

    config = PerperConfig()
    fs = FabricService(ignite, config)
    fs.start()
    print('Started fabric service')

    async for (k, n) in fs.get_notifications():
        if isinstance(n, StreamItemNotification):
            cache = ignite.get_cache(n.cache)
            item = cache.get(n.key)
            print(f"Item received from [{n.cache}]: " + str(item))
            fs.consume_notification(k)
        else:
            print("Something else: " + str(n.__class__))


if __name__ == "__main__":
    # Python 3.6
    # loop = asyncio.get_event_loop()
    # result = loop.run_until_complete(test_call_notification("Application2", "Application2", "Application", None))

    # Python 3.7+
    asyncio.run(test_consumption())
    # asyncio.run(test_call_notification("Application2", "Application2", "Application", None))