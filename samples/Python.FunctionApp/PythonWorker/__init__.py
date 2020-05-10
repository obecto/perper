import logging
import azure.functions as func
from azure_functions.perper import PerperCache

def main(context, data : PerperCache):
    logging.info(f"Python stream trigger")
    logging.info(f"context -> " + context)
    logging.info(data.get_data())
    
    return "10"
