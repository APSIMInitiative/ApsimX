from typing import List
from collections.abc import Callable
from numpy.typing import NDArray

import csv
import numpy as np
from datetime import datetime

from .apsim import ApsimController

# hardcoded values until we find a better way
N_LAYERS = 10
SOIL_PROFILE = "Munden:118087"

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
        """Get the vwc of each layer in the field
        
        Returns: 
            List of vwc at each layer 
        """
       
        vwc_list = [] 
    
        # loop over layers
        # NOTE: the layers are currently hardcoded as I (jtmadden173) do not know
        # how to get the available number of layers    
        for i in range(1, N_LAYERS+1):
            vwc = self.vwc_layer(i)
            vwc_list.append(vwc)
            
        return vwc_list
    
    def vwc_layer(self, layer : int=1) -> float:
        """Get the vwc of a specific soil layer of the field
        
        Args:
            layer: Soil layer. 1 indexed
            
        Returns:
            Decimal vwc format
        """
       
        # get vwc 
        sw = self.send_command("get", [f"[{self.name}].{SOIL_PROFILE}.Water.Volumetric({layer})"])
        return sw
    
    def runoff(self) -> float:
        """Water runoff from field
        
        Returns:
            Soil water runoff 
        """
        
        runoff = self.send_command("get", [f"[{self.name}].{SOIL_PROFILE}.SoilWater.Runoff"])
        return runoff
    
    def irrigate(self, depth : float, amount : float):
        """Irrigate field at depth with amount
        
        Args:
            depth: Depth below surface
            amount: Amount of water to irrigate 
        """
       
        # get field index 
        field_idx = self.name.rstrip("Field")
       
        # send irrigate command 
        self.send_command(
            "do",
            ["applyIrrigation", "amount", amount, "field", field_idx],
            unpack=False
            )

class Simulation:
    """Simulation class that preforms actions on fields"""
    def __init__(self, apsim : ApsimController, config : str):
        """Initializes the fields on apsim given a field config files
        
        Args:
            apsim: Connection to apsim server instance
            config: Path to config csv
        """
        
        self.apsim = apsim
        self.action_list = {}
       
        # create fields 
        self.fields = self.create_fields(config)
       
    
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

    def add_action(self, date : datetime, action : Callable, args : List) -> FieldNode:
        """Adds an action to take on a specific date
     
        Supported methods are:
            irrigate 
      
        Args:
            date: Date of the event
            action: Action to perform
            args: List of arguments of the action
            
        Raises:
            NotImplementedError: When action does not refer to an implemented action
        """
        
        if action in dir(self):
            if date in self.action_list:
                self.action_list[date].append([getattr(self, action), args])
            else:
                self.action_list[date] = [[getattr(self, action), args]]
        else:
            raise NotImplementedError(f"Action {action} is not implemented")

    def irrigate(self, x, y, depth : float, amount : float) -> FieldNode:
        """Irrigate a field based on its location and soil depth
       
        Args:
            x: x-coordinate
            y: y-coordinate
            depth: Depth of irrigation
            amount: Amount of irrigation
        
        Returns:
            Object that was irrigated
        """
        field = self.fields[x][y]
        field.irrigate(depth, amount)
        return field

    def vwc(self) -> NDArray:
        """Get the volumetric water content of all fields
        
        Returns:
            List of vwc at each layer for each field. The shape is formatted as
            (x, y, layer).
        """
       
        # get the vwc for each field 
        shape = self.fields.shape       
        vwc_arr = np.empty_like(self.fields, dtype=list) 
        for i in shape[0]:
            for j in shape[1]:
                vwc_arr[i][j] = self.fields[i][j].vwc()
                
        return np.array(vwc_arr)
    
    def runoff(self) -> NDArray:
        """Get the runoff for fields"""
        
        # get the vwc for each field 
        shape = self.fields.shape       
        vwc_arr = np.empty_like(self.fields, dtype=float) 
        for i in shape[0]:
            for j in shape[1]:
                vwc_arr[i][j] = self.fields[i][j].runoff()
                
        return vwc_arr
             
    def date(self) -> datetime:
        """Get the date of the simulation step
        
        Returns:
            Date in datetime format 
        """
        
        ts = self.apsim.send_command("get", ["[Clock].Today"])
        return ts
    
    def run(self) -> tuple[NDArray, NDArray]:
        """Run the simulation and return vwc history
        
        Returns:
            Arrays of dates and vwc in the format (date_arr, vwc_arr). The
            vwc_arr is a 4-dim array of each fields vwc at each step with vwc at
            each layer. The shape corresponds to (date, x, y, layer).
        """
        
        # start simulation 
        self.apsim.energize()
       
        # timestamp array 
        date_arr = []
        # volumetric water content array
        vwc_arr = []
      
        running = True
        while (running):
            # run commands
            date = self.date()
            date_arr.append(date)

            # NOTE Order does not matter between the gets and the actions.
            # Actions are added to a queue that runs on the "DoManagement" event
            # within Apsim 
            
            # call all actions specified on the date 
            if date in self.action_list:
                while self.action_list[date]:
                    action, args = self.action_list[date].pop()
                    action(*args)
            
            # get runoff        
            runoff = self.runoff()
            # loop over each element
            for i in runoff.shape[0]:
                for j in runoff.shape[1]:
                    # naive split 4 ways
                    split_runoff = runoff[i][j] / 4
                  
                    # get valid neighbors 
                    neighbors = [] 
                    # x-axis pos
                    if (i+1 > 0) and (i+1 < runoff.shape[0]):
                        neighbors.append([i+1, j])
                    # x-axis neg
                    if (i-1 > 0) and (i-1 < runoff.shape[0]):
                        neighbors.append([i-1, j])
                    # y-axis pos
                    if (j+1 > 0) and (j+1 < runoff.shape[1]):
                        neighbors.append([i, j+1])
                    # y-axis neg
                    if (j-1 > 0) and (j+1 < runoff.shape[1]):
                        neighbors.append([i, j-1])
                       
                    # irrigate neighbors 
                    for neighbor in neighbors:
                        self.irrigate(neighbor[0], neighbor[1], 0, split_runoff)
           
            # get vwc of entire field
            vwc = self.vwc()
            vwc_arr.append(vwc)
            
            # step to next date
            running = not self.apsim.step()
            
        date_arr = np.array(date_arr)
        vwc_arr = np.array(vwc_arr)

        return (date_arr, vwc_arr)
    
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