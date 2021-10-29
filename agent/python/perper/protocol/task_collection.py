import asyncio
import functools


class TaskCollection:
    def __init__(self):
        self.tasks = set()
        self.tasks_left = 0
        self.cancelled = False
        self.future = asyncio.get_running_loop().create_future()

    def remove(self, task):
        if task in self.tasks:
            self.tasks.remove(task)
            self.tasks_left -= 1
            if task.done():
                if task.cancelled():
                    pass
                elif task.exception() is not None:
                    if not self.future.done():
                        self.future.set_exception(task.exception())
                        ex = task.exception()
                elif self.tasks_left == 0:
                    if not self.future.done():
                        self.future.set_result(None)

    def add(self, task):
        task = asyncio.ensure_future(task)
        if task not in self.tasks:
            self.tasks.add(task)
            self.tasks_left += 1
            task.add_done_callback(self.remove)
            if self.cancelled:
                task.cancel()

    def cancel(self):
        self.cancelled = True
        for task in self.tasks:
            task.cancel()

    def wait(self, complete=True):
        if complete and self.tasks_left == 0 and not self.future.done():
            self.future.set_result(None)

        return self.future
