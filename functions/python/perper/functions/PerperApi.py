from pyignite import Client
from perper.cache.perper_instance_data import PerperInstanceData
from perper.services.fabric_service import FabricService
from perper.cache.stream_data import StreamData
from perper.services.perper_config import PerperConfig
from perper.model.state import State
from perper.model.context import Context
from perper.services.serializer import Serializer
from perper.utils.perper_thin_client import PerperThinClient
from perper.cache.notifications import StreamTriggerNotification


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
        async for (k, n) in self.fs.get_notifications():
            if isinstance(n, StreamTriggerNotification):
                streams_cache = self.ignite.get_cache("streams")
                stream_data = streams_cache.get(n.stream)
                parameter_data = stream_data.parameters
                if n.delegate in functions:
                    functions[n.delegate](self, *parameter_data.parameters)

            self.fs.consume_notification(k)
