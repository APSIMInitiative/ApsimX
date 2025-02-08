"""
@file   quail.py

Analysis script comparing volumetric water content (VWC) from OASIS sim and
in-ground sensors.

@author jLab
@date   27 Jan 2024
"""
import argparse
import csv
import geojson
import numpy as np
import os
import rasterio

from dataclasses import dataclass
from datetime import datetime
from matplotlib import pyplot as plt


DEFAULT_DATA_DIR = "./data/"
DEFAULT_TOLERANCE = 0.0001      # Tolerance in coordinates for locations.

DEFAULT_MM_TO_LAYER = 200       # Encoding of depth until we can encode TIFF
                                # files with depth data in Apsim.

"""Datum
Storage for either sim or sensor datum.
"""
@dataclass
class Datum:
    timestamp:  datetime
    VWC:        float
    SC:         float = None
    ST:         float = None

"""Sensor
Storage for time series data from either a location in sim or a sensor.
"""
@dataclass
class Sensor:
    coordinates:    [float, float]  #   [lat, lon]
    data:           list[Datum]
    depth:          float
    name:           str

"""Farm
Class for holding data from an array of sensors IRL.
"""
class Farm(object):
    def __init__(
            self,
            sensors: [Sensor] = []
        ):
        self.Sensors = []
        # Simplify the shape of the Farm to a rectangle for now.
        self.boundary = {
            "NE": [0,0],
            "NW": [0,0],
            "SE": [0,0],
            "SW": [0,0]
        }
        [self.add_sensor(sensor) for sensor in sensors]

    def _update_boundary(self, coordinates: [float, float]):
        """_update_boundary(coordinates)
        Expand the border of the Farm in the direction of the new set of
        coordinates.

        @param  coordinates ([float, float])  Latitude, longitude.
        """
        # TODO(nubby): Make these meaningful
        self.boundary["NE"] = coordinates
        self.boundary["NW"] = coordinates
        self.boundary["SE"] = coordinates
        self.boundary["SW"] = coordinates

    def check_in_farm(self, coordinates: [float, float]):
        """check_in_farm(coordinates) -> bool
        Are the coordinates within the Farm boundaries?

        @param  coordinates [float, float]  Latitude, longitude.
        """
        # TODO(nubby)
        return False
        
    def add_sensor(self, sensor: Sensor):
        """add_sensor(sensor)
        Add sensor to Farm and adjust boundaries if needed.

        @param  sensor  (Sensor)
        """
        self.Sensors.append(sensor)
        if !(self.check_in_farm(sensor.coordinates)):
            self._update_boundary(sensor.coordinates)
        print("うまい")


"""_ingest_sensor_data_single_dict(data_json) -> Sensor, crs

@param  data_json   (dict)      Data from a sensor saved as a dict.
@return             (Sensor)    Translated data.
@return crs         (str)       Coordinate reference system used (if any).
"""
def _ingest_sensor_data_single_dict(data_json: dict) -> Sensor:
    data = []
    name = data_json["name"]
    # Assume coordinates and depth do not change before the file entry.
    coordinates = data_json["features"][-1]["geometry"]["coordinates"]
    crs = None  # TODO(nubby)
    depth = data_json["features"][-1]["properties"]["depth"]
    [data.append(Datum(
            timestamp=entry["properties"]["ts"],
            SC=entry["properties"]["SC"],
            ST=entry["properties"]["ST"],
            VWC=entry["properties"]["WC"]
    )) for entry in data_json["features"]]
    return Sensor(
        coordinates=coordinates,
        data=data,
        depth=depth,
        name=name
    ), crs

"""_ingest_sensor_data_geojson(data_paths) -> data

@param  data_paths  (list[str]) 
@param  IRLFarm     (Farm)
"""
def _ingest_sensor_data_geojson(data_paths: list[str], IRLFarm: Farm):
    for file in data_paths:
        print(f"Ingesting sensor data from {file}...")
        with open(file, "r+") as fp:
            raw_data = geojson.load(fp)
            sensor, crs = _ingest_sensor_data_single_dict(raw_data)
            IRLFarm.add_sensor(sensor)
    return sensor_data, crs

"""_ingest_sensor_data_csv(data_path) -> data
"""
def _ingest_sensor_data_csv(data_path: str, IRLFarm: Farm):
    # Set the path to farm sensor data.
    farm_data_path = None
    for file in data_files:
        if "csv" in file:
            farm_data_path = file
            break
    assert farm_data_path   # Raise assertion error if farm data not found.

    # Extract information about each sensor used through the main farm CSV file.
    csv_lines = []
    with open(farm_data_path, "r+") as fdp:
        reader = csv.reader(fdp)
        for row in reader:
            csv_lines += row

    # TODO(nubby): Get @jtmadden's code for CSV conversion.
    
    return None

