---
title: "APSIM Server"
draft: false
---

## Overview

The APSIM Server holds an .apsimx file open in memory and runs it on demand, potentially with modified parameter values. Communication with the server occurs primarily via unix sockets, which results in far less overhead than repeatedly invoking Models.exe. There is currently no windows implementation of a client capable of communicating with the server, nor are there any plans to do so.

## Behaviour

When the server starts, it will read the .apsimx file, prepare the file to be run, and will then wait for client connections. If a client application is already running, the server will establish a connection with this client. Once a connection is established with a client, the server will wait for instructions from the client. Currently two commands are implemented:

- RUN (run the .apsimx file, possibly with modified parameters)
- READ (read results from the most recent run)

The recommended way to send these commands to the server is to use a client API. A sample C API is provided [here](https://github.com/APSIMInitiative/APSIM.Client).

## Notes on running/invoking the server

There is currently no binary installer provided for the server. The server is written in C#, targeting .NET Core - therefore there are two ways to invoke the server (these should be run from the ApsimX directory):

`dotnet run -p APSIM.Server -- <arguments>`

`dotnet <path/to/apsim-server.dll> <arguments>`

When using the first option, the `--` is necessary - it separates the arguments passed to the `dotnet` command, from the arguments passed to the server.

When using the second syntax, it's important to use the correct path to the apsim-server.dll file, as this path can be variable depending on how it was built. Typically, it will be located at `ApsimX/bin/Debug/netcoreapp3.1/apsim-server.dll`, but this will be different if the server was built in release mode (path will contain Release instead of Debug), and will be different again if the project was published.

## Command Line Arguments

When the server is started, several command-line arguments may be passed. These may be viewed by running the server with the `-h` or `--help` command-line arguments. The only mandatory argument is the `--file <file.apsimx>` argument, where <file.apsimx> should be the path to an .apsimx file on disk. This is the file which will be read when the server starts, and run whenever the server receives a RUN command.

The `--comunication-mode` flag tells the server which communications protocol to use for communication to a client application. Normally this should be set to `Native`, which will allow for communications with a native application (e.g. a C program). If the client program is written in C# using C# named pipes, this option may be set to `Managed`, and will allow for much simpler comms code.

The `--keep-alive` argument tells the server to continue running when a client disconnects. If this argument is not passed, then the server will exit immediately after a client disconnects.
