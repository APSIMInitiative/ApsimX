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

import zmq
import msgpack


class ZMQServer(object):
    """Object for interacting with the OASIS 0MQ server.

    Implements a response loop to control the simulation and transfer data. The
    sequence of commands in the format (server -> client) are as follows:
        connect     -> ok
        paused      -> resume/get/set/do
        finished    -> ok

    Attributes:
        context (:obj: `zmq.Context`)
        info (:obj: `dict`): { address: str, port: str }
        socket
    """

    def __init__(self, addr: str, port: str):
        """Initialize ZMQ server.
        Initialize connection by default.

        Args:
            addr: str
            port: str
            connect: bool = True
        """
        self.info = {"address": addr, "port": port}
        self.context = zmq.Context.instance()
        self.socket = None
        self.conn_str = f"tcp://{addr}:{port}"
        self._open_socket()

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
        print("    ...connected.")

    def close_socket(self):
        """Closes connection to Apsim server."""
        self.socket.disconnect(self.conn_str)
        self.context.destroy()  # NOTE: this method is not threadsafe!! Make
        #   sure that there are not currently active
        #   threads when you call this.

    def send_command(self, command: str, args: tuple = None, unpack: bool = True):
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

    def energize(self):
        """Begin the simulation

        This is normally called after all fields are created.
        """
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
