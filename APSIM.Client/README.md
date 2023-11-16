# APSIM.Client

This is a C API which can communicate with an [APSIM Server](https://apsimnextgeneration.netlify.app/usage/server) to rapidly re-run an apsim simulation with modified parameter values. Traditionally, this would be achieved by using the apsim CLI (Models.exe) but apsim's startup is (relatively) slow and the initialisation overheads often exceed the actual simulation runtimes. The apsim server will load the simulation and perform the associated initialisation once only, and thereafter the simulations can be run with almost no overhead. This is useful for tasks such as parameter optimisation, sensitivity analysis, etc.

This repository contains:

- A C library which implements the communications protocol to interact with an apsim server
- An example application which uses the library
- Unit tests suite

## Requirements

Compilation requires that GNU make/gcc and pkg-config are installed. This can be built on windows via WSL.

Building/running the unit tests requires that [`libcheck`](https://github.com/libcheck/check) is installed.

On Ubuntu/WSL:

```bash
apt update
apt install -y build-essential check pkg-config git
```

## Compilation

`make`

## Installation

`make install` (requires root)

This will install to the `/usr/local` prefix by default.

## Usage

The public API contains 5 functions:

- `connectToServer()`: Connect to a local server (via unix socket)
- `connectToRemoteServer()`: Connect to a remote server (TCP connection)
- `runWithChanges()`: Run the simulations with certain changes
- `readOutput()`: Read certain outputs from the server
- `disconnectFromServer()`: Disconnect from the server

## Docker

The repository contains a dockerfile which will build a minimal image containing the apsim client library. An official image is available on [dockerhub](https://hub.docker.com/r/hol430/apsimclient)

## Troubleshooting

### Connecting to a server running inside docker

Client connects on 127.0.0.1:$PORT

Server listens on docker 0.0.0.0:$PORT

docker run --rm -it -p $PORT:$PORT -v $inputs:/inputs apsiminitiative/apsimng-server:latest -p $PORT
