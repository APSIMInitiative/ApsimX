#!/bin/bash

# Copy and paste of commands run for future reference
# John Madden
# 2024-09-17

# Generate field template
Rscript ../../../../Tools/LocationGenerator/LocationGenerator.R

# Generate field config
../scripts/generate_fields_csv.py MetompkinFarm_config.csv

# Run client 
../ZMQ-InteractiveVariables.py MetompkinFarm_config.csv

# Run server from root directory
dotnet run --project APSIM.Server/ZMQ+msgpack/APSIM.ZMQServer.csproj --framework net6.0 -- -P interactive -f Tests/Simulation/ZMQ-Sync/toplevelsync/toplevelsync.apsimx