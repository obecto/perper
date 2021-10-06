import time
import asyncio
import threading
import queue
import grpc

from typing import Generator
from collections import OrderedDict

from .proto import fabric_pb2
from .proto import fabric_pb2_grpc

from .notifications import (
    StreamItemNotification,
    StreamTriggerNotification,
    CallResultNotification,
    CallTriggerNotification,
    NotificationKeyLong,
    NotificationKeyString
)

class NotificationService:
    CALL = object()
    CALL_RESULT = object()
    STREAM = object()

    def __init__(self, ignite, address, agent, instance=None):
        self.agent = agent
        self.instance = instance
        self.address = address
        self.ignite = ignite
        self.channels = {}
        self.running = False
        self.listening = False

    def consume_notification(self, key):
        return self.notifications_cache.remove(key)

    def get_notification_key(self, notification: fabric_pb2.Notification):
        if notification.stringAffinity not in (None, ''):
            return NotificationKeyString(
                key=notification.notificationKey, affinity=notification.stringAffinity
            )

        if notification.intAffinity not in (None, 0):
            return NotificationKeyLong(
                key=notification.notificationKey, affinity=notification.intAffinity
            )

        raise Exception('Invalid grpc notification.')

    async def start(self):
        self.loop = asyncio.get_running_loop()
        self._grpc_channel = grpc.aio.insecure_channel(self.address)

        grpc_stub = fabric_pb2_grpc.FabricStub(self._grpc_channel)
        notifications = grpc_stub.Notifications(fabric_pb2.NotificationFilter(agent = self.agent, instance = (self.instance or "")))
        await notifications.wait_for_connection()

        self.notifications_cache = self.ignite.get_or_create_cache(f'{self.agent}-$notifications')
        self.ignite.register_binary_type(StreamItemNotification)
        self.ignite.register_binary_type(StreamTriggerNotification)
        self.ignite.register_binary_type(CallResultNotification)
        self.ignite.register_binary_type(CallTriggerNotification)

        self.background_task = asyncio.create_task(self.run(notifications))

    async def stop(self):
        await self._grpc_channel.close()
        await self.background_task

    def get_channel(self, channel):
        if channel not in self.channels:
            self.channels[channel] = asyncio.Queue()

        return self.channels[channel]

    def write_channel_value(self, channel, value):
        asyncio.run_coroutine_threadsafe(self.get_channel(channel).put(value), self.loop)

    async def run(self, notifications):
        async for notification in notifications:
            key = self.get_notification_key(notification)
            item = self.notifications_cache.get(key)
            instance_class = type(item).__name__

            if instance_class == 'StreamItemNotification':
                self.write_channel_value((item.stream, item.parameter), (key, item))

            if instance_class == 'StreamTriggerNotification':
                self.write_channel_value((NotificationService.STREAM, item.delegate), (key, item))

            if instance_class == 'CallTriggerNotification':
                self.write_channel_value((NotificationService.CALL, item.delegate), (key, item))

            if instance_class == 'CallResultNotification':
                self.write_channel_value((NotificationService.CALL_RESULT, item.call), (key, item))

    async def get_notifications(self, instance, parameter) -> Generator:
        channel = self.get_channel((instance, parameter))
        while True:
            item = await channel.get()
            yield item
            channel.task_done()

    async def get_notification(self, instance, parameter):
        channel = self.get_channel((instance, parameter))
        item = await channel.get()
        channel.task_done()
        return item

    async def get_call_result_notification(self, call):
        return await self.get_notification(NotificationService.CALL_RESULT, call)  # HACK: Workaround race condition in CallResultNotification
        #grpc_stub = fabric_pb2_grpc.FabricStub(self._grpc_channel)
        #notification = await grpc_stub.CallResultNotification(
            #fabric_pb2.CallNotificationFilter(
                #agent = self.agent,
                #call = call
            #)
        #)

        #key = self.get_notification_key(notification)
        #item = self.notifications_cache.get(key)

        #return (key, item)

    def consume_notification(self, key):
        return self.notifications_cache.remove_key(key)
