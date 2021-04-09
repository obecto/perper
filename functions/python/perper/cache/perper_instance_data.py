import threading


class PerperInstanceData(object):
    def __init__(self, ignite, serializer):
        self.__ignite = ignite
        self.__serializer = serializer

        self.__next_stream_parameter_index = 0
        self.__next_anonymous_stream_parameter_index = 0
        self.__initialized = False
        self.lock = threading.Lock()
        self.instance_name = " "
        self.agent = " "
        self.parameters = " "

    def get_parameters(
        self,
    ):
        return self.parameters

    def get_agent(
        self,
    ):
        return self.agent

    def get_instance_name(
        self,
    ):
        return self.instance_name

    def __set_parameters(self, value):
        self.parameters = value

    def __set_agent(self, value):
        self.agent = value

    def __set_instance_name(self, value):
        self.instance_name = value

    def set_trigger_value(self, trigger):

        if "Call" in trigger:
            self.instance_name = trigger["Call"]
            calls_cache = self.__ignite.get_or_create_cache("calls")
        else:
            self.instance_name = trigger["Stream"]
            streams_cache = self.__ignite.get_or_create_cache("streams")

        instance_data_binary = instance_cache.get(instance_name)

        self.agent = instance_data_binary.agent
        self.parameters = instance_data_binary.parameters

        self.__initialized = True

    def get_parameters(self, object_type):
        return __serializer.deserialize(self.parameters)

    def get_stream_parameter_index(
        self,
    ):
        with self.lock:
            if self.__initialized:
                self.__next_stream_parameter_index -= 1
                return self.__next_stream_parameter_index
            else:
                self.__next_stream_parameter_index += 1
                return self.__next_stream_parameter_index
