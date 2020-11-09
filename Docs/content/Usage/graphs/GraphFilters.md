---
title: "Graph filters"
draft: false
---

The filter box in the graph configuration is very flexible - see examples below. All column names need to be in square brackets and must be in the table specified by 'Data Source'. The column names that are available to be used in the filter are shown in the X or Y drop down lists.

## Examples

**if [Clock].Today has been output then these filters will work:**

```
[Clock.Today]>='1996-01-01'
[Clock.Today]>='1996-01-01' AND [Clock.Today] <= '2000-12-31'
```

**if [Clock].Today.Year has been output then these filters will work:**

```
[Clock.Today.Year] = 1995
[Clock.Today.Year] <> 1995
[Clock.Today.Year] >= 1995
[Clock.Today.Year] > 1995 AND [Clock.Today.Year] < 2000
[Clock.Today.Year] = 1995 OR [Clock.Today.Year] = 1996
[Clock.Today.Year] IN (1995, 1997, 1999)
[Clock.Today.Year] NOT IN (1995, 1997, 1999)
```

**Other examples**

```
[Wheat.SowingData.Cultivar] = 'Hartog'
[SimulationName] LIKE 'ExperimentFactorOneSlurp_Minus'
[SimulationName] LIKE 'ExperimentFactorOneSlurp_%'
```