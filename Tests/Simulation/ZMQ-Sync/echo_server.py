#!/usr/bin/env python

#
#   Echo server in Python
#   Binds REP socket to tcp://*:5555
#   Expects string from client, replies with the same string 1 second later
#

import time
import zmq

context = zmq.Context()
socket = context.socket(zmq.REQ)
socket.bind("tcp://localhost:5555")
MULTIPART_NEXT = False

if __name__ == "__main__":
    #  Wait for next request from client
    socket.send_string("connect")
   
    msg = socket.recv_multipart() 
    
    #print("Received request: ", [tmp.decode() for tmp in msg])
    print("Received request: ")
    print(msg)
    time.sleep(1)
    socket.send_multipart(msg)
