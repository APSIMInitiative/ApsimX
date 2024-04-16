#!/usr/bin/bash
APSIM_PATH=/home/mwmaster/Documents/oasis_sim/oasis_sim
DLL_PATH=$APSIM_PATH/bin/Debug/net6.0/ApsimZMQServer.dll
APSIMX_FILEPATH=$APSIM_PATH/Tests/Simulation/ZMQ-Sync/ZMQ-sync.apsimx
PYTHON_CLIENT_PATH=$APSIM_PATH/Tests/Simulation/ZMQ-Sync/ZMQ-InteractiveVariables.py

echo $APSIM_PATH
echo $DLL_PATH
echo $APSIMX_FILEPATH

# Options for APSIM.Server
# Required option 'f, file' 
#   -P, --protocol     (Default: oneshot) Control protocol to use - interactive or oneshot.
#   -f, --file         Required. .apsimx file to hold in memory.
#   -v, --verbose      Display verbose debugging info
#   -p, --port         (Default: 27746) Port number on which to listen for connections. 0 = choose ephemeral port
#   -a, --address      (Default: 0.0.0.0) IP Address on which to listen/connect to.
#   -c, --cpu-count    (Default: 1) Number of vCPUs per worker node
#   --help             Display this help screen.
#   --version          Display version information.

# /usr/bin/dotnet $DLL_PATH -P interactive -f $APSIMX_FILEPATH -v &
/usr/bin/dotnet run --project $APSIM_PATH/APSIM.Server/ZMQ+msgpack/APSIM.ZMQServer.csproj --framework net6.0 -- -P interactive -f $APSIMX_FILEPATH -v &

# echo $!
echo 'started APSIM server process'

echo 'starting Python3 client'
/usr/bin/python3 $PYTHON_CLIENT_PATH
echo 'exited Python3 client'

pkill $!
echo 'ping'
