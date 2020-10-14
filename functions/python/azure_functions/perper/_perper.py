import abc
import typing

class AbstractPerperCache(abc.ABC):

    @abc.abstractmethod
    def get_data(self) -> list:
        pass

