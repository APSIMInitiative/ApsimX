#!/usr/bin/python3
"""
apsim.py - APSim-specific methods.
"""
import logging;
from utils import *;


class APSim():
    """
    """
    def load(self, fpath: str):
        """
        Load an existing .apsimx file.
        @input: fpath
        """
        self.sim = load_file(fpath);
        logging.info(self.sim);

if __name__ == "__main__":
    sim = APSim();
    sim.load("../examples/toplevelsync.apsimx");
