import grpc
import fabric_pb2_grpc
import fabric_pb2
from SimpleData import SimpleData
from pyignite.datatypes.prop_codes import *
from AffinityKey import *
from Notification import CallData

from Notification import Notification, StreamItemNotification, StreamTriggerNotification, CallResultNotification, CallTriggerNotification, CallData

from PerperThinClient import PerperThinClient

agent_delegate = "Application"

channel = grpc.insecure_channel('localhost:40400')
stub = fabric_pb2_grpc.FabricStub(channel) # Used to call rpc methods

notification_filter = fabric_pb2.NotificationFilter(agentDelegate=agent_delegate);

print(f"Using notification filter with parameters {notification_filter}");
print("Processing notifications...")

client = PerperThinClient()
client.compact_footer = True
client.connect('localhost', 10800)

client.register_binary_type(NotificationKeyLong, affinity_key_field = "affinity")
client.register_binary_type(NotificationKeyString, affinity_key_field = "affinity")

client.register_binary_type(SimpleData)

calls = client.get_cache("calls")

cache_config = {
    PROP_NAME: f"{agent_delegate}-$notifications",
    PROP_CACHE_KEY_CONFIGURATION: [
        {
            'type_name': 'NotificationKey',
            'affinity_key_field_name': 'affinity'
        }
    ]
}

notifications_cache = client.get_cache(cache_config);

for notification in stub.Notifications(notification_filter):

    item = None
    key = None

    print(notification)

    if not (notification.stringAffinity in (None, "")):
        key = NotificationKeyString(key=notification.notificationKey, affinity=notification.stringAffinity)

    if (notification.intAffinity != 0) :
        key = NotificationKeyLong(key=notification.notificationKey, affinity=notification.intAffinity)

    item = notifications_cache.get(key)
    assert item != None

    if "cache" in vars(item):
        item_cache = client.get_cache(item.cache);
        final_item = item_cache.get(item.key);

        print(f"Retrieved Item: {final_item}");

        notifications_cache.get_and_remove(key);
