---
title: "Procedurally Edit .apsimx Files"
draft: false
---

The /Edit switch on Models.exe allows the user to edit an .apsimx file from the command line in a language-agnostic way. The syntax is:

```
/path/to/Models.exe <PathToApsimXFile> /Edit <PathToConfigFile>
```

The first argument, <PathToApsimXFile> should be the path to the .apsimx file which you want to edit. This file will be edited in-place - that is, if you don't want to modify the original .apsimx file, you should copy it somewhere else and edit the copy.

The /Edit argument instructs APSIM to edit the .apsimx file rather than run it.

The argument immediately following /Edit must be the path to a config file. The config file should contain zero or more lines of the form `path = value`.

The path should be the path to a model or property of a model in the .apsimx file. This can be a [scoped or absolute path](/development/model/4-pathspecification). An absolute path can be obtained by right-clicking on the model in the user interface and clicking "Copy path to node". This will copy the path to the model, not to one of the model's properties. For example:

![Wheat clock image](/images/Usage.EditFile.WheatClock.png)

Clicking copy path to node will yield `.Simulations.Simulation.Clock`.

In general, model property names can be found in the [Params/Inputs/Outputs documentation](/modeldocumentation). In this case, if we want the simulation to start in 1901 instead of 1900 we could add this to the config file:

```
.Simulations.Simulation.Clock.StartDate = 1901-1-1
```

## Property formatting

### Dates

dd/MM/yyyy will not work. yyyy-MM-dd is the recommended format, but yyyy-mm-ddThh:mm:ss will work (APSIM will ignore the time component) as will MM/dd/yyyy. Anything else is use-at-your-own-risk.

### Numbers

Decimal place should be a period (not a comma). Comma is allowed as thousands separator but isn't mandatory.

### Strings (text)

Quotes will be included in the value which is assigned to the property. For example, if you do this:

```
[Clock].Name = "This is a clock"
```

You will end up with this:

![This is a clock - result](/images/Usage.EditFile.ThisIsAClock.png)

### Arrays

Array or list properties should be specified as comma-separated values. It is also possible to modify an element at a particular index of an array or list, but the indices start at 1.

For example this:

```
.Simulations.Simulation.Field.Soil.Physical.BD = 1,2,3,4,5,6,7
.Simulations.Simulation.Field.Soil.Physical.AirDry[1] = 8
```

Results in:

![Edit array result](/images/Usage.EditFile.EditArray.png)

## Replacing an entire model

Instead of modifying one particular property, it is possible to replace an entire model. This requires that a second .apsimx file exists, which contains the model being used to perform the replacement. For example, if we wanted to replace the soil water model with the Swim3 model, we would need to create a separate .apsimx file containing Swim3 somewhere:

![ReplaceSW](/images/Usage.EditFile.SWModels.png)

Then add something like this to the config file:

```
.Simulations.Simulation.Field.Soil.SoilWater = replace.apsimx;.Simulations.SoilWaterModels.Swim3
```

Here we see a new syntax. On the right-hand side are two values, separated by a semicolon. The first value is the path to an .apsimx file. The second part (after the semicolon) specifies the path to the model inside this .apsimx file which will be replacing the model specified on the left-hand side.

In this case, we are replacing the SoilWater model with the Swim3 model contained in replace.apsimx. This might be easier to read (although potentially less predictable) with scoped paths:

```
[SoilWater] = replace.apsimx;[Swim3]
```

Note that the path to the .apsimx file containing the replacement model (in this case, replace.apsimx) must be relative to the config file's path.

Before: ![Before Swim replacement](/images/Usage.EditFile.SwimBefore.png)

After: ![After Swim replacement](/images/Usage.EditFile.SwimAfter.png)
