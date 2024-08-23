#!/usr/bin/python3
"""
apsim.py - APSim-specific methods.
"""
import json;
import logging;
import os;
from typing import List;
from utils import *;


class APSimNode():
    """
    Base class for all APSim nodes.
    """
    def __init__(self, configs: dict) -> None:
        self.info = {**{
                "$type": "Models",
                "Children": [],         # NOTE: exploring other data structures.
                "Enabled": True,
                "Name": "",
                "ReadOnly": False,
                "ResourceName": None
            }, **configs
        };

    def __str__(self):
        return str(self.info);

    def __repr__(self):
        return self.info;

    def generate(self):
        """
        Get the "APSim-friendly" representation of this Node.
        @output: 
        """
        return json.dumps(self.info);

"""
CodeArray

Utilities and datatype for CodeArrays.
"""
def convert_str_to_code_array(code: str) -> List[str]:
    return [line.replace('\\', '\\\\') for line in code.split("\n")];

class CodeArray(APSimNode):
    """
    @input: name
    @input: code - String representation of CodeArray substance.
    """
    def __init__(self, name: str, code: str = "") -> None:
        self.code = code;
        configs = {
            "$type": "Models.Manager, Models",
            "CodeArray": code,
            "Name": name
        }
        super().__init__(configs);

    def import_cs(self, fpath: str) -> None:
        """
        """
        if (os.path.isfile(fpath)):
            self.code = load_file(fpath);
        self.info["CodeArray"] = convert_str_to_code_array(self.code);
    gamma_knife = import_cs;


class APSim():
    """
    """
    def _digest(self, sim_dict: dict) -> None:
        """
        Parse out useful information from a dict-formatted simulation.
        """
        self.info = sim_dict;
        self.fields = [];


    def load(self, fpath: str) -> None:
        """
        Load an existing .apsimx file.
        @input: fpath
        """
        self._digest(load_file(fpath));
        logging.info("Loaded simulation from {}".format(fpath));

    def generate(self, fpath: str) -> None:
        """
        Generate a .apsimx file from the current simulation.
        @input: fpath
        """
        write_file(self.info, fpath);


if __name__ == "__main__":
    print("Hi nub.");
