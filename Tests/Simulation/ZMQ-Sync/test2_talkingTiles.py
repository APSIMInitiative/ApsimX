#!/usr/bin/env python

import zmq
import msgpack
import pdb

import numpy as np
import matplotlib.pyplot as plt


RAIN_DAY = 90
RAIN_LOC = 0
RUNOFF_FLAGS = [False, False]


def open_zmq2(ports=[27746]):
    #  Socket to talk to server
    ret = []
    for port in ports:
        context = zmq.Context.instance()
        print("Connecting to server...")
        print(f"Port {port}")
        socket = context.socket(zmq.REP)
        socket.bind(f"tcp://0.0.0.0:{port}")
        print('    ...connected.')
        ret.append([context, socket])
    # print(context.closed)
    # print(socket.closed)
    return ret


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


def poll_zmq(sockets):
    """Runs the simulation and obtains simulated data

    Implements a response loop to control the simulation and transfer data. The
    sequence of commands in the format (server -> client) are as follows:
        connect     -> ok
        paused      -> resume/get/set/do
        finished    -> ok

    Args
    ----
    sockets: list of opened ZMQ created sockets

    Returns
    -------
    Returns a tuple of (timestamp, extractable soil water (mm), rain)
    """

    ts_arrs = []
    sw_arrs = []
    rain_arrs = []
    runoff_arrs = []

    counter = [0]*len(sockets)
    pdb.set_trace()
    while (True):
        ts_arr = []
        sw_arr = []
        rain_arr = []
        runoff_arr = []
        for i, ret in enumerate(sockets):
            _, socket = ret
            msg = socket.recv_string()
            print(f"recv msg: {msg}")
            if msg == "connect":
                sendCommand(socket, "ok")
                continue
            elif msg == "paused":
                sendCommand(socket, "get", ["[Clock].Today"])
                ts = msgpack.unpackb(socket.recv())
                ts_arr.append(ts.to_unix())

                sendCommand(socket, "get", ["sum([Soil].Water.Volumetric)"])
                sw = msgpack.unpackb(socket.recv())
                sw_arr.append(sw)

                if counter[RAIN_LOC] == RAIN_DAY and i == RAIN_LOC:
                    print("Our rainy day.")
                    sendCommand(socket, "do", ["applyIrrigation", "amount", 80.27])
                    socket.recv()
                    global rain_day_ts
                    rain_day_ts = ts.to_unix()

                sendCommand(socket, "get", ["[Weather].Rain"])
                rain = msgpack.unpackb(socket.recv())
                rain_arr.append(rain)

                sendCommand(socket, "get", ["[Soil].SoilWater.Runoff"])
                runoff = msgpack.unpackb(socket.recv())  # unknown units on runoff
                runoff_arr.append(runoff)
                if runoff > 0:
                    RUNOFF_FLAGS[i] = True

                if RUNOFF_FLAGS[(i-1)**2]:
                    print(f'Field {i+1} runoff event', end=' ')
                    sendCommand(socket, "do", ["applyIrrigation", "amount", runoff])
                    socket.recv()
                    RUNOFF_FLAGS[(i-1)**2] = False

                # resume simulation until next ReportEvent
                sendCommand(socket, "resume")
            elif msg == "finished":
                sendCommand(socket, "ok")
                return (np.array(ts_arrs), np.array(sw_arrs), np.array(rain_arrs), np.array(runoff_arrs))

            counter[i] += 1
        if msg != "connect":
            ts_arrs.append(ts_arr)
            sw_arrs.append(sw_arr)
            rain_arrs.append(rain_arr)
            runoff_arrs.append(runoff_arr)

    # return (np.array(ts_arrs), np.array(sw_arrs), np.array(rain_arrs))


if __name__ == '__main__':
    # initialize connection
    ports = [27746, 27747]
    ret = open_zmq2(ports=ports)

    ts_arr, sw_arr, rain_arr, runoff_arr = poll_zmq(ret)
    # print(ts_arr.shape, sw_arr.shape, rain_arr.shape)

    # Code for testing with echo server
    #start_str = socket.recv_string()
    #if start_str != "connect":
    #    raise ValueError(f"Did not get correct start starting, got {start_str}, expected 'connect'")

    #sendCommand(socket, "get", ["[Manager].Script.cumsumfert"])

    #print('Do we get a reply?')
    #reply = socket.recv_multipart()
    #print(reply)
    fig, ax = plt.subplots(2,1, sharex=True)
    ax[0].plot(ts_arr[:,0], sw_arr[:,0], label='sw_field1')
    ax[0].plot(ts_arr[:,1], sw_arr[:,1], label='sw_field2')
    ax[0].plot(ts_arr[:,0], sw_arr[:,0]-sw_arr[:,1], label='sw_diff')
    ax[0].axvline(x=rain_day_ts, color='red', linestyle='--', label="Rain day")

    ax[1].plot(ts_arr[:,0], runoff_arr[:,0], ':',label='runoff_field1')
    ax[1].plot(ts_arr[:,1], runoff_arr[:,1], ':', label='runoff_field2')
    ax[1].plot(ts_arr[:,0], runoff_arr[:,0]-runoff_arr[:,1], label='runoff_diff')
    ax[1].axvline(x=rain_day_ts, color='red', linestyle='--', label="Rain day")
    ax[1].set_xlabel("Time (Unix Epochs)")
    ax[0].set_ylabel("Volumetric Water Content")
    ax[1].set_ylabel("Runoff (mm)")

    # plt.twinx()
    # plt.plot(ts_arr, rain_arr, color="orange")
    # plt.ylabel("Rain")

    ax[0].legend()
    ax[1].legend()
    ax[0].grid(True)
    ax[1].grid(True)
    plt.show()

    # make plot

    # make a clean getaway
    # close_zmq2(socket)
    for context, socket in ret:
        context.destroy()
