#!/usr/bin/python3
"""
app.py - launcher for kabutops apps.
@usage: ./app.py
"""
import logging;
from src.utils import *;

def configure_logger():
    logger = logging.getLogger(__name__);
    logging.basicConfig(
            datefmt="%Y%m%d-%H:%M:%S",
            format="%(asctime)s [%(levelname)s] %(message)s",
            level=logging.INFO
    );

def kabutops():
    """
    """
    logging.info("Welcome to kabutops!")
    return 0;

if __name__ == "__main__":
    configure_logger();
    kabutops();
