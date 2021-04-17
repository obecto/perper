from datetime import datetime

class Message_placeholders:
    def __init__(self):
        self.execute_form = {
            "header": {
                "msg_id": "",
                "msg_type": "execute_request",
                "version": "5.3",
                "date": datetime.now(),
            },
            "msg_id": "",
            "parent_header": {},
            "metadata": {},
            "content": {
                "code": "",
                "silent": False,
                "store_history": True,
                "user_expressions": {},
                "allow_stdin": True,
                "stop_on_error": True,
            }
        }

        self.comm_open = {
            "header": {
                "msg_id": "",
                "msg_type": "comm_open",
                "version": "5.3",
                "date": datetime.now()
            },
            "msg_id": "",
            "msg_type": "comm_open",
            "parent_header": {},
            "metadata": {},
            "content": {
                "comm_id": "",
                "target_name": "azure_handler",
                "data": {}
                }
            }

        self.comm_message = {
            "header": {
                "msg_id": "",
                "msg_type": "comm_msg",
                "version": "5.3",
                "date": datetime.now()
            },
            "msg_id": "",
            "parent_header": {},
            "metadata": {},
            "content": {
                "comm_id": "",
                "target_name": "azure_handler",
                "data": {}
                }
            }

