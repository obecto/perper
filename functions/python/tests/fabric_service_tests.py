
import sys
import os
import grpc
import unittest

sys.path.append(os.path.dirname(os.path.realpath(__file__)) + "/../")

from unittest.mock import MagicMock, patch
from contextlib import redirect_stdout
from io import StringIO

from perper.services import FabricService, PerperConfig
from perper.cache.notifications import StreamItemNotification, StreamTriggerNotification, \
                                       CallResultNotification, CallTriggerNotification, \
                                       CallData, NotificationKeyString, NotificationKeyLong

from perper.utils.perper_thin_client import PerperThinClient
from perper.cache.notifications import FabricStub


class FabricServiceTests(unittest.TestCase):

    @patch('perper.utils.perper_thin_client.PerperThinClient')
    def test_startup_config(self, ignite_mock: PerperThinClient):

        perper_config = PerperConfig()
        os.environ["PERPER_AGENT_NAME"] = "TestAgent"
        os.environ["PERPER_ROOT_AGENT"] = "NotTestAgent"

        grpc.insecure_channel = MagicMock()
        fabric = FabricService(ignite = ignite_mock, config=perper_config)

        assert fabric.agent_delegate == "TestAgent"
        assert not fabric.is_initial_agent

        grpc.insecure_channel.assert_called_with(f"{perper_config.fabric_host}:40400")

        fabric.start()

        assert isinstance(fabric._grpc_stub, FabricStub)

    @patch('perper.utils.perper_thin_client.PerperThinClient')
    def test_call_initial_agent(self, ignite_mock: PerperThinClient):

        perper_config = PerperConfig()
        os.environ["PERPER_AGENT_NAME"] = "TestAgent"
        os.environ["PERPER_ROOT_AGENT"] = "TestAgent"

        grpc.insecure_channel = MagicMock()

        with redirect_stdout(StringIO()) as stdout:
            fabric = FabricService(ignite = ignite_mock, config=perper_config)
            fabric.start_initial_agent = MagicMock()
            fabric.start()

        fabric.start_initial_agent.assert_called_with()
        assert fabric.is_initial_agent