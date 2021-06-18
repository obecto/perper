import grpc
from thin_client import PerperIgniteClient
from notification_service import NotificationService

ignite = PerperIgniteClient()
with ignite.connect('127.0.0.1', 10800):
    channel = grpc.insecure_channel('127.0.0.1:40400')
    notification_service = NotificationService(ignite, channel, 'test_agent')

    print(notification_service)
