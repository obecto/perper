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

import os
import json
import asyncio
import threading

# TODO: Move it under functions/python
class Perper():
    def __init__(self):
        self.serializer = Serializer()
        self.ignite = PerperThinClient()
        self.ignite.compact_footer = True
        self.ignite.connect('localhost', 10800)

        config = PerperConfig()
        self.fs = FabricService(self.ignite, config)
        self.fs.start()

        self.instance = PerperInstanceData(self.ignite, self.serializer)
        self.state = State(self.instance, self.ignite, self.serializer)
        self.context = Context(self.instance, self.fs, self.state, self.ignite)


    async def functions(self, functions):
        # TODO: Implement Call Triggers
        async for (k, n) in self.fs.get_notifications():
            print(n)
            incoming_type = n.__class__.__name__
            if incoming_type == 'StreamTriggerNotification':
                self.fs.consume_notification(k)
                streams_cache = self.ignite.get_cache("streams")
                stream_data = streams_cache.get(n.stream)
                parameter_data = stream_data.parameters

                if n.delegate in functions:
                    result = await asyncio.create_task(functions[n.delegate](self, *parameter_data.parameters))
                    stream_data.result = result
                    stream_data.finished = True
                    streams_cache.replace(n.stream, stream_data)

            elif incoming_type == 'CallTriggerNotification':
                self.fs.consume_notification(k)
                calls_cache = self.ignite.get_cache("calls")
                call_data = calls_cache.get(n.call)
                parameter_data = call_data.parameters

                if n.delegate in functions:
                    result = await asyncio.create_task(functions[n.delegate](self, *parameter_data.parameters))
                    call_data.result = result
                    call_data.finished = True
                    calls_cache.replace(n.call, call_data)
