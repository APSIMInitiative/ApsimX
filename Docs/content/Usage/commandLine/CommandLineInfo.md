---
title: "Command Line Info"
draft: false
---

# Using ApsimX command line to create, run and edit apsimx files.

Apsim Next Gen can be used to do your normal functions as you would from the full version.

## How to run apsim next gen from the command line?

Navigate to where you have apsim next gen installed, on Windows it's installed by default under: 

```
C:\Program Files\APSIM<Version-number>\bin
```

<small> replace \<version-number\> with your current version number. </small>

Use: 

```
Models.exe --apply <configFileName.txt>
``` 
<strong style="color:red"> Note: Above command can only be used when load is the first command in the configFile. </strong>

or

Use: 

```
Models.exe <pathToApsimxFile> --apply <configFileName.txt> 
```

## What is a config file?

A config file is simply a text file with a simple command on each line.

## What commands can you perform?

- save
    - ```save C:\TestSims\wheatTest.apsimx```
- load
    - ```load C:\TestSims\wheatTest.apsimx```
- run
    - This requires no further text. It will run the apsimx file loaded with a load command.
- add
    - ```add [ParentNodeName] childNodeName```
    - ```add [ParentNodeName] exampleApsimFile.apsimx;[nodeName]```
        - note: the apsimx file path must be relative to the configFile path.
- delete
    - ```delete [ParentNodeName].childNodeName```
- copy
    - ```copy [NodeName] [AnotherNodesName]```
- duplicate
    - ```duplicate [NodeName].ChildNodeName.ChildNodeName NewName```
- You can also edit a node's properties by using the node's name followed by a period-separated path to the property you would like to change followed by a equals sign and the new value. E.g. ```[Weather].FileName=Dalby.met```
    - paths must be relative to the location of the configFile.
- you can comment out command lines by using ```#``` or ```/```
    - note: must be at the start of the line. 

## What does an example config file look like?
```
# Makes a new apsimx file and builds it up to something that will run
# Dalby.met needs to be in C:\TestSims\

load C:\TestSims\minimalSim.apsimx

# Add various nodes to the Simulations node.
add [Simulations] Simulation
add [Simulations] Experiment
add [Simulation] Summary
add [Simulation] Clock
add [Simulation] Weather

# Change data in various nodes.
[Weather].FileName=Dalby.met
[Clock].Start=1900/01/01
[Clock].End=1900/01/31

# Makes a copy of the simulation named Simulation and moves it to the Experiment node named Experiment.
copy [Simulation] [Experiment]

# Duplicate the simulation in the experiment
duplicate [Experiment].Simulation

# Deletes the original Simulation.
delete [Simulations].Simulation

# Saves the Simulation to new file.
save C:\TestSims\newMinimalSim.apsimx

# Runs the simulation.
run
```

## Changing log verbosity
- This can be done using the `--log` switch.
- This is helpful for reducing database file sizes when running simulations on cloud infrastructure. The best argument type for reducing the file size is `error`.
- Acceptable arguments:
    - error
    - warning
    - information
    - diagnostic
    - all 

### Example
` Models.exe example.apsimx --log error`

## Use in memory database
- This uses a in memory database rather than database files for running simulations.
- This can be done using the `--in-memory-db` switch.
- A database file is still created however no data will be stored to the database file.

### Example
`Models.exe example.apsimx --in-memory-db`

`Models.exe --apply config-file.txt --in-memory-db`

`Models.exe example.apsimx --apply config-file.txt --in-memory-db`

