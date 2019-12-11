import logging

import azure.functions as func
import azure_functions.perper as perper


def main(context: perper.StreamContex, state: func.Out[int]):
    logging.info(f"Python stream trigger")
