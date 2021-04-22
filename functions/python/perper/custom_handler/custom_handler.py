from typing import Optional, Awaitable
import os
import json
import time

from tornado import gen, web
from tornado.web import RedirectHandler

from jupyterlab.labapp import LabApp
from notebook.utils import url_path_join, maybe_future
from notebook.base.handlers import IPythonHandler
from ipykernel.comm import Comm
from jupyter_client.session import Session
from jupyter_client.jsonutil import date_default
from datetime import datetime

from perper.custom_handler.handler_utils import Message_placeholders
from . import global_state


class Handler(IPythonHandler):
    def __init__(self, *args, **kwargs):
        self.log.info("\n\n The custom handler is called \n\n")
        super().__init__(*args, **kwargs)

    # Using azure functions only post is available
    @gen.coroutine
    def post(self):
        if "initial_kernel_id" not in global_state:
            self.log.info("Starting a kernel")
            yield self.start_notebook_kernel()
            kernel_id = global_state["initial_kernel_id"]
        if not global_state["perper_imported"]:
            yield self.import_perper()
            # Wait until perper is imported, then continue
            yield gen.sleep(4)

        kernel_id = global_state["initial_kernel_id"]
        message = self.get_json_body()
        message = message['Metadata']

        if global_state["comm_opened"]:
            message_form = Message_placeholders().comm_message
        else:
            message_form = Message_placeholders().comm_open
            global_state["comm_opened"] = True

        try:
            message_form["content"]["data"] = message["content"]["data"]
            # This is a Postman message
        except:
            message_form["content"]["data"] = message["InstanceName"]
            # This is a C# message

        yield maybe_future(self.send_to_kernel(message_form, kernel_id))
        # TODO:Take care of kernel restarts
        self.set_status(200)
        self.finish({"Outputs":{}, "Logs":["Success"], "ReturnValue":None})

    @gen.coroutine
    def import_perper(self):
        kernel_id = global_state["initial_kernel_id"]
        perper_message = Message_placeholders().execute_form
        perper_message['content']['code'] = "import perper.jupyter as jupyter"
        yield maybe_future(self.send_to_kernel(perper_message, kernel_id))
        global_state["perper_imported"] = True

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
        global_state["session"] = Session(
                session=global_state["session_id"], config=self.config
            )
        self.kernel_manager.add_restart_callback(
            global_state["initial_kernel_id"], self.kernel_restart_callback
        )

        return model

    def kernel_restart_callback(self):
        self.log.info("Custom handler: kernel restarted")
        global_state["kernel_restarted"] = True

    @gen.coroutine
    def send_to_kernel(self, message, kernel_id):
        self.channels = {}
        if "session_id" in global_state:
            self.session = Session(
                session=global_state["session_id"], config=self.config
            )
            self.session = global_state["session"]
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
        self.log.debug(f"Sending to kernel: {message}")
        self.session.send(stream, message)


def load_jupyter_server_extension(serverapp):
    """
    Called when the extension is loaded.

    Args:
        serverapp (NotebookWebApplication): handle to the Notebook webserver instance.
    """
    web_app = serverapp.web_app
    host_pattern = ".*$"
    route_pattern = url_path_join(web_app.settings["base_url"], "/PythonNotebook")
    web_app.add_handlers(host_pattern, [(route_pattern, Handler)])
    route_pattern = url_path_join(web_app.settings["base_url"], "/Notebook")
    web_app.add_handlers(host_pattern, [(route_pattern, Handler)])
