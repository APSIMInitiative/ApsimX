import csv
import fiona
import os

def metompkin_to_geojson(
    inp : str,
    out : str,
    loc : dict[str, tuple[float, float]],
    depth : dict[str, float],
    overwrite : bool=True
):
    """Convert metompkin dataset to geojson format based on locations of each
    sensors.

    Locations are specified in WGS84 format in (lon, lat) format. loc is
    a dictionary in the following format:
    {
        "VFHA": (123.45, 67.89),
        ...
    }

    Depths are specified in meters. depth is a dictionary in teh following
    format:
    {
        "S": 0.1,
        "D": 0.3
    }

    Args:
        inp: Path to metompkin farm dataset csv
        out: Output path to write geojson file
        loc: Locations information of sensors
        depth: Depth information of sensors
        overwrite: Overwrite out file
    """

    if overwrite:
        out_opts = "w"
    else:
        out_opts = "x"

    # open csv file
    with open(inp, "r", newline="", encoding="utf-8") as csvfile:
        inp_reader = csv.reader(csvfile)

        # specify schema
        schema= {
            "geometry": "Point",
            "properties": dict([
                ("name", "str"),
                ("depth", "float"),
                ("sc", "float"),
                ("st", "float"),
                ("wc", "float"),
            ])
        }

