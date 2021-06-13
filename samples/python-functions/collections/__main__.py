from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import CollectionObject, String, ObjectArrayObject
from perper.functions import Perper

perper = Perper()

class A(
    metaclass=GenericObjectMeta,
    type_name="A",
    schema=OrderedDict([("list", CollectionObject),]),
):
    pass

class B(
    metaclass=GenericObjectMeta,
    type_name="B",
    schema=OrderedDict([("string", String),]),
):
    pass

perper.ignite.register_binary_type(B)
cache = perper.ignite.get_or_create_cache("test")

cache.put(
    "data1",
    A(
        list=(ObjectArrayObject.OBJECT, [B(string="123")])
    )
)

data = cache.get("data1")
print(data)