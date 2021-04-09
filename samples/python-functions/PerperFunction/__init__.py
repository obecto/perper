import logging
from typing import Any
import azure.functions as func

# Temporary simulating installed package
import sys
sys.path.append("/home/viktorv/Projects/Work/Obecto/perper/functions/python")

from perper.functions.converters import PerperConverter

def main(perper):
    logging.info(f'Perper Function has been started with args: {perper}')
