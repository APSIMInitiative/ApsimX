import zmq
import msgpack

apsimDir = "/home/mwmaster/Documents/oasis_sim/oasis_sim"
# apsimDir = "/usr/local/lib/apsim/"


def open_zmq2(port=27746):
    context = zmq.Context()
    #  Socket to talk to server
    print("Connecting to APSIM server...")
    socket = context.socket(zmq.REQ)
    socket.connect(f"tcp://localhost:{port}")
    print('    ...connected.')
    # print(context.closed)
    # print(socket.closed)
    return context, socket

    
def close_zmq2(socket, port=27746):
    socket.disconnect(f"tcp://localhost:{port}")
    print('disconnected from APSIM Server')
    
    
def sendCommand(socket, command, args=None):
    print(f'Sending command \'{command}\'to server...')
    # MWM: I did not verify that send.more flagging is right 
    socket.send_string(command)
    print('    ...command sent. \nSending args...')
    if args is not None:
        for i in range(len(args)):
            socket.send(msgpack.pack(args[i]))
    print('    ...args sent.')


# def poll_zmq(socket):
#     while True:
#         msg = socket.recv()
#         if msg == 'connect':
#             sendCommand(socket, 'ok')
#         elif msg == 'paused':
            
#     pass

    
if __name__ == '__main__':
    # initialize connection
    context, socket = open_zmq2()
    
    sendCommand(socket, 'version')
    
    reply = socket.recv()
    print(reply)

    # make a clean getaway
    close_zmq2(socket)
    context.destroy()
    