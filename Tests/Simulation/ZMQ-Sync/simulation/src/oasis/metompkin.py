"""Converts the Metompkin farm dataset into GeoJSON format

Each sensor has it's own layer

Source to download dataset: https://www.hydroshare.org/resource/c414b4b08e2647da9ae5d70dc1aae60c/
"""

import csv
import fiona

class MetompkinConverter:
    """Class to convert dataset into GeoJSON format"""

    # Python geo protocol defintion
    SCHEMA = {
        "geometry": "Point",
        "properties": dict([
            ('depth', 'float'),
            ('ts', 'int'),
            ('SC', 'float'),
            ('ST', 'float'),
            ('WC', 'float'),
        ])
    }

    #north: 37.7448
    #east: -75.5833
    #west: -75.5838
    #south: 37.7427

    # Stores locations of sensor measurements
    LOCATIONS = {
        "VFHA": (-75.5838, 37.7448),
        "VFMA": (-75.5838, 37.7427),
        "VFMS": (-75.5833, 37.7448),
        "VFTA": (-75.5833, 37.7427),
    }

    # Depths of sensors
    DEPTHS = {
        "S": 0.1,
        "D": 0.3,
    }

    def __init__(self, epsg : int = 4326, driver : str = "GeoJSON"):
        """Default constructor

        Reads the csv at the specified path into memory

        Args:
            epsg: Coordinate reference system. Default WGS84
            driver: fiona driver
        """

        self.crs = fiona.crs.CRS.from_epsg(epsg)
        self.driver = driver

    def find_sensors(self, path : str):
        """Finds list of sensors from csv header

        Args:
            path: Path to csv file
        """

        sensor_names = set()

        # get header
        with open(path, "r", newline="") as csvfile:
            reader = csv.reader(csvfile)
            header_row = next(reader)

        # add name to set excluding "TimeSamp"
        for col in header_row:
            if col != "TimeStamp":
                name, _ = col.split("_")
                sensor_names.add(name)

        return sensor_names

    def find_pos(self, name : str) -> tuple:
        """Find position given a sensor name

        Uses the self.LOCATIONS dict as a lookup table for position.

        Args:
            name: Name of sensor

        Returns:
            Tuple of coordinates in format (lon, lat)
        """

        loc_name, _, _ = name.split("-")
        return self.LOCATIONS[loc_name]

    def find_depth(self, name : str) -> float:
        """Find depth from a sensor name

        Uses the self.DEPTHS dict as a lookup table for depth

        Args:
            name: Name of sensor

        Returns:
            Depth of sensor
        """

        _, _, depth_name = name.split("-")
        return self.DEPTHS[depth_name]

    def convert_layer(self, name : str, in_path : str, vector_file : fiona.Collection):
        """Add single sensor as layer in vector represenation

        The name of the sensor is given in the following format "VFHA-SP-D_SC"

        Args:
            name: Name of sensor
            in_path: Path to dataset
            out_path: Path to write vector data
        """

        cords = self.find_pos(name)
        depth = self.find_depth(name)

        # open file
        with open(in_path, "r", newline="") as csvfile:
            reader = csv.reader(csvfile)

            # get header
            header_row = next(reader)

            # loop over rows
            for row in reader:

                # skip over rows containing NULL
                if "NULL" in row:
                    continue

                # create vector object
                obj =  {
                    "geometry": {
                        "type": "Point",
                        "coordinates": cords,
                    },
                    "properties": {
                        "depth": depth,
                    }
                }

                # loop over columns
                for col_name, data in zip(header_row, row):

                    # handle timestamp
                    if col_name == "TimeStamp":
                        obj["properties"]["ts"] = data
                    # sensor measurements
                    else:
                        sensor_name, meas_type = col_name.split("_")
                        if sensor_name == name:
                            obj["properties"][meas_type] = data

                vector_file.write(obj)

    def convert(self, in_path : str, out_path : str):
        """Convert file at path to GeoJSON representation

        Args:
            in_path: Path to dataset csv
            out_path: Path to store GeoJSON file
        """

        #import pdb; pdb.set_trace()

        # get list of sensor names
        sensor_names = self.find_sensors(in_path)

        for idx, name in enumerate(sensor_names):
            # open geojson file
            with fiona.open(f"{name}_{out_path}", mode="w", layer=name, driver=self.driver,
                            crs=self.crs, schema=self.SCHEMA) as vectorfile:
                self.convert_layer(name, in_path, vectorfile)