"""_get_sensor_data( data_path, data_files) -> crs 

@return crs (str)   Coordinate reference system used.
"""
def _get_sensor_data(
        data_path: str,
        data_files: list[str],
        IRLFarm: Farm
    ) -> str:
    # Read .geojson-formatted sensor data if pre-processed, otherwise read
    # .csv-formatted sensor data.
    if any("geojson" in file for file in data_files):
        sensor_data_paths = [
            os.path.join(
                data_path,
                file
            ) for file in data_files if "geojson" in file
        ]
        crs = _ingest_sensor_data_geojson(sensor_data_paths, IRLFarm)
    elif any("csv" in file for file in data_files):
        sensor_data_path = [os.path.join(
            data_path,
            file
        ) for file in data_files if "csv" in file][0]
        crs = _ingest_sensor_data_csv(sensor_data_path, IRLFarm)
    else:
        raise IOError

    return crs

"""_ingest_sim_data_tiff(data_path) -> data
NOTE:
    Depth map:
        0 =    0-200
        1 =  200-400
        2 =  400-600
        3 =  600-800
        4 =  800-1000
        5 = 1000-1200
        6 = 1200-1400
        7 = 1400-1600
        8 = 1600-1800
        9 = 1800-2000
"""
def _ingest_sim_data_tiff(
        data_paths: list[str],
        crs: dict = None
    ) -> list[Sensor]:
    sim_data = []
    for file in data_paths:
        print(f"Ingesting sim data from {file}...")
        # "top" and "right" of bounding boxesn correspond to locations of
        # sensors, while 
        #with (rasterio.open(file), "r+") as rf:
        #    print(str(rf.count))
        try:
            dataset = rasterio.open(file)
            #print(dataset.indexes)
            band1 = dataset.read(1)
            #print(dataset.bounds)
            row, col = dataset.index(-75.5838, 37.7427)
            print(band1[row, col])
            dataset.close()
            print("うまい.")
        except:
            pass
    return sim_data

"""_ingest_sim_data_npy(data_path) -> data
"""
def _ingest_sim_data_npy(data_path: str, crs: dict = None) -> list[Sensor]:
    # TODO(nubby)
    return None

"""_get_sim_data(data_path, data_files, crs, SimFarm)
Digest real sensor coordinates to generate simulated sensors.

@param  data_path   (str)
@param  data_files  (list[str])
@param  SimFarm     (Farm)
@param  crs         (dict)      [optional] Use native crs if none given.
"""
def _get_sim_data(
        data_path: str,
        data_files: list[str],
        SimFarm: Farm,
        crs: dict = None
    ) -> list[Sensor]:
    # Read .tiff-formatted sim data if pre-processed, otherwise read .npy data.
    if any("tiff" in file for file in data_files):
        sim_data_paths = [
            os.path.join(
                data_path,
                file
            ) for file in data_files if "tiff" in file
        ]
        _ingest_sim_data_tiff(sim_data_paths, SimFarm, crs)
    elif any("npy" in file for file in data_files):
        sim_data_path = [os.path.join(
            data_path,
            file
        ) for file in data_files if "npy" in file][0]
        sim_data = _ingest_sim_data_npy(sim_data_path, crs)
    else:
        raise IOError

    return sim_data

"""compare_sim2real()
"""
def compare_sim2real(sensor_data, sim_data):
    sim_data = []
    for sim in sim_data:
        for sensor in sensor_data:
            print(sensor.depth)

        exit()
        print(f"Ingesting sim data from {file}...")
        try:
            dataset = rasterio.open(file)
            print(dataset.read())
            dataset.close()
            print("うまい.")
        except:
            pass
    return sim_data

"""ingest(data_path) -> data
Extract data from provided directory or return an empty array.
"""
def ingest(data_path: str) -> tuple[Sensor, Sensor]:
    IRLFarm = Farm()
    SimFarm = Farm()
    data_files = os.listdir(path=data_path)

    crs = _get_sensor_data(
            data_path=data_path,
            data_files=data_files,
            IRLFarm=IRLFarm
            )
    _get_sim_data(
            data_path=data_path,
            data_files=data_files,
            SimFarm=SimFarm,
            crs=crs
            )
    compare_sim2real(SimFarm=SimFarm, IRLFarm=IRLFarm)

    return sensor_data, sim_data


"""quail(data_path)
Generate plots comparing real and sim data.

@args   data_path   (str)   Path to directory containing data.
"""
def quail(data_path: str):
    sensor_data, sim_data = ingest(data_path)

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "-d",
        "--data_path",
        default=DEFAULT_DATA_DIR,
        help="Specify path to directory containing data for comparison.",
        type=str
    )
    args = parser.parse_args()
    quail(args.data_path)
