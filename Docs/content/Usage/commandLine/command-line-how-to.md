---
title: "Command Line How To"
draft: false
---

# Using ApsimX command line to create, run and edit apsimx files.

Apsim Next Gen can be used to do your normal functions as you would from the full version.

## How to run apsim next gen from the command line?

Copy the path to the Models.exe executable. If installed for all users, this is located at:

```
"C:\Program Files\APSIM<Version-number>\bin\Models.exe"
```

<small> replace \<version-number\> with your current version number. </small>

Use: 

```
"C:\Program Files\APSIM<Version-number>\bin\Models.exe" --apply <configFileName.txt>
``` 
<strong style="color:red"> Note: Above command can only be used when load is the first command in the configFile. </strong>

or use: 

```
"C:\Program Files\APSIM<Version-number>\bin\Models.exe" --apply <configFileName.txt> 
```


## Making running models easier
* To make the running of models easier, models can be add to Path. Doing so means that you can type `Models` instead of typing the full file path to the Models.exe.
* To do this add the full path to the Path system environment variable. 
* For more details on how to do this, see <a href="https://www.architectryan.com/2018/03/17/add-to-the-path-on-windows-10/" target="_blank">here</a>


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
- Example
    - ` Models.exe example.apsimx --log error`


## Use in memory database
- This uses an in memory database rather than database files for running simulations.
- This can be done using the `--in-memory-db` switch.
- A database file is still created however no data will be stored to the database file.
- Examples:
    - `Models.exe example.apsimx --in-memory-db`
    - `Models.exe --apply config-file.txt --in-memory-db`
    - `Models.exe example.apsimx --apply config-file.txt --in-memory-db`

## Making repeated changes to many files (batching)
* For situations where you need to make the same changes to many apsim files but need specific nodes or parameters changed, 
the `--apply` switch can be used in conjunction with the `--batch` switch.
* An example where this would be useful is when you want to change the soil and weather for each individual APSIM file and you have 10s to 100s to 1000s of APSIM files.

* To do this you will need two specific files along with any APSIM files you want to change, these files are:

    * A config file containing 'placeholders'
        * the placeholders are the values that will be replaced by the values in the batch file 
        * a placeholder is a name that starts with a $ symbol. An example would be `$weather-file-name`.
        * placeholders cannot contain spaces.
        * an example config file:

        ```
        load BaseCl.apsimx
        [Soil]=SoilLibrary.apsimx;[$soil-name]
        [Weather].FileName=$weather-file-name
        [SimulationExp].Name=$sim-name
        run 
        ```

    * A batch file, this is a csv file with headers that match the placeholders (minus the $ symbols)
        * for each row in the batch file a run through of the config file is completed. 
        * an example batch file:

        
        |soil-name|weather-file-name|sim-name|
        |----|----|----|
        |Ahiaruhe_1a1|16864.met|Sim0001|
        |Ahuriri_7a1|19479.met|Sim0002|
        |Ailsa_5a1|19479.met|Sim0003|
        
    
* To run this we would run something like: `"C:\Program Files\APSIM<your version number>\bin\Models.exe" --apply config-file-name.txt --batch batch-file-name.csv`

