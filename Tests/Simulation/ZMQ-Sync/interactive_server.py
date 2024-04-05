#!/usr/bin/env python

import argparse
import zmq

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--addr", default="127.0.0.1",
                        help="the address to bind the server to")
    parser.add_argument("--port", default=27746,
                        help="the port to bind the server to")
    args = parser.parse_args()

    addr = args.addr
    port = args.port

    # Create a new socket
    context = zmq.Context()
    socket = context.socket(zmq.REP)
    socket.bind(f"tcp://{addr}:{port}")
    print("Connected")

    while True:
        # Receive response from the server
        response = socket.recv_string()

        # Print the response
        print(response)
        
        # Get user input
        user_input = input()

        # Send user input over the socket
        socket.send_string(user_input)