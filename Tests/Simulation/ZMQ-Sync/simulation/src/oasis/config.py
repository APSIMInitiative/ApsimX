#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Kraww!!
"""

import copy
import csv
import random

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
    spacing_default = 1
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