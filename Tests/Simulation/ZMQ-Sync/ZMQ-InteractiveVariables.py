#!/usr/bin/env python

import zmq
import msgpack

import matplotlib.pyplot as plt

# iteration that rain happens on
RAIN_DAY = 90


class ApsimController:
    """Controller for apsim server"""
    
    def __init__(self, addr="0.0.0.0", port=27746):
        """Initializes a ZMQ connection to the Apsim synchronizer
       
        Starts the command sequence by checking connect was received and sending
        "ok" back.
        
        Args:
            addr: Server address
            port: Server port number
        """

        # connection string
        self.conn_str = f"tcp://{addr}:{port}"
        self.open_socket()
       
         
        msg = self.socket.recv_string()
        print(f"recv msg: {msg}")
        if msg == "connect":
            self.send_command("ok") 
    
    def __del__(self):
        """Handles cleanup of protocol"""
        
        # TODO implement cleanup of protocol
        
        self.close_socket()
    
    def open_socket(self):
        """Opens socket to apsim ZQM synchronizer
       
        Should not be called externally. Uses self.conn_str 
        """
        
        # connect to server
        self.context = zmq.Context.instance()
        print(f"Connecting to server @ {self.conn_str}...")
        self.socket = self.context.socket(zmq.REP)
        self.socket.bind(self.conn_str)
        print('    ...connected.')    
    
    def close_socket(self):
        """Closes connection to apsim server"""
        
        self.socket.disconnect(self.conn_str)
        self.context.destroy() 

    def send_command(self, command : str, args = None):
        """Sends a string command and optional arguments to the server

        The objects in args can be any serializable type. Internally uses
        msgpack.packb for serialization.

        Args:
            socket: Opened ZMQ created socket
            command: Command string
            args: List of arguments
            
        Returns:
            Message received from server
        """
        
        # quick and dirty way dealing with this
        if args:
            self.socket.send_string(command, flags=zmq.SNDMORE)
        else:
            self.socket.send_string(command)

        # check for additional arguments
        if args:
            # list of data to send
            msg = []

            # loop over them
            for arg in args:
                # serialize using msgpack
                ser = msgpack.packb(arg)
                msg.append(ser)

            self.socket.send_multipart(msg)
           
        recv_bytes = self.socket.recv()
        recv_data = msgpack.unpackb(recv_bytes)
        return recv_data


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
    esw_arr = []
    rain_arr = []

    counter = 0

    while (True):
        msg = controller.socket.recv_string()
        print(f"recv msg: {msg}")
        if msg == "paused":
            ts = controller.send_command("get", ["[Clock].Today"])
            ts_arr.append(ts.to_unix())

            sw = controller.send_command("get", ["sum([Soil].Water.Volumetric)"])
            esw_arr.append(sw)

            rain = controller.send_command("get", ["[Weather].Rain"])
            rain_arr.append(rain)

            if counter == RAIN_DAY:
                print("Applying irrigation")
                controller.send_command("do", ["applyIrrigation", "amount", 204200.0])
                global rain_day_ts
                rain_day_ts = ts.to_unix()

            # resume simulation until next ReportEvent
            controller.send_command("resume")
        elif msg == "finished":
            controller.send_command("ok")
            break

        counter += 1

    return (ts_arr, esw_arr, rain_arr)


if __name__ == '__main__':
    # initialize connection
    
    apsim = ApsimController() 

    ts_arr, esw_arr, rain_arr = poll_zmq(apsim)

    # Code for testing with echo server
    #start_str = socket.recv_string()
    #if start_str != "connect":
    #    raise ValueError(f"Did not get correct start starting, got {start_str}, expected 'connect'")

    #sendCommand(socket, "get", ["[Manager].Script.cumsumfert"])

    #print('Do we get a reply?')
    #reply = socket.recv_multipart()
    #print(reply)

    # make plot
    plt.figure()

    plt.plot(ts_arr, esw_arr)
    plt.xlabel("Time (Unix epochs)")
    plt.ylabel("Volumetric Water Content")
    plt.axvline(x=rain_day_ts, color='red', linestyle='--', label="Rain day")

    plt.twinx()
    plt.plot(ts_arr, rain_arr, color="orange")
    plt.ylabel("Rain")

    plt.legend()
    plt.grid(True)
    plt.show()
