import time
import threading

import fabric_pb2, fabric_pb2_grpc
from notifications import NotificationKeyLong, NotificationKeyString

from perper.cache.notifications import (
    StreamItemNotification,
    StreamTriggerNotification,
    CallResultNotification,
    CallTriggerNotification,
    NotificationKeyLong,
    NotificationKeyString
)

class NotificationService:
    def __init__(self, ignite, grpc_channel, agent):
        self.agent = agent
        self._grpc_channel = grpc_channel
        self.ignite = ignite
        self.notifications_cache = self.ignite.get_or_create_cache(f'{agent}-$notifications')

        self.ignite.register_binary_type(StreamItemNotification)
        self.ignite.register_binary_type(StreamTriggerNotification)
        self.ignite.register_binary_type(CallResultNotification)
        self.ignite.register_binary_type(CallTriggerNotification)

    def consume_notification(self, key):
        return self.notifications_cache.remove(key)

    def get_notification_key(self, notification: fabric_pb2.Notification): # TODO: Implement properly
        if notification.stringAffinity not in (None, ''):
            return NotificationKeyString(
                key=notification.notificationKey, affinity=notification.stringAffinity
            )

        if notification.intAffinity not in (None, 0):
            return NotificationKeyLong(
                key=notification.notificationKey, affinity=notification.intAffinity
            )

        raise Exception('Invalid grpc notification.')

    def start(self):
        self.running = True
        thread = threading.Thread(target=self.run, daemon=True, args=())
        thread.start()
    
    def stop(self):
        self.running = False

    def run(self):
        grpc_stub = fabric_pb2_grpc.FabricStub(self._grpc_channel)
        for notification in grpc_stub.Notifications(fabric_pb2.NotificationFilter(agent = self.agent)):

            key = self.get_notification_key(notification)
            item = self.notifications_cache.get(key)

            print(key, item)

    async def get_call_result_notification(self, call):
        grpc_stub = fabric_pb2_grpc.FabricStub(self._grpc_channel)
        notification = grpc_stub.CallResultNotification(
            fabric_pb2.CallNotificationFilter(
                agent = self.agent,
                call = call
            )
        )

        key = self.get_notification_key(notification)
        item = self.notifications_cache.get(key)

        return (key, item)
    
    def consume_notification(self, key):
        return self.notifications_cache.remove_key(key)