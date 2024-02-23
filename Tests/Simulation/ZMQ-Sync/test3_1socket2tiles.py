#!/usr/bin/env python

import zmq
import msgpack
import pdb

import numpy as np
import matplotlib.pyplot as plt

FIELDS = 2
LAYERS = 7

RAIN_DAY = 90
RAIN_LOC = 0
RUNOFF_FLAGS = [False, False]


def open_zmq2(ports=27746):
    #  Socket to talk to server
    context = zmq.Context.instance()
    print("Connecting to server...")
    print(f"Port {ports}")
    socket = context.socket(zmq.REP)
    socket.bind(f"tcp://0.0.0.0:{ports}")
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


def poll_zmq(ret, fields):
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
    _, socket = ret

    ts_arrs = []
    sw_arrs = []
    rain_arrs = []
    runoff_arrs = []

    counter = [0]*fields
    # pdb.set_trace()
    while (True):
        ts_arr = []
        sw_arr = []
        rain_arr = []
        runoff_arr = []

        msg = socket.recv_string()
        # print(f"recv msg: {msg}")
        if msg == "connect":
            sendCommand(socket, "ok")
            continue
        elif msg == "paused":
            sendCommand(socket, "get", ["[Clock].Today"])
            ts = msgpack.unpackb(socket.recv())
            ts_arr.append(ts.to_unix())

            if counter[RAIN_LOC] == RAIN_DAY and i == RAIN_LOC:
                # print("Our rainy day.")
                sendCommand(socket, "do", ["applyIrrigation", "amount", 80.27, "location", RAIN_LOC+1])
                socket.recv()
                global rain_day_ts
                rain_day_ts = ts.to_unix()

            sendCommand(socket, "get", ["[Weather].Rain"])
            rain = msgpack.unpackb(socket.recv())
            rain_arr.append(rain)

            for i in range(fields):
                tmp = []
                for layer in range(LAYERS):
                    sendCommand(socket, "get", [f"[Field{i+1}].Soil.Water.Volumetric[{layer+1}]"])
                    sw = msgpack.unpackb(socket.recv())
                    # print(f'field {i}, vwc @ layer {layer+1}: {sw}')
                    tmp.append(sw)
                sw_arr.append(tmp)

                sendCommand(socket, "get", [f"[Field{i+1}].Soil.Runoff"])
                runoff = msgpack.unpackb(socket.recv())  # unknown units on runoff
                print(f'field {i}, runoff: {runoff}')
                runoff_arr.append(runoff)
                if runoff > 0:
                    RUNOFF_FLAGS[i] = True

                if RUNOFF_FLAGS[(i-1)**2]:
                    # print(f'Field {i+1} runoff event', end=' ')
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
    ports = 27747
    ret = open_zmq2(ports=ports)

    ts_arr, sw_arr, rain_arr, runoff_arr = poll_zmq(ret, FIELDS)
    print(ts_arr.shape, sw_arr.shape, rain_arr.shape)

    # Code for testing with echo server
    #start_str = socket.recv_string()
    #if start_str != "connect":
    #    raise ValueError(f"Did not get correct start starting, got {start_str}, expected 'connect'")

    #sendCommand(socket, "get", ["[Manager].Script.cumsumfert"])

    #print('Do we get a reply?')
    #reply = socket.recv_multipart()
    #print(reply)
    fig, ax = plt.subplots(4,2, sharex=True, figsize=(10, 10))
    plt.tight_layout()
    for i in range(LAYERS):
        ax[i%4,i//4].plot(ts_arr[:,0], sw_arr[:,0,i], label='vwc_field1')
        ax[i%4,i//4].plot(ts_arr[:,1], sw_arr[:,1,i], label='vwc_field2')
        ax[i%4,i//4].plot(ts_arr[:,0], sw_arr[:,0,i]-sw_arr[:,1,i], label='vwc_diff')
        ax[i%4,i//4].axvline(x=rain_day_ts, color='red', linestyle='--', label="Rain day")
        ax[i%4,i//4].set_ylim(bottom=-0.1, top=1)
        ax[i%4,i//4].set_ylabel("VWC")
        ax[i%4,i//4].set_title(f'Layer {i+1}')
        ax[i%4,i//4].grid(True)

    ax[3,1].scatter(ts_arr[:,0], runoff_arr[:,0], s=2, label='sr_field1')
    ax[3,1].plot(ts_arr[:,0], runoff_arr[:,0], alpha=0.3)
    ax[3,1].scatter(ts_arr[:,1], runoff_arr[:,1], s=2, label='sr_field2')
    ax[3,1].plot(ts_arr[:,1], runoff_arr[:,1], alpha=0.3)
    ax[3,1].scatter(ts_arr[:,0], runoff_arr[:,0]-runoff_arr[:,1], c='g', s=2, label='runoff_diff')
    ax[3,1].plot(ts_arr[:,0], runoff_arr[:,0]-runoff_arr[:,1],'g', alpha=0.5)
    ax[3,1].axvline(x=rain_day_ts, color='red', linestyle='--', label="Rain day")
    ax[3,1].set_xlabel("Time (Unix Epochs)")
    ax[3,0].set_xlabel("Time (Unix Epochs)")
    ax[3,1].set_ylabel("Runoff (mm)")


    # plt.twinx()
    # plt.plot(ts_arr, rain_arr, color="orange")
    # plt.ylabel("Rain")

    ax[0,0].legend()
    ax[3,1].legend()
    ax[3,1].grid(True)
    plt.show()

    # make plot

    # make a clean getaway
    # close_zmq2(socket)
    for context, socket in ret:
        context.destroy()
