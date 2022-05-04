import asyncio
import datetime
import uuid

import perpernet
from perpernet import task_to_future
from perpernet.extensions.context_vars import fabric_execution, fabric_service

from System import Activator, Object
from Perper.Model import PerperAgent
from Perper.Extensions import AsyncLocals
from PerperUtils import ClassBuilder


async def try_set_state(key, func, *lazyawaitargs):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    none_id = str(uuid.uuid4())
    result = await perpernet.get_state(key, none_id)

    if result == none_id:
        result = await func(*(await asyncio.gather(*(f() for f in lazyawaitargs))))
        extra = None
        if isinstance(result, tuple):
            (result, extra) = result
        print("Started instance for", key, ":", result, "|", extra)
        await perpernet.set_state(key, result)
        if extra is not None:
            await perpernet.set_state(key + "_", extra)
    else:
        print("Already have instance for", key, ":", result)


key_epoch = datetime.datetime(1970, 1, 1, tzinfo=datetime.timezone.utc)
key_delta = datetime.timedelta(milliseconds=1)


def to_key(x):
    if isinstance(x, datetime.datetime):
        return (x.astimezone(datetime.timezone.utc) - key_epoch) // key_delta
    else:
        return x


def to_datetime(x):
    if isinstance(x, int):  # key
        return key_epoch + x * key_delta
    else:
        return x


async def to_stream(x, from_key=-1):
    if type(x).__name__ == "PerperStream":
        return x
    elif type(x).__name__ == "PerperAgent":
        return await perpernet.call_agent(x, "GetStream", to_key(from_key), void=False)


async def enumerate_stream_with_times(stream, start_key=-1, end_key=-1, *, show_progress=False):
    end_key = to_key(end_key)
    start_key = to_key(start_key)
    stream = await to_stream(stream, start_key)
    if show_progress and end_key != -1 and start_key != -1:
        from tqdm.auto import tqdm

        progress_bar = tqdm(total=(end_key - start_key) // stream.stride)
    async for res in perpernet.enumerate_stream_with_keys(stream):
        key = res.Item1
        item = res.Item2
        if end_key != -1 and key >= end_key:
            break
        if show_progress:
            progress_bar.update()
        yield to_datetime(key), item
    if show_progress:
        progress_bar.close()


def dict_to_data_object(dict_, class_=None):
    if class_ is None:
        class_ = create_hatcherydata_class(dict_.keys())

    data_object = Activator.CreateInstance(class_)
    for (k, v) in dict_.items():
        prop = class_.GetProperty(k)
        prop.SetValue(data_object, v)
    return data_object


def data_object_to_dict(data):
    return {PI.Name: getattr(data, PI.Name) for PI in data.GetType().GetProperties()}


def data_object_to_dict_with_time(time, data):
    d = data_object_to_dict(data)
    d["time"] = time
    return d


async def csv_to_stream(csv_path):
    async def fill_stream(csv_path, stream):
        i = 0
        hatcherydata_class = None
        with open(csv_path, "r") as source:
            for line in source:
                if line:
                    i += 1
                    line_arr = line.split(",")
                    line_arr[-1] = line_arr[-1].replace("\n", "")

                    if i == 1:
                        headers = line_arr
                        hatcherydata_class = create_hatcherydata_class([header for header in headers if header != "time"])
                    elif i > 1:
                        line_dict = {headers[j]: line_arr[j] for j in range(len(headers))}

                        if "time" in line_dict:
                            await task_to_future(
                                lambda _: fabric_service.get().WriteStreamItem[Object](
                                    stream.Stream, int(line_dict.pop("time")), dict_to_data_object(line_dict, hatcherydata_class)
                                )
                            )
                        else:
                            await task_to_future(
                                lambda _: fabric_service.get().WriteStreamItem[Object](
                                    stream.Stream, fabric_service.get().CurrentTicks, dict_to_data_object(line_dict, hatcherydata_class)
                                )
                            )

    stream = await perpernet.create_blank_stream(ephemeral=False)
    await fill_stream(csv_path, stream)
    stream = perpernet.replay_stream(stream)

    return stream


def create_hatcherydata_class(keys):
    builder = ClassBuilder("HatcheryData")
    return builder.CreateType(keys, [Object] * len(keys))
