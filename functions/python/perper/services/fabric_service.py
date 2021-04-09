from perper.utils import PerperThinClient
from perper.services import PerperConfig

from perper.cache.notifications import NotificationKeyLong, NotificationKeyString
from perper.cache.notifications import (
    StreamItemNotification,
    StreamTriggerNotification,
    CallResultNotification,
    CallTriggerNotification,
)
from perper.cache.notifications import fabric_pb2_grpc
from perper.cache.notifications import fabric_pb2
from perper.cache import CallData

from pyignite.datatypes.prop_codes import *
from logging import Logger
from typing import *

import logging
import grpc
import os


class FabricService(object):
    def __init__(
        self,
        ignite: PerperThinClient,
        config: PerperConfig,
        logger: Logger = logging.getLogger("fabric_service"),
    ):
        """
        FabricService is responsible for listening for perper fabric
        notifications and rerouting them accordingly. Provides
        utility methods for consuming and receiving notifications

        """
        self._ignite = ignite
        self._logger = logger

        self._logger.info("Initializing Python Perper Fabric Service...")

        self.agent_delegate = os.getenv("PERPER_AGENT_NAME")
        if self.agent_delegate is None:
            self.agent_delegate = self.get_agent_delegate_from_path()

        self._logger.info(f"Fabric agent delegate: {self.agent_delegate}")

        root_agent = os.getenv("PERPER_ROOT_AGENT")
        if root_agent is None:
            root_agent = ""

        self.is_initial_agent = root_agent == self.agent_delegate

        # TODO: Maybe abstract this into PerperConfig()
        cache_config = {
            PROP_NAME: f"{self.agent_delegate}-$notifications",
            PROP_CACHE_KEY_CONFIGURATION: [
                {"type_name": "NotificationKey", "affinity_key_field_name": "affinity"}
            ],
        }

        self._notifications_cache = self._ignite.get_cache(cache_config)

        self._ignite.register_binary_type(
            NotificationKeyLong, affinity_key_field="affinity"
        )
        self._ignite.register_binary_type(
            NotificationKeyString, affinity_key_field="affinity"
        )
        self._ignite.register_binary_type(StreamItemNotification)
        self._ignite.register_binary_type(StreamTriggerNotification)
        self._ignite.register_binary_type(CallResultNotification)
        self._ignite.register_binary_type(CallTriggerNotification)
        self._ignite.register_binary_type(CallData)

        address = f"{config.fabric_host}:40400"

        self._grpc_channel = grpc.insecure_channel(address)
        self._grpc_stub = None
        self._notification_filter = fabric_pb2.NotificationFilter(
            agentDelegate=self.agent_delegate
        )

    def get_agent_delegate_from_path(self) -> str:
        return os.path.basename(os.getcwd())

    def get_notification_key(self, notification: fabric_pb2.Notification):
        """
        Receives a grpc notification and converts it to a
        registered type known to Apache Ignite, namely:
        NotificationKeyLong or NotificationKeyString
        """
        if notification.stringAffinity not in (None, ""):
            return NotificationKeyString(
                key=notification.notificationKey, affinity=notification.stringAffinity
            )

        if notification.intAffinity not in (None, 0):
            return NotificationKeyLong(
                key=notification.notificationKey, affinity=notification.intAffinity
            )

        raise Exception("Invalid grpc notification.")

    def start(self):
        self._logger.info(f"Started FabricService for agent {self.agent_delegate}")
        try:
            self._setup_grpc()
            self.start_initial_agent()
        except Exception as e:
            self._logger.error(f"A Critical error has occured: {e}")
            raise Exception("TODO: Implement failover")

    def start_initial_agent(self):
        """
        Sends the initial call over to the perper fabric to
        let it know that we want to start an agent (a Perper application)
        """

        if self.is_initial_agent:
            self._logger.info("Calling initial agent")
            _call_delegate = self.agent_delegate
            _agent_name = f"{self.agent_delegate}-$launchAgent"
            _call_name = f"{_call_delegate}-$launchCall"

            calls_cache = self._ignite.get_cache("calls")
            calls_cache.put(
                _call_name,
                CallData(
                    agent=_agent_name,
                    agent_delegate=self.agent_delegate,
                    delegate=_call_delegate,
                    caller_agent_delegate="",
                    caller="",
                    finished=False,
                    local_to_data=False,
                ),
            )

    def _setup_grpc(self):
        """
        Sets up GRPC stub object for calling all grpc methods
        """
        if self._grpc_stub is None:
            self._grpc_stub = fabric_pb2_grpc.FabricStub(
                self._grpc_channel
            )  # Used to call rpc methods
        self._logger.debug("Setting up grpc")
        return self._grpc_stub

    def consume_notification(self, key):
        self._logger.debug(f"Consumed notification {key}.")
        return self._notifications_cache.get_and_remove(key)

    async def get_notifications(self, _instance: str = None) -> Generator:
        """
        Retrieves and returns perper notifications (could be actual data, or
        function calls) corresponding to their type or corresponding to the _instance formal parameter.

        if '_instance' is None, all agent-wide notifications will be returned
        """

        self._logger.info(
            f"Listening for " + ("all streams" if _instance is None else _instance)
        )

        for notification in self._grpc_stub.Notifications(self._notification_filter):

            key = self.get_notification_key(notification)
            item = self._notifications_cache.get(key)

            if item is None:
                self._logger.error(
                    f"FabricService failed to read notification for key: {key}"
                )
                continue

            self._logger.debug(f"FabricService received: {key} -> {item}")

            item_instance = None

            if isinstance(item, StreamItemNotification):
                item_instance = item.stream

            if isinstance(item, StreamTriggerNotification):
                item_instance = item.delegate

            if isinstance(item, CallTriggerNotification):
                item_instance = item.delegate

            if isinstance(item, CallResultNotification):
                continue
            # If the item is from the stream (instance) you are looking for, yield it
            if _instance is None or item_instance == _instance:
                yield key, item

    async def get_call_notification(self, call: str):
        """
        Retreives call result notification based on a call name.
        """
        grpc_stub = fabric_pb2_grpc.FabricStub(
            self._grpc_channel
        )  # Used to call rpc methods

        notification = grpc_stub.CallResultNotification(
            fabric_pb2.CallNotificationFilter(
                agentDelegate=self.agent_delegate, callName=call
            )
        )

        key = self.get_notification_key(notification)
        item = self._notifications_cache.get(key)

        return (key, item)
