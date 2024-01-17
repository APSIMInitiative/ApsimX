#!/usr/bin/env python

import zmq
import msgpack

import matplotlib.pyplot as plt


RAIN_DAY = 10

def open_zmq2(port=27746):
    context = zmq.Context.instance()
    #  Socket to talk to server
    print("Connecting to server...")
    print(f"Port {port}")
    socket = context.socket(zmq.REP)
    socket.bind(f"tcp://0.0.0.0:{port}")
    print('    ...connected.')
    # print(context.closed)
    # print(socket.closed)
    return context, socket


def close_zmq2(socket : zmq.Socket, port=27746):
    socket.disconnect(f"tcp://127.0.0.1:{port}")
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

    # quick and dirty way dealing with this
    if args: 
        socket.send_string(command, flags=zmq.SNDMORE)
    else:
        socket.send_string(command)
   
    # check for additional arguments 
    if args:  
    
        # list of data to send
        msg = []
    
        # loop over them
        for arg in args:
            # serialize using msgpack
            ser = msgpack.packb(arg)
            msg.append(ser)

        socket.send_multipart(msg)
    
    
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
    
    counter = 0
    
    while (True):
        msg = socket.recv_string()
        print(f"recv msg: {msg}")
        if msg == "connect":
            sendCommand(socket, "ok")
        elif msg == "paused":
            sendCommand(socket, "get", ["[Clock].Today"])
            ts = msgpack.unpackb(socket.recv())
            ts_arr.append(ts.to_unix())
            
            sendCommand(socket, "get", ["sum([Soil].SoilWater.ESW)"])
            esw = msgpack.unpackb(socket.recv())
            esw_arr.append(esw)
             
            sendCommand(socket, "get", ["[Weather].Rain"])
            rain = msgpack.unpackb(socket.recv())
            rain_arr.append(rain)
           
            if counter == RAIN_DAY:
                print("Applying irrigation")
                sendCommand(socket, "do", ["applyIrrigation", "amount", 12345913.0])
                socket.recv()
                global rain_day_ts
                rain_day_ts = ts.to_unix()
            
            # resume simulation until next ReportEvent
            sendCommand(socket, "resume")
        elif msg == "finished":
            sendCommand(socket, "ok")
            break
        
        counter += 1
        
    return (ts_arr, esw_arr, rain_arr)


if __name__ == '__main__':
    # initialize connection
    context, socket = open_zmq2()
    
    ts_arr, esw_arr, rain_arr = poll_zmq(socket)
    
    # Code for testing with echo server 
    #start_str = socket.recv_string()
    #if start_str != "connect":
    #    raise ValueError(f"Did not get correct start starting, got {start_str}, expected 'connect'")
    
    #sendCommand(socket, "get", ["[Manager].Script.cumsumfert"])

    #print('Do we get a reply?')
    #reply = socket.recv_multipart()
    #print(reply)

    plt.figure()
    plt.plot(ts_arr, esw_arr, label="ESW")
    plt.plot(ts_arr, rain_arr, label="Rain")
    plt.axvline(x=rain_day_ts, color='red', linestyle='--', label="Rain day")
    plt.xlabel("Time")
    
    plt.legend()
    plt.grid(True)
    plt.show()
    
    # make plot

    # make a clean getaway
    # close_zmq2(socket)
    context.destroy()
