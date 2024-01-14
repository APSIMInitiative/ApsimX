#!/usr/bin/env python

import zmq
import msgpack


def open_zmq2(port=27746):
    context = zmq.Context()
    #  Socket to talk to server
    print("Connecting to server...")
    socket = context.socket(zmq.REP)
    socket.connect(f"tcp://0.0.0.0:{port}")
    print('    ...connected.')
    # print(context.closed)
    # print(socket.closed)
    return context, socket


def close_zmq2(socket : zmq.Socket, port=27746):
    socket.disconnect(f"tcp://0.0.0.0:{port}")
    print('disconnected from server')


def sendCommand(socket : zmq.Socket, command : str, args=None):
    """Sends a string command and optional arguments to the server
    
    The objects in args can be any serializable type. Internally uses
    msgpack.packb for serialization.
    
    Args
    ----
    socket: Opened ZMQ created socket
    command: Command string
    args: List of arguments 
    """

    print(f"Sending command '{command}' to server...")
    
    socket.send_string(command, flags=zmq.SNDMORE)
    
    print("Command send")
   
    # check for additional arguments 
    if args:  
        print("Sending arguments...")
    
        # list of data to send
        msg = []
    
        # loop over them
        for arg in args:
            # serialize using msgpack
            ser = msgpack.packb(arg)
            msg.append(ser)

    socket.send_multipart(msg)
    print('    ...arguments ssent.')
    
    
def poll_zmq(socket : zmq.Socket) -> tuple:
    """Runs the simulation and obtains simulated data
   
    Implements a response loop to control the simulation and transfer data. The
    sequence of commands in the format (server -> client) are as follows:
        connect     -> ok
        paused      -> resume/get/set/do
        finished    -> ok
 
    Args
    ----
    socket: Opened ZMQ created socket
     
    Returns
    -------
    Returns a tuple of (timestamp, extractable soil water (mm), rain)
    """
    
    ts_arr = []
    esw_arr = []
    rain_arr = []
      
    while (True):
        msg = socket.recv_string()
        print(f"recv msg: {msg}")
        if msg == "connect":
            sendCommand(socket, "ok")
        elif msg == "paused":
            sendCommand(socket, "get", ["[Clock].Today"])
            ts = msgpack.unpackb(socket.recv())
            ts_arr.append(ts)
            
            sendCommand(socket, "get", ["sum([Soil].SoilWater.ESW)"])
            esw = msgpack.unpackb(socket.recv())
            esw_arr.append(esw)
            
            
            sendCommand(socket, "get", ["[Weather].Rain"])
            rain = msgpack.unpackb(socket.recv())
            rain_arr.append(rain)
            
            # print data for that day
            print(ts)
            print(f"ESW: {esw}")
            print(f"Rain: {rain}")
            
            # resume simulation until next ReportEvent
            sendCommand(socket, "resume")
        elif msg == "finished":
            sendCommand(socket, "ok")
            break
        
        return (ts_arr, esw_arr, rain_arr)


if __name__ == '__main__':
    # initialize connection
    context, socket = open_zmq2(port=5555)
    
    ts_arr, esw_arr, rain_arr = poll_zmq(socket)
   
    # Code for testing with echo server 
    #start_str = socket.recv_string()
    #if start_str != "connect":
    #    raise ValueError(f"Did not get correct start starting, got {start_str}, expected 'connect'")
    
    #sendCommand(socket, "get", ["[Manager].Script.cumsumfert"])

    #print('Do we get a reply?')
    #reply = socket.recv_multipart()
    #print(reply)

    # make a clean getaway
    # close_zmq2(socket)
    context.destroy()
