import time
import random

from pyignite import Client
from pyignite.datatypes.primitive_objects import BoolObject

ignite = Client()
def test():
    with ignite.connect('127.0.0.1', 10800):
        sc = ignite.get_cache('test_stream')

        while True:
            sc.put(random.randrange(100000), random.randrange(10, 20))
            time.sleep(1)

test()