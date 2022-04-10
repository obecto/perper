from Perper.Application import PerperConnection


async def establish_connection():
    fabric = await PerperConnection.EstablishConnection()
    return fabric


def configure_instance() -> (str, str):
    agent, instance = PerperConnection.ConfigureInstance()
    return agent, instance

