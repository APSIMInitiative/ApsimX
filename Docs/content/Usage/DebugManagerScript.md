---
title: "Debug Manager Script"
draft: false
---

To debug a manager script you need to insert

```c#
using System.Diagnostics;
```

at the top of your manager script. Then to trigger a breakpoint, insert

```c#
Debugger.Break();
```

into a method or property to have the debugger stop. Apsim Next Generation needs to be run from Visual Studio and be in debug mode.When a simulation is run from APSIM Next Generation, Visual Studio will stop on the above line and you will be able to inspect values of variables and step into/over lines of code.