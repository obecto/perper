import time
import random

from pyignite.datatypes.primitive_objects import BoolObject
from perper.protocol.thin_client import PerperIgniteClient

ignite = PerperIgniteClient()
def test():
    with ignite.connect('127.0.0.1', 10800):
        sc = ignite.get_cache('test_stream')

        while True:
            sc.put(random.randrange(100000), random.randrange(10, 20))
            time.sleep(1)

test()