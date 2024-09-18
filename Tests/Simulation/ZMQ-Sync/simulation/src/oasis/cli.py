#!/usr/bin/env python

import argparse
from dataclasses import dataclass
import os

from .apsim import ApsimController
from .plots import plot_oasis
from .config import generate_csv_from_grist, generate_data

# iteration that rain happens on
RAIN_DAY = 90
rain_day_ts = 0

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

def client(args):
    """Starts oasis client
    
    See argparser set_defaults() (https://docs.python.org/3/library/argparse.html#sub-commands) 
    """

    # initialize connection
    apsim = ApsimController(args.config, addr=args.addr, port=args.port) 

    # TODO(nubby): Integrate polling into ZMQServer object.
    ts_arr, esw1_arr, esw2_arr, rain_arr = poll_zmq(apsim)
    
    # Plot simulation.
    # TODO(nubby): Integrate irrigation with colors.
    plot_oasis(apsim)

def kraww(args):
    """Procedurally generate a CSV file of APSIM Field configs.
     
    See argparser set_defaults() (https://docs.python.org/3/library/argparse.html#sub-commands) 
    """
 
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

def entry():
    """Entry point for oasis"""
   
    # cli interface
    parser = argparse.ArgumentParser(description="OASIS Apsim Python client")
   
    subparsers = parser.add_subparsers(help="Subcommand", required=True) 
    
    client_parser = subparsers.add_parser("client", help="Runs oasis client")    
    client_parser.add_argument(
        "--addr",
        type=str,
        default="0.0.0.0",
        help="Server address"
    )
    client_parser.add_argument(
        "--port",
        type=int,
        default=27746,
        help="Server port number"
    )
    client_parser.add_argument(
        "config",
        type=str,
        help="Configuration CSV" 
    )
    client_parser.set_defaults(func=client)
    
    config_parser = subparsers.add_parser("config", help="Generates field config csv") 
    config_parser.add_argument("path", type=str, help="Path to save csv")
    config_parser.set_defaults(func=kraww)
    
    args = parser.parse_args()
    args.func(args) 

if __name__ == '__main__':
    entry()