from pyignite import Client
from perper.cache.perper_instance_data import PerperInstanceData
from perper.services.fabric_service import FabricService
from perper.cache.stream_data import StreamData
from perper.services.perper_config import PerperConfig
from perper.model.state import State
from perper.model.context import Context
from perper.services.serializer import Serializer
from perper.utils.perper_thin_client import PerperThinClient
from perper.cache.notifications import StreamItemNotification, StreamTriggerNotification


# TODO: Move it under functions/python
class Perper():
    def __init__(self):
        serializer = Serializer()
        self.ignite = PerperThinClient()
        self.ignite.compact_footer = True
        self.ignite.connect('localhost', 10800)

        config = PerperConfig()
        self.fs = FabricService(self.ignite, config)
        self.fs.start()

        self.instance = PerperInstanceData(self.ignite, serializer)
        self.state = State(self.instance, self.ignite, serializer)
        self.context = Context(self.instance, self.fs, self.state, self.ignite)


    async def functions(self, functions): 
        import json
        result = []
        async for (k, n) in self.fs.get_notifications():
            incoming_type = n.__class__.__name__
            print(incoming_type)
            if incoming_type == 'StreamTriggerNotification':
                self.fs.consume_notification(k)
                streams_cache = self.ignite.get_cache("streams")
                stream_data = streams_cache.get(n.stream)
                parameter_data = stream_data.parameters
                if n.delegate in functions:
                    functions[n.delegate](self, *parameter_data.parameters)
            elif incoming_type == 'StreamItemNotification':
                stream_cache = self.ignite.get_cache(n.cache)
                item_data = stream_cache.get(n.key)
                if item_data is not None:
                    result.append(json.loads(item_data.json))
                json_obj = json.loads(item_data.json)
                print(json_obj)
                pass

            self.fs.consume_notification(k)
