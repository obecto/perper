from ..bindings import task_to_future

from Perper.Application import PerperConnection


async def establish_connection():
    fabric = await task_to_future(lambda _: PerperConnection.EstablishConnection())
    return fabric


def configure_instance() -> (str, str):
    tuple = PerperConnection.ConfigureInstance()
    agent = tuple.Item1
    instance = tuple.Item2
    return agent, instance
