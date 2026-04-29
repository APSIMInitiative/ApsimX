import zmq
import msgpack
import subprocess
import psutil
import time
import pandas as pd

# Set up a listening server on a random port
def test_proto2(apsim_dir):
    apsim = {}
    
    # The simulation will connect back to this port:
    context = zmq.Context()
    apsim['apsim_socket'] = context.socket(zmq.REP)
    apsim['apsim_socket'].bind("tcp://0.0.0.0:0")
    apsim['random_port'] = apsim['apsim_socket'].getsockopt(zmq.LAST_ENDPOINT).decode().split(":")[-1]
	
    print("Listening on", apsim['apsim_socket'].getsockopt(zmq.LAST_ENDPOINT))
    
    apsim['process'] = subprocess.Popen([
        "/usr/bin/dotnet",
        f"{apsim_dir}/bin/Debug/net6.0/ApsimZMQServer.dll",
        "-p", apsim['random_port'],
        "-P", "interactive",
        "-f", f"{apsim_dir}/Tests/Simulation/ZMQ-Sync/ZMQ-sync.apsimx"
    ])
    
    print("Started Apsim process id", apsim['process'].pid)
    
    return apsim

# Send a command, eg resume/set/get
def send_command(socket, command, args=None):
    socket.send_string(command, zmq.SNDMORE if args else 0)
    if args:
        for i, arg in enumerate(args):
            socket.send(msgpack.packb(arg), zmq.SNDMORE if i < len(args) - 1 else 0)

# The response loop. When the simulation connects we tell it what to do.
# connect -> ok
# paused -> resume/get/set
# finished -> ok
def poll_zmq2(socket):
    numToReset = 5
    while True:
        msg = socket.recv_string()
        if msg == "connect":
            send_command(socket, "ok")
        elif msg == "paused":
            send_command(socket, "get", ["[Clock].Today.Day"])
            reply = msgpack.unpackb(socket.recv())
            print("Today = " + str(reply))
            if reply > 5 and numToReset > 0:
                send_command(socket, "do", ["do_rewind"])
                msg = socket.recv_string()
                print("Sent reset - " + msg)
                numToReset = numToReset - 1
           
            send_command(socket, "resume")
        elif msg == "finished":
            send_command(socket, "ok")
            break

def close_zmq2(apsim):
    apsim['apsim_socket'].close()
    apsim['process'].terminate()
    # process = psutil.Process(apsim['process'].pid)
    # for proc in process.children(recursive=True):
    #     proc.kill()
    # process.kill()

apsim_dir = "/home/uqpdevo1/src/ApsimX" 
apsim = test_proto2(apsim_dir)

poll_zmq2(apsim['apsim_socket'])

close_zmq2(apsim)
