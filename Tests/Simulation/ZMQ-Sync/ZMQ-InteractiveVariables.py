#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""OASIS Apsim Python client.

Welcome to the Python client for the OASIS project!! This module communicates
with a corresponding Apsim client via a ZMQ server. 

Example:
    $ python3 ZMQ-InteractiveVariables.py

Todo:
    * Create a 3D visual.
    * Make the ZMQServer object run as part of its own thread, reading from a
        dedicated queue.
"""
import csv
import zmq
import msgpack
import time

import matplotlib.pyplot as plt
from matplotlib.figure import Axes, Figure
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
import numpy as np
    
import pdb
import argparse

# iteration that rain happens on
RAIN_DAY = 90
rain_day_ts = 0


class ZMQServer(object):
    """Object for interacting with the OASIS 0MQ server.
    
    Attributes:
        context (:obj: `zmq.Context`)
        info (:obj: `dict`): { address: str, port: str } 
        socket
    """
    def __init__(
            self,
            addr: str,
            port: str
        ):
        """Initialize ZMQ server.
        Initialize connection by default.

        Args:
            addr: str
            port: str
            connect: bool = True
        """
        self.info = {
            "address": addr,
            "port": port
        }
        self.context = zmq.Context.instance()
        self.socket = None
        self.conn_str = f"tcp://{addr}:{port}"
        self._open_socket();

    def __del__(self):
        """Handles cleanup of protocol"""
        # TODO implement cleanup of protocol, something to restart sim
        self.close_socket()
    
    def _open_socket(self):
        """Opens socket to Apsim ZMQ Synchroniser."""
        # connect to server
        print(f"Connecting to server @ {self.conn_str}...")
        self.socket = self.context.socket(zmq.REP)
        self.socket.bind(self.conn_str)
        print('    ...connected.')    
    
    def close_socket(self):
        """Closes connection to Apsim server."""
        self.socket.disconnect(self.conn_str)
        self.context.destroy()  # NOTE: this method is not threadsafe!! Make 
                                #   sure that there are not currently active
                                #   threads when you call this.
                                
    def send_command(
            self,
            command : str,
            args : tuple = None,
            unpack : bool = True
        ):
        """Sends a string command and optional arguments to the server.

        The objects in args can be any serializable type. Internally uses
        msgpack.packb for serialization.

        Args:
            command (str): Command string
            args (:obj:`list` of :obj:`str`): List of arguments
            unpack (bool): Unpack the resulting bytes

        Returns:
            Bytes received from the server. If unpack is True, then the data is
            decoded in a python object. Else if unpack is False the raw bytes is
            returned.
        """
        # check for arguments, otherwise just send string
        if args:
            self.socket.send_string(command, flags=zmq.SNDMORE)
            # list of data to send
            msg = []
            # loop over them; serialize using msgpack.
            [msg.append(msgpack.packb(arg)) for arg in args]
            self.socket.send_multipart(msg)
        else:
            self.socket.send_string(command)
            
        # get the response
        recv_bytes = self.socket.recv()
        # process based on arg
        if unpack:
            recv_data = msgpack.unpackb(recv_bytes)
        else:
            recv_data = recv_bytes

        # return data
        return recv_data
    

class FieldNode(object):
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


class ApsimController:
    """Controller for apsim server.

    Attributes:
        fields (:obj:`list` of :obj:`FieldNode`): List of Fields in simulation.
    """
    def __init__(self, path, addr="0.0.0.0", port=27746):
        """Initializes a ZMQ connection to the Apsim synchronizer
       
        Starts the command sequence by checking connect was received and sending
        "ok" back.
        
        Args:
            fields_configs: Path t
            addr (str): Server address
            port (str): Server port number
        """
        self.fields = []
        self.server = ZMQServer(addr, port)
        # Aliases.
        self.send_command = self.server.send_command
        self.socket = self.server.socket
        
        # initialize the connection protocol
        msg = self.socket.recv_string()
        print(f"recv msg: {msg}")
        
        if msg != "connect":
            # TODO error handling
            pass
            
        msg = self.send_command("ok", unpack=False)
        
        if msg != "setup":
            # TODO error handling
            pass
        
        # Create all your Fields here.
        """
        Format (dict[list]): [{
            "Name": "(str)",
            "Radius": "(float)",
            "SW": "(float)",
            "X": "(float)",
            "Y": "(float)",
            "Z": "(float)"
            }...]
        """
        field_configs = read_csv_file(path)
        [
            self.fields.append(
                FieldNode(
                    server=self.server,
                    configs=config
                )
            ) for config in field_configs
        ]
        [print(field) for field in self.fields]
        # Begin the simulation.
        msg = self.send_command("energize", [], unpack=False)

    def step(self) -> bool:
        """Steps the simulation to the next timestep
        
        Returns:
            Status if simulation has finished. True if simulation is done. False
            if it is still running and can be stepped.
            
        Raises:
            ValueError: When a response the server doesn't match a expected string
        """

        rc = None 
        
        resp_bytes = self.send_command("resume", unpack=False)
        resp = resp_bytes.decode("utf-8")
        if resp == "paused":
            rc = False
        elif resp == "finished":
            rc = True
        else:
            raise ValueError
        return rc


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


## MEAT.
def poll_zmq(controller : ApsimController) -> tuple:
    """Runs the simulation and obtains simulated data

    Implements a response loop to control the simulation and transfer data. The
    sequence of commands in the format (server -> client) are as follows:
        connect     -> ok
        paused      -> resume/get/set/do
        finished    -> ok

    Args:
        controller: Reference to apsim controller

    Returns:
        Returns a tuple of (timestamp, extractable soil water (mm), rain)
    """

    ts_arr = []
    esw1_arr = []
    esw2_arr = []
    rain_arr = []

    counter = 0
    
    running = True

    while (running):
        ts = controller.send_command("get", ["[Clock].Today"])
        ts_arr.append(ts.to_unix())
        
        sw1 = controller.send_command("get", ["sum([Field1].Munden:118087.Water.Volumetric)"])
        esw1_arr.append(sw1)
        
        sw2 = controller.send_command("get", ["sum([Field2].Munden:118087.Water.Volumetric)"])
        esw2_arr.append(sw2)

        rain = controller.send_command("get", ["[Weather].Rain"])
        rain_arr.append(rain)

        if counter == RAIN_DAY:
            print("Applying irrigation")
            controller.send_command(
                "do",
                ["applyIrrigation", "amount", 204200.0, "field", 0],
                unpack=False
                )
            global rain_day_ts
            rain_day_ts = ts.to_unix()

        # resume simulation until next ReportEvent
        running = not controller.step()

        # increment counter
        counter += 1

    return (ts_arr, esw1_arr, esw2_arr, rain_arr)


def plot_field_geo(ax: Axes, field: FieldNode, color="g"):
    """Add a the physical dimensions of a field as a subplot in a figure.

    Args:
        ax      (:obj:`Axes`)
        fig     (:obj:`Figure`)
        field   (:obj:`FieldNode`)
        color   (:obj:`Optional[str]`)  Field color.
    """
    [x_coord, y_coord, z_coord] = [float(coord) for coord in field.coords]
    r = float(field.radius)

    # Define the vertices.
    x_min = x_coord - r
    y_min = y_coord - r
    z_min = z_coord - r
    x_max = x_coord + r
    y_max = y_coord + r
    z_max = z_coord + r

    vertices = np.array([
        [x_min, y_min, z_min],
        [x_max, y_min, z_min],
        [x_max, y_max, z_min],
        [x_min, y_max, z_min],
        [x_min, y_min, z_max],
        [x_max, y_min, z_max],
        [x_max, y_max, z_max],
        [x_min, y_max, z_max]
    ])

    # Define the faces of the rectangular prism.
    faces = [
        [vertices[v] for v in [0, 1, 2, 3]],    # Bottom face.
        [vertices[v] for v in [4, 5, 6, 7]],    # Top face.
        [vertices[v] for v in [0, 1, 5, 4]],    # Front face.
        [vertices[v] for v in [2, 3, 7, 6]],    # Back face.
        [vertices[v] for v in [0, 3, 7, 4]],    # Left face.
        [vertices[v] for v in [1, 2, 6, 5]]     # Right face.
    ]

    rprism = Poly3DCollection(
        faces,
        facecolors=color,
        alpha=0.5,
        edgecolors="black",
        linewidths=1
    )
    ax.add_collection3d(rprism)

def get_sphere_coords(field: FieldNode, u, v):
    """Let's get parametric.
    """
    h2o_scaler = 1 
    center = [float(coord) for coord in field.coords]
    radius = h2o_scaler * float(field.v_water)
    x = center[0] + radius * np.sin(v) * np.cos(u)
    y = center[1] + radius * np.sin(v) * np.sin(u)
    z = center[2] + radius * np.cos(v)
    return x, y, z

def plot_field_h2o(ax: Axes, field: FieldNode, color="c"):
    """Add a field's water content as a spherical subplot in a figure.

    Args:
        ax      (:obj:`Axes`)
        field   (:obj:`FieldNode`)
    """
    [x_coord, y_coord, z_coord] = [float(coord) for coord in field.coords]
    r = float(field.v_water)
    u = np.linspace(0, 2 * np.pi, 100)
    v = np.linspace(0, np.pi, 100)
    u, v = np.meshgrid(u, v)
    x, y, z = get_sphere_coords(field, u, v)
    ax.plot_surface(
        x,
        y,
        z,
        color=color,
        alpha=0.2
    )

def plot_field(geox: Axes, h2ox: Axes, field: FieldNode):
    """Add a field as a subplot in a figure.

    Args:
        geox    (:obj:`Axes`)   Axes for geospatial depiction.
        h2ox    (:obj:`Axes`)   Axes for H2O volume depiction.
        field   (:obj:`FieldNode`)
    """
    plot_field_geo(geox, field)
    plot_field_h2o(h2ox, field)

def _format_geo_plot(
        ax: Axes,
        x: list[float],
        y: list[float],
        z: list[float],
        margin: float = 3.0
    ):
    """
    ax: (:obj:`Axes`)
    """
    ax.set_xlabel("X [acres]")
    ax.set_ylabel("Y [acres]")
    ax.set_zlabel("Z [acres]")
    ax.set_xlim([min(x) - margin, max(x) + margin])
    ax.set_ylim([min(y) - margin, max(y) + margin])
    ax.set_zlim([min(z) - margin, max(z) + margin])

def _format_h2o_plot(
        ax: Axes,
        x: list[float],
        y: list[float],
        z: list[float],
        margin: float = 3.0
    ):
    """
    ax: (:obj:`Axes`)
    """
    ax.set_xlabel("X [gal]")
    ax.set_ylabel("Y [gal]")
    ax.set_zlabel("Z [gal]")
    ax.set_xlim([min(x) - margin, max(x) + margin])
    ax.set_ylim([min(y) - margin, max(y) + margin])
    ax.set_zlim([min(z) - margin, max(z) + margin])

def plot_oasis(controller: ApsimController):
    """Generate a 3D plot representation of an OASIS simulation.

    Args:
        controller (:obj:`ApsimController`)
    """
    fig = plt.figure(figsize=(10, 8))
    geox = fig.add_subplot(211, projection="3d")
    h2ox = fig.add_subplot(212, projection="3d")
    x, y, z = [], [], []
    for field in controller.fields:
        plot_field(geox, h2ox, field)
        [x_coord, y_coord, z_coord] = field.coords
        x.append(float(x_coord))
        y.append(float(y_coord))
        z.append(float(z_coord))

    _format_geo_plot(geox, x, y, z)
    _format_h2o_plot(h2ox, x, y, z)

    geox.set_title(f"Sample Apsim Field Positions")
    h2ox.set_title(f"Sample Apsim Field Total H2O Volumes")
    plt.tight_layout()
    plt.show()

if __name__ == '__main__': 
    # cli interface
    parser = argparse.ArgumentParser(description="OASIS Apsim Python client")
    parser.add_argument(
        "--addr",
        type=str,
        default="0.0.0.0",
        help="Server address"
    )
    parser.add_argument(
        "--port",
        type=int,
        default=27746,
        help="Server port number"
    )
    parser.add_argument(
        "config",
        type=str,
        help="Configuration CSV" 
    )
    args = parser.parse_args()
    
    # initialize connection
    apsim = ApsimController(args.config, addr=args.addr, port=args.port) 

    # TODO(nubby): Integrate polling into ZMQServer object.
    ts_arr, esw1_arr, esw2_arr, rain_arr = poll_zmq(apsim)
    
    # Plot simulation.
    # TODO(nubby): Integrate irrigation with colors.
    plot_oasis(apsim)
