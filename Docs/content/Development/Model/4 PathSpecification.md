---
title: "4. Path Specification"
draft: false
---

Paths are structured similarly to directory paths in Windows and Unix, using a ‘.’ character instead of slashes.
 
## Relative paths

Relative paths are relative to the model that is using the path e.g.

*Report* - relative path - refers to the child model called Report

*Soil.Water* - relative path - refers to the child Water model of the child Soil model.

## Absolute paths

Absolute paths have a leading ‘.’ e.g.

*.Simulations.Test.Clock* - absolute path - refers to the clock model in the 'Test' simulation.

## Scoped paths:

Scoped paths have a leading model type in square brackets. A model of the specified name, in scope, is located before applying the rest of the path.

*[Soil].Water* - scoped path - refers to the Water model that is a child of a model that has the name 'Soil' that is in scope
