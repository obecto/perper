import logging

import azure.functions as func

def main(context, value):
    logging.info(f"Python stream trigger")
    logging.info(f"context -> " + context)
    logging.info(f"value -> " + value)
    return "10"
