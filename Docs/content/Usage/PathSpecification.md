---
title: "Path Specification"
draft: false
---

Paths are used throughout APSIM e.g. in REPORT. Paths are structured similarly to directory paths in Windows and Unix, using a ‘.’ character instead of slashes.
 
## Relative paths

Relative paths are not used much in APSIM. They are relative to the model that is using the path e.g.

If the soil model does a get for *Water* the a child model of that name will be returned.

## Absolute paths

Absolute paths have a leading ‘.’ e.g.

*.Simulations.Test.Clock* - absolute path - refers to the clock model in the 'Test' simulation.

## Scoped paths:

Scoped paths have a leading model type in square brackets. A model of the specified name, in scope, is located before applying the rest of the path.

*[Soil].Water* - scoped path - refers to the Water model that is a child of a model that has the name 'Soil' that is in scope
