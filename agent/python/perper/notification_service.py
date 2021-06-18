import fabric_pb2, fabric_pb2_grpc

from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import LongObject, String

class NotificationService:
    def __init__(self, ignite, grpc_channel, agent):
        self.agent = agent
        self.notifications_cache = ignite.get_or_create_cache('{agent}-$notifications')
        self._grpc_channel = grpc_channel

    def consume_notification(self, key):
        return self.notifications_cache.remove(key)

    def get_notification_key(self, notification: fabric_pb2.Notification): # TODO: Implement properly

        class NotificationKeyString(
            metaclass=GenericObjectMeta,
            type_name='NotificationKeyString',
            schema=OrderedDict(
                [
                    ('affinity', String),
                    ('key', LongObject),
                ]
            ),
        ):
            pass


        class NotificationKeyLong(
            metaclass=GenericObjectMeta,
            type_name='NotificationKeyLong',
            schema=OrderedDict(
                [
                    ('affinity', LongObject),
                    ('key', LongObject),
                ]
            ),
        ):
            pass

        if notification.stringAffinity not in (None, ''):
            return NotificationKeyString(
                key=notification.notificationKey, affinity=notification.stringAffinity
            )

        if notification.intAffinity not in (None, 0):
            return NotificationKeyLong(
                key=notification.notificationKey, affinity=notification.intAffinity
            )

        raise Exception('Invalid grpc notification.')

    async def get_call_result_notification(self, call):
        grpc_stub = fabric_pb2_grpc.FabricStub(self._grpc_channel)
        notification = grpc_stub.CallResultNotification(
            fabric_pb2.CallNotificationFilter(
                Agent = self.agent,
                Call = call
            )
        )

        key = self.get_notification_key(notification)
        item = self.notifications_cache.get(key)

        return (key, item)