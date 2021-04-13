import logging
from typing import Any

# Temporary simulating installed package
import sys
import os

from perper.functions.converters import PerperConverter

def main(perper):
    logging.info(f'Perper Function has been started with args: {perper}')
    # with open("/tmp/temp.log", "w") as f:
        # print(context, file=f)
