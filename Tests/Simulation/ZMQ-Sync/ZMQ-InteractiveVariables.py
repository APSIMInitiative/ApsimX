#!/usr/bin/env python

import zmq
import msgpack


def open_zmq2(port=27746):
    context = zmq.Context()
    #  Socket to talk to server
    print("Connecting to server...")
    socket = context.socket(zmq.REP)
    socket.connect(f"tcp://localhost:{port}")
    print('    ...connected.')
    # print(context.closed)
    # print(socket.closed)
    return context, socket


def close_zmq2(socket : zmq.Socket, port=27746):
    socket.disconnect(f"tcp://localhost:{port}")
    print('disconnected from server')


def sendCommand(socket : zmq.Socket, command : str, args=None):
    """Sends a string command and optional arguments to the server
    
    The objects in args can be any serializable type. Internally uses
    msgpack.packb for serialization.
    
    Args
    ----
    socket: ZeroMQ created socket
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


if __name__ == '__main__':
    # initialize connection
    context, socket = open_zmq2(port=5555)
    
    start_str = socket.recv_string()
    if start_str != "connect":
        raise ValueError(f"Did not get correct start starting, got {start_str}, expected 'connect'")
    
    sendCommand(socket, "get", ["[Manager].Script.cumsumfert"])

    print('Do we get a reply?')
    reply = socket.recv_multipart()
    print(reply)

    # make a clean getaway
    # close_zmq2(socket)
    context.destroy()
