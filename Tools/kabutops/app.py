#!/usr/bin/python3
"""
app.py - launcher for kabutops apps.
@usage: ./app.py
"""
import logging;
import sys;
sys.path.append("src/");    # Sharing is caring.
from utils import *;
from apsim import *;

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
    logging.info("Welcome to kabutops!");
    node = CodeArray(name="Test");
    node.gamma_knife("tmp.cs");
    print(node);
    #sim = APSim();
    #sim.load("./examples/toplevelsync.apsimx");
    return 0;

if __name__ == "__main__":
    configure_logger();
    kabutops();
