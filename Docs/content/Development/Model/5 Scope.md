---
title: "5. Model Scope"
draft: false
---

When trying to find models that are in scope (either by links or the ‘get’ method), the APSIM framework will look for matches hierarchically in the simulation. The find algorithm will return all children (recursively), all siblings and then all parent siblings recursively for a given reference model.

![Scope](/images/Development.Scope.png)