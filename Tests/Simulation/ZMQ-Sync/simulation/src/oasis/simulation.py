from typing import List
from numpy.typing import NDArray

import csv
import numpy as np

from .apsim import ApsimController

class FieldNode:
    """A single Field, corresponding to a node in the OASIS sim.
    
    Create a new Field that is linked to its instance in APSIM.
    
    Attributes:
        id (int): Location (index) within Apsim list.
        info (dict): Includes the following keys:
            { "X", "Y", "Z", "Radius", "WaterVolume", "Name" }
        
    """
    def __init__(
            self,
            server,
            configs: dict = {}
        ):
        """
        Args:
            configs (:obj: `dict`, optional): Pre-formatted field configs; see
                [example_url.com] for supported configurations.

        TODO:
            * Replace XYZ with GPS data and elevation/depth?
        """
        self.id = None
        self.info = {}
        for key in ["Name", "SW", "X", "Y", "Z"]:
            self.info[key] = configs[key]
        # TODO(nubby): Make the radius/area settings better.
        self.info["Area"] = str((float(configs["Radius"]) * 2)**2)

        self.coords = [configs["X"], configs["Y"], configs["Z"]]
        self.radius = configs["Radius"]
        self.name = configs["Name"]
        self.v_water = configs["SW"]

        # Aliases.
        self.socket = server.socket
        self.send_command = server.send_command
        self.create()

    def __repr__(self):
        return "{}: {} acres, {} gal H2O; @({},{},{})".format(
            self.info["Name"],
            self.info["SW"],
            self.info["Area"],
            self.info["X"],
            self.info["Y"],
            self.info["Z"]
        )


    def digest_configs(
            self,
            fpath: str
        ):
        """Import configurations from a CSV file.
        Args:
            fpath (str): Relative path of file.
        """
        pass

    def _format_configs(self):
        """Prepare FieldNode configs for creating a new Field in Apsim.
        Returns:
            csv_configs (:obj:`list` of :obj:`str`): List of comma-separated
                key-value pairs for each configuration provided.
        """
        return ["{},{}".format(key, val) for key, val in self.info.items()]

    def create(self):
        """Create a new field and link with ID reference returned by Apsim."""
        csv_configs = self._format_configs()
        self.id = int.from_bytes(
            self.send_command(
                command="field",
                args=csv_configs,
                unpack=False
            ),
            "big",
            signed=False
        )
        
    def vwc(self) -> List[float]:
        """Get the vwc of each layer in the field"""
        pass
    
    def vwc_layer(self, layer : int=0) -> float:
        """Get the vwc of a specific soil layer of hte field"""
        pass

class Simulation:
    """Simulation class that preforms actions on fields"""
    def __init__(self, apsim : ApsimController, config : str):
        """Initializes the fields on apsim given a field config files
        
        Args:
            apsim: Connection to apsim server instance
            config: Path to config csv
        """
        
        self.apsim = apsim
       
        # create fields 
        self.fields = self.create_fields(config)
       
        # start simulation 
        self.apsim.energize()
    
    def create_fields(self, config : str) -> NDArray:
        """Create new fields on the server given a config
        
        The config csv is read in the following format:
         
        Format (dict[list]): [{
            "Name": "(str)",
            "Radius": "(float)",
            "SW": "(float)",
            "X": "(float)",
            "Y": "(float)",
            "Z": "(float)"
            }...]
         
        Args:
            config: Path to field configuration
        
        Returns:
            Numpy array where (x,y) location is the index of the field
        """
        
        field_configs = read_csv_file(config)
     
        # calculate the shape of the grid of fields 
        shape_x = 0
        shape_y = 0 
        for config in field_configs:
            shape_x = max(shape_x, config["X"])
            shape_y = max(shape_y, config["Y"]) 
       
        # create 2d array of fields 
        fields = np.empty((shape_x, shape_y), dtype=FieldNode)
        for config in field_configs:
            field = FieldNode(server=self.apsim, configs=config)
            fields[config["X"]][config["Y"]] = field 
        
        return fields

    def irrigate_idx(self, idx : int, amount : float) -> FieldNode:
        """Irrigate a field based on its index
       
        Args:
            idx: Field index
            amount: Amount of irrigation
            
        Returns:
            Object that was irrigated
        """
        pass

    def irrigate_loc(self, x, y, depth : float, amount : float) -> FieldNode:
        """Irrigate a field based on its location and soil depth
       
        Args:
            x: x-coordinate
            y: y-coordinate
            depth: Depth of irrigation
            amount: Amount of irrigation
        
        Returns:
            Object that was irrigated
        """
        pass

    def vwc(self):
        """Get the volumetric water content of all fields"""
        pass
    
    def runoff(self):
        """Get the runoff for fields"""
        pass
    
    
# Function decs.
## Helpers.
def read_csv_file(fpath: str) -> list[dict]:
    data = []
    print(f"Reading from {fpath}...")
    with open(fpath, "r+") as csvs:
        reader = csv.DictReader(csvs)
        for row in reader:
            data.append(row)
    print(f"    DONE")
    if (not data):
        print(f"WARNING!! {fpath} is an empty file!")
    return data