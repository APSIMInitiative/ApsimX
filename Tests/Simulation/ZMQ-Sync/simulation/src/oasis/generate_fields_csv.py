#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Kraww!!
"""

import argparse
import copy
import csv
from dataclasses import dataclass
import random
import os

def generate_csv_from_grist(grist: list[dict], fpath: str):
    """Write the details of a flight plan to a CSV file.
    """
    print("Milling grist...")
    with open(fpath, "w", newline="") as csvp:
        writer = csv.DictWriter(csvp, fieldnames=grist[0].keys())
        writer.writeheader()
        [writer.writerow(datum) for datum in grist]
    print(f"Grist millt upon {fpath}.")


def generate_data(configs: dict) -> list[dict]:
    """Grist for The Mill.
    """
    data = []
    datum = {
        "Name": "",
        "Radius": "",
        "SW": "",
        "X": "",
        "Y": "",
        "Z": ""
    }

    r_default = 0.5 # Acres?
    spacing_default = 2.0
    vol_h2o_min = 0.1
    vol_h2o_max = 2.0
    index = 0
    for i in range(0, configs.dim_x):
        for j in range(0, configs.dim_y):
            for k in range(0, configs.dim_z):
                fresh_datum = copy.deepcopy(datum)
                fresh_datum["Name"] = f"Field{index}"
                fresh_datum["Radius"] = str(r_default)
                fresh_datum["SW"] = str(random.uniform(
                    vol_h2o_min, vol_h2o_max
                ))
                fresh_datum["X"] = str(spacing_default * i)
                fresh_datum["Y"] = str(spacing_default * j)
                fresh_datum["Z"] = str(spacing_default * k)
                data.append(fresh_datum)
                index += 1
    return data

def kraww():
    """Procedurally generate a CSV file of APSIM Field configs.
    """

    # cli args    
    parser = argparse.ArgumentParser(description="Generate field configuration csv")
    parser.add_argument("path", type=str, help="Path to save csv")
    args = parser.parse_args()
   
    # create path if doesn't already exist 
    dir_path = os.path.dirname(args.path)
    if dir_path and not os.path.exists(args.path):
        os.makedirs(os.path.dirname(args.path), exist_ok=True)
    
    # Number of fields in each dimension of spacetime.
    @dataclass
    class GristConfigs:
        dim_x: int = 4
        dim_y: int = 4
        dim_z: int = 1

    configs = GristConfigs()
    grist = generate_data(configs)
    generate_csv_from_grist(grist, args.path)
    

if __name__ == "__main__":
    kraww()

