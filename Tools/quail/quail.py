"""
@file   quail.py

Analysis script comparing volumetric water content (VWC) from OASIS sim and
in-ground sensors.

@author jLab
@author nubby   (jlee211@ucsc.edu)

@date   27 Jan 2024
"""
import argparse
import csv
import geojson
import numpy as np
import os
import rasterio
import re

from dataclasses import dataclass
from datetime import datetime, timedelta
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
        if (not self.check_in_farm(sensor.coordinates)):
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
    print(data_json["features"][0]["properties"]["ts"])
    print(data_json["features"][-1]["properties"]["ts"])
    [data.append(Datum(
            timestamp=datetime.strptime(
                entry["properties"]["ts"],
                "%Y-%m-%d %H:%M:%S"
            ),
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
    return crs

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

"""_ingest_sim_data_single_dict(data_tiff, IRLSensor) -> Sensor

@param  coordinates
@param  data_rio   ()          Raster data from a GeoTIFF file..
@param  depth
@param  name

@return             (Sensor)    Translated data.
"""
# TODO: Needs timestamps of start/end.
def _ingest_sim_data_single_sensor(
        coordinates: tuple[float],
        data_rio: any, # TODO(nubby)
        depth: float,
        name: str,
        start_ts: datetime
        ) -> Sensor:
    data = []
    ts = start_ts
    print(ts.strftime("%Y-%m-%d %H:%M:%S"))
    td_24hrs = timedelta(days=1)
    for index in data_rio.indexes:
        try:
            band = data_rio.read(index)
            data.append(Datum(
                timestamp=ts,
                VWC=band[coordinates[0], coordinates[1]]
            ))
        except IndexError:
            # Some raster frames are deficient, but not a problem so long as
            # we keep track of time.
            pass
        finally:
            # Always advance a day, even when a sample is missing.
            ts = ts + td_24hrs
    print(ts.strftime("%Y-%m-%d %H:%M:%S"))

    return Sensor(
        coordinates=coordinates,
        data=data,
        depth=depth,
        name=name
    )

"""_ingest_sim_data_tiff(data_paths, sensors, SimFarm, crs)
"""
def _ingest_sim_data_tiff(
        data_paths: list[str],
        sensors: list[Sensor],
        SimFarm: Farm,
        crs: dict = None
    ):
    regex = re.compile(r".*([0-9]).*")
    depth_lut = {
        "0": [0.0,0.199],
        "1": [0.2,0.399],
        "2": [0.4,0.599],
        "3": [0.6,0.799],
        "4": [0.8,0.999],
        "5": [1.0,0.1199],
        "6": [1.2,0.1399],
        "7": [1.4,0.1599],
        "8": [1.6,0.1799],
        "9": [1.8,0.1999]
    }
    for sensor in sensors:
        for file in data_paths:
            try:
                check = regex.match(file)
                depth_code = check.group(1)
                depth_range = depth_lut[depth_code]
            except AttributeError:
                # Skip files with name formatting issues.
                continue
            if (
                sensor.depth >= depth_range[0] and
                sensor.depth <= depth_range[1]
            ):
                print(f"Ingesting sim data from {file}...")
                dataset = rasterio.open(file)
                x, y = dataset.index(
                    sensor.coordinates[0],
                    sensor.coordinates[1]
                )
                SimFarm.add_sensor(_ingest_sim_data_single_sensor(
                    coordinates=[x, y],
                    data_rio=dataset,
                    depth=sensor.depth,
                    name=sensor.name,
                    start_ts=sensor.data[0].timestamp
                ))
                dataset.close()

"""_ingest_sim_data_npy(data_path) -> data
"""
def _ingest_sim_data_npy(data_path: str, crs: dict = None):
    # TODO(nubby)
    return None

"""_get_sim_data(data_path, data_files, crs, SimFarm)
Digest real sensor coordinates to generate simulated sensors.

@param  data_path   (str)
@param  data_files  (list[str])
@param  sensors     (list[Sensor])
@param  SimFarm     (Farm)
@param  crs         (dict)                  [optional] Use native crs if none
                                            given.
"""
def _get_sim_data(
        data_path: str,
        data_files: list[str],
        sensors: list[Sensor],
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
        _ingest_sim_data_tiff(
                data_paths=sim_data_paths,
                sensors=sensors,
                SimFarm=SimFarm,
                crs=crs
                )
    elif any("npy" in file for file in data_files):
        sim_data_path = [os.path.join(
            data_path,
            file
        ) for file in data_files if "npy" in file][0]
        _ingest_sim_data_npy(sim_data_path, crs)
    else:
        raise IOError

"""compare_sim2real()
"""
def compare_sim2real(IRLFarm: Farm, SimFarm: Farm):
    pass

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
        sensors=IRLFarm.Sensors,
        SimFarm=SimFarm,
        crs=crs
    )
    compare_sim2real(SimFarm=SimFarm, IRLFarm=IRLFarm)


"""quail(data_path)
Generate plots comparing real and sim data.

@param  data_path   (str)   Path to directory containing data.
"""
def quail(data_path: str):
    ingest(data_path)
    print("𓅪")

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
