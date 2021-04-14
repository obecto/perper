from typing import Optional, Awaitable
import os
import json
import time

from tornado import gen, web

from notebook.utils import url_path_join, maybe_future
from notebook.base.handlers import IPythonHandler
from jupyter_client.session import Session
from jupyter_client.jsonutil import date_default

from perper.custom_handler.handler_utils import Message_placeholders, registration_code
from . import global_state


class Handler(IPythonHandler):
    def __init__(self, *args, **kwargs):
        print("\n\n The custom handler is called \n\n")
        super().__init__(*args, **kwargs)

    # Using azure functions only post is available
    @gen.coroutine
    def post(self):
        if "initial_kernel_id" not in global_state:
            yield maybe_future(self.start_notebook_kernel())
            kernel_id = global_state["initial_kernel_id"]
            self.post()
        else:
            self.log.info(f"Global state: {global_state}")
            kernel_id = global_state["initial_kernel_id"]
            kernel = self.kernel_manager.get_kernel(kernel_id)

            message = self.get_json_body()
            self.log.info(f"Message: {message}")
            if "Metadata" in message:
                # When using azure the message is in the 'Metadata' section of the json
                message = message["Metadata"]

            if "header" not in message:
                self.log.info(f"Not a comm message: {message}")
                self.finish("Not a com message, not being sent to kernel")
                return

            self.send_to_kernel(message, kernel_id)
            # TODO:Take care of kernel restarts
            sessions = yield maybe_future(self.session_manager.list_sessions())
            self.finish(
                {
                    "Outputs": {"res": {"body": json.dumps(sessions)}},
                    "ReturnValue": json.dumps(sessions),
                }
            )

    @gen.coroutine
    def start_standalone_kernel(self):
        kernel_id = yield maybe_future(
            self.kernel_manager.start_kernel(path="Test.ipynb", kernel_name="python3")
        )
        global_state["initial_kernel_id"] = kernel_id

    @gen.coroutine
    def start_notebook_kernel(self):
        model = yield maybe_future(
            self.session_manager.create_session(
                path="Test.ipynb",
                kernel_name="python3",
                kernel_id=None,
                name="",
                type="notebook",
            )
        )
        global_state["initial_kernel_id"] = model["kernel"]["id"]
        global_state["session_id"] = model["id"]
        self.kernel_manager.add_restart_callback(
            global_state["initial_kernel_id"], self.kernel_restart_callback
        )

        return model

    def kernel_restart_callback(self):
        self.log.info("Custom handler: kernel restarted")
        global_state["kernel_restarted"] = True

    def send_to_kernel(self, message, kernel_id):
        self.channels = {}
        if "session_id" in global_state:
            self.session = Session(
                session=global_state["session_id"], config=self.config
            )
        else:
            self.session = Session(config=self.config)
            raise NotImplementedError()
            # TODO: locate the session_id in the Session object, it is not Session.session

        kernel = self.kernel_manager.get_kernel(kernel_id)
        self.session.key = kernel.session.key

        km = self.kernel_manager
        identity = self.session.bsession
        meth = getattr(km, "connect_shell")
        self.channels["shell"] = stream = meth(kernel_id, identity=identity)
        stream.channel = "shell"

        stream = self.channels["shell"]
        self.session.send(stream, message)


def load_jupyter_server_extension(nb_server_app):
    """
    Called when the extension is loaded.

    Args:
        nb_server_app (NotebookWebApplication): handle to the Notebook webserver instance.
    """
    web_app = nb_server_app.web_app
    host_pattern = ".*$"
    route_pattern = url_path_join(web_app.settings["base_url"], "/PythonNotebook")
    web_app.add_handlers(host_pattern, [(route_pattern, Handler)])
