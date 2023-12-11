import zmq
import msgpack

apsimDir = "/home/mwmaster/Documents/oasis_sim/oasis_sim"
# apsimDir = "/usr/local/lib/apsim/"


def open_zmq2(port=27746):
    context = zmq.Context()
    #  Socket to talk to server
    print("Connecting to server...")
    socket = context.socket(zmq.REQ)
    socket.connect(f"tcp://localhost:{port}")
    print('    ...connected.')
    # print(context.closed)
    # print(socket.closed)
    return context, socket


def close_zmq2(socket, port=27746):
    socket.disconnect(f"tcp://localhost:{port}")
    print('disconnected from server')


def sendCommand(socket, command):
    """ currently only work with commands represented as iterables
    of strings """

    print(f'Sending command \'{command}\' to server...')

    msg_parts = None
    msg = []
    try:
        msg_parts = len(command)
    except TypeError:
        msg_parts = 1
    for i in range(msg_parts):
        msg.append(command[i].encode())  # Python3 string.encode() default is UTF-8
    socket.send_multipart(msg)
    print('    ...command sent.')


if __name__ == '__main__':
    # initialize connection
    context, socket = open_zmq2(port=5555)
    msg = 'message for test'
    command = [msg] + ['arg1', 'arg2']
    sendCommand(socket, command)

    print('Do we get a reply?')
    reply = socket.recv_multipart()
    print('    Reply: ', [tmp.decode() for tmp in reply])

    # make a clean getaway
    # close_zmq2(socket)
    context.destroy()
