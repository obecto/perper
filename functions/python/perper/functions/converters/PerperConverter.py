import logging
import typing
import json

from typing import Any, List
from azure.functions import meta

from perper.cache.notifications import CallTriggerNotification


class PerperConverter(meta.InConverter, meta.OutConverter, binding="perperTrigger"):
    @classmethod
    def decode(cls, data: meta.Datum, *, trigger_metadata) -> object:
        logging.info(f"Decoding: {data.value}")

        decoded_object = json.loads(data.value)
        # When Python 3.10 gets released and adopted TODO: Rewrite via structural pattern matching

        if decoded_object["Call"] != None and decoded_object["Delegate"] != None:
            return CallTriggerNotification(
                call=decoded_object["Call"], delegate=decoded_object["Delegate"]
            )

        return decoded_object

    @classmethod
    def encode(
        cls, obj: typing.Any, *, expected_type: typing.Optional[type]
    ) -> meta.Datum:
        print(f"Encoding {obj}")
        return meta.Datum(obj, object)


"""
    @classmethod
    def check_input_type_annotation(cls, pytype) -> bool:
        valid_types = (bytes)

        return (
            meta.is_iterable_type_annotation(pytype, valid_types)
            or (isinstance(pytype, type) and issubclass(pytype, valid_types))
        )

    @classmethod
    def check_output_type_annotation(cls, pytype) -> bool:
        valid_types = (str, bytes)
        return (
            meta.is_iterable_type_annotation(pytype, str)
            or (isinstance(pytype, type) and issubclass(pytype, valid_types))
        )

    @classmethod
    def decode(
        cls, data: meta.Datum, *, trigger_metadata
    ) -> str:
        logging.info(f"Decoding {data} with {trigger_metadata}")

        # JObject causes this to be None
        # data_type = data.type

        return "Hello"


    @classmethod
    def encode(cls, obj: typing.Any, *,
               expected_type: typing.Optional[type]) -> meta.Datum:
        return meta.Datum("Hello", str)
"""
