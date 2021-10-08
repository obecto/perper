import asyncio
import functools

class TaskCollection():
    def __init__(self):
        self.tasks = set()
        self.tasks_left = 0
        self.future = asyncio.get_running_loop().create_future()

    def remove(self, task):
        if task in self.tasks:
            self.tasks.remove(task)
            self.tasks_left -= 1
            if task.done():
                if task.cancelled():
                    self.future.cancel()
                elif task.exception() is not None:
                    self.future.set_exception(task.exception())
                elif self.tasks_left == 0:
                    self.future.set_result(None)

    def add(self, task):
        task = asyncio.ensure_future(task)
        if task not in self.tasks:
            self.tasks.add(task)
            self.tasks_left += 1
            task.add_done_callback(self.remove)

    def wait(self):
        return self.future
