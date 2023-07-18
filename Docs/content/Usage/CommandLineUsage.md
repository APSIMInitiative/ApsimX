---
title: "Command Line Usage"
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
- remove
    - ```delete [ParentNodeName].childNodeName```
- You can also edit a node's properties by using the node's name followed by a period-separated path to the property you would like to change followed by a equals sign and the new value. E.g. ```[Weather].FileName=Dalby.met```
    - paths must be relative to the location of the configFile.
- you can comment out command lines by using ```#``` or ```/```
    - note: must be at the start of the line. 

## What does an example config file look like?

```
#Makes a new apsimx file and builds it up to something that will run
#Dalby.met needs to be in C:\TestSims\
save C:\TestSims\newMinimalSim.apsimx
load C:\TestSims\newMinimalSim.apsimx
add [Simulations] Simulation
add [Simulation] Summary
add [Simulation] Clock
add [Simulation] Weather
[Weather].FileName=Dalby.met
[Clock].Start=1900/01/01
[Clock].End=1900/01/31
run
```


