import logging

import azure.functions as func
import azure_functions.perper as perper


def main(context: perper.StreamContex):
    logging.info(f"Python stream trigger")
