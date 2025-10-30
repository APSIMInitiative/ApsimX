---
title: "APSIM Command Language"
draft: false
weight: 20
---

The APSIM command language is a way for the user to change the structure of a simulation and parameterise models from the command line. The language is specified via a series of string commands, usually contained within a text file (a command file).

An example use case for the command language is: "Replace the soil, weather file, clock start/end dates in a .apsimx file and run APSIM i.e. run an APSIM simulation for a grid point". The user can create a command file to do the replacements and have it applied from the command line. They can then call this from a batch/bash file to do this repeatedly for multiple grid points. Prior to the command language, the user would have had to write Python/R scripts to directly manipulate the JSON in the .apsimx file. The problem with this approach is that the structure of the JSON changes over time which breaks the user's scripts.

This language replaces an older version of the language. If you have command files written using the old syntax, [see here](/usage/commandline/commandlanguage-old-to-new) for details on how to convert the syntax.

## What commands can you perform?

- __load__ - loads a .apsimx file into memory. Subsequent commands will then apply to the contents of the file.
     ```load C:\TestSims\wheatTest.apsimx```
- __save__ - saves the in-memory simulations to file.
    ```save C:\TestSims\wheatTest.apsimx```
- __run__ - run APSIM on the in-memory simulations.
    ```run```
- __add__ - add a new or existing model to another model.
    ```add new Report to [Zone]```
    ```add new Report to [Zone] name MyReport```
    ```add [Report] to all [Zone]```
    ```add [Soil1] from soils.apsimx to [Zone] name Soil```
- __delete__ - delete a model
    - ```delete [Zone].Soil```
- __duplicate__ - duplicate a model.
    - ```duplicate [Zone].Report name NewReport```
- __property set__ - Change the property of a model
      ```[Weather].FileName=Dalby.met```
- __property set__ - Change the property of a model using the value read from a file (contents of value.txt).
      ```[Weather].FileName=<value.txt```
- __property add to array__ - Add a string to a model array property or update it if it already exists.
      ```[Janz].Command += [Phenology].CAMP.EnvData.VrnTreatTemp = 5.5```
- __property delete from array__ - Remove a string from a model array property
      ```[Janz].Command -= [Phenology].CAMP.EnvData.VrnTreatTemp```
- __comment lines__ - you can comment out command lines
      ```# Add Soil1 from the soils database file```

    _note 1: all file name references can either have an absolute path or no path making the file relative to the command file._

    _note 2: a property set/add/remove/update is only applied to the first occurence of it in the simulation tree. For example, if there are multiple occurences of ```Janz```  and you are setting ```[Janz].Command``` then the change will only be applied to the first occurence._

## Property formatting when setting property values

### Dates

dd/MM/yyyy will not work. yyyy-MM-dd is the recommended format.

### Numbers

Decimal place should be a period (not a comma). Comma is allowed as  thousands separator but isn’t mandatory.

### Strings (text)

Quotes will be included in the value which is assigned to the property. For example, if you do this:

[Clock].Name = "This is a clock"

the name of the Clock model will be ```"This is a clock"```

### Arrays

Array or list properties should be specified as comma-separated values. It is also possible to modify an element at a particular index or indices of an array or list, but the indices start at 1. If modifying multiple elements, a second index can be provided after a colon, as in the example below.

```
[Physical].BD = 1,2,3,4,5,6,7
[Physical].AirDry[1] = 8
[Physical].LL15[3:5] = 9
[Physical].BD =                # sets to an empty array.
```

## What does an example config file look like?
```
# Makes a new apsimx file and builds it up to something that will run
# Dalby.met needs to be in C:\TestSims\

load C:\TestSims\minimalSim.apsimx

# Add various models to the Simulations model. minimalSim.apsimx only has
# a Simulations model with a child DataStore model.
add new Simulation to [Simulations]
add new Summary to [Simulation]
add new Clock to [Simulation]
add new Weather to [Simulation]

# Change data in various nodes.
[Weather].FileName=Dalby.met
[Clock].Start=1900-01-01
[Clock].End=1900-01-31

# Adds a soil from soils.apsimx, removing the existing soil
delete [Zone].Soil
add [Soil1] from soils.apsimx to [Zone] name Soil

# Saves the Simulation to new file.
save C:\TestSims\Test.apsimx

# Runs the simulation.
run
```
