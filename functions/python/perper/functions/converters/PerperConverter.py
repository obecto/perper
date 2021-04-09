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
