#
#   Echo server in Python
#   Binds REP socket to tcp://*:5555
#   Expects string from client, replies with the same string 1 second later
#

import time
import zmq

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5555")
MULTIPART_NEXT = False

while True:
    #  Wait for next request from client
    msg = socket.recv_multipart()
    print("Received request: ", [tmp.decode() for tmp in msg])
    time.sleep(1)
    socket.send_multipart(msg)
