---
title: "PMF code design"
draft: false
---

In addition to the 

* Root models must allow for multi-point root systems. This is provided when [IUptake](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Interfaces/IUptake.cs) is implemented and the standard *Root* class is used. If an alternate *Root* model is used, it needs to allow for multi-point root systems.
* Plant models must provide CO2 impacts. If the user changes CO2 in the weather component the plant model should respond.
* Plant models must use the transpiration value provided by MicroClimate. Without this, intercropping will not be possible.