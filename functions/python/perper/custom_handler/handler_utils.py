from datetime import datetime

class Message_placeholders:
    def __init__(self):
        self.execute_form = {
            "header": {
                "msg_id": "",
                "username": "username",
                "session": "",
                "msg_type": "execute_request",
                "version": "5.2",
                "date": datetime.now().strftime("%m/%d/%Y, %H:%M:%S"),
            },
            "msg_id": "",
            "msg_type": "execute_request",
            "parent_header": {},
            "metadata": {},
            "content": {
                "code": "",
                "silent": False,
                "store_history": True,
                "user_expressions": {},
                "allow_stdin": True,
                "stop_on_error": True,
            },
            "buffers": [],
        }

        self.comm_open = {
            "header": {
                "msg_id": "",
                "session": "",
                "msg_type": "comm_open",
                "version": "5.2",
                "date": datetime.now().strftime("%m/%d/%Y, %H:%M:%S")},
            "msg_id": "",
            "msg_type": "comm_open",
            "parent_header": {},
            "metadata": {},
            "content": {
                "comm_id": "",
                "target_name": "azure_handler",
                "data": {}
                },
            "buffers": []}

        self.comm_message = {
            "header": {
                "msg_id": "",
                "session": "",
                "msg_type": "comm_msg",
                "version": "5.2",
                "date": datetime.now().strftime("%m/%d/%Y, %H:%M:%S")},
            "msg_id": "",
            "msg_type": "comm_msg",
            "parent_header": {},
            "metadata": {},
            "content": {
                "comm_id": "",
                "target_name": "azure_handler",
                "data": {}
                },
            "buffers": []}


registration_code = "def azure_handler(comm, open_msg):\n    # comm is the kernel Comm instance\n    # msg is the comm_open message\n\n    # Register handler for later messages\n    @comm.on_msg\n    def _recv(msg):\n        # Use msg['content']['data'] for the data in the message\n        comm.send({'echo': msg['content']['data']})\n        global message_received\n        message_received = msg\n\n    # Send data to the frontend on creation\n    #comm.send({'foo': 5})\n    \n    comm.send(open_msg['content']['data'])\n    global open_message_received\n    open_message_received = open_msg\n\nget_ipython().kernel.comm_manager.register_target('azure_handler', azure_handler)"
