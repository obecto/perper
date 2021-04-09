class Agent:
    def __init__(self, **kwargs):
        if len(kwargs) == 2:
            self.construct_with_two_kwargs(
                context=kwargs["context"], serializer=kwargs["serializer"]
            )
        elif len(kwargs) == 4:
            self.construct_with_four_kwargs(
                agent_name=kwargs["agent_name"],
                agent_delegate=kwargs["agent_delegate"],
                context=kwargs["context"],
                serializer=kwargs["serializer"],
            )
        else:
            raise Exception("Arguments should be either 2 or 4")

    def construct_with_two_kwargs(self, context, serializer):
        self._context = context
        self._serializer = serializer

    def construct_with_four_kwargs(
        self, agent_name: str, agent_delegate: str, context, serializer
    ):
        self.construct_with_two_kwargs(context, serializer)
        self.agent_name = agent_name
        self.agent_delegate = agent_delegate

    async def call_function(self, function_name: str, parameters):
        call_data = await self._context.call(
            self.agent_name, self.agent_delegate, function_name, parameters
        )
        print("Result call data is:", call_data)
        if call_data.result == None:
            return None
        return self._serializer.deserialize(call_data.result)

    def call_action(self, action_name: str, parameters):
        return self._context.call(
            self.agent_name, self.agent_delegate, action_name, parameters
        )
