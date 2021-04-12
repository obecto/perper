import logging
from typing import Any
import azure.functions as func

# Temporary simulating installed package
import sys
import os
sys.path.append(os.path.join(sys.path[0], '../../perper/functions/python'))

from perper.functions.converters import PerperConverter

def main(perper):
    logging.info(f'Perper Function has been started with args: {perper}')
    # with open("/tmp/temp.log", "w") as f:
        # print(context, file=f)
