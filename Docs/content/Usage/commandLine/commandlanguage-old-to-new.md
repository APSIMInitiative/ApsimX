---
title: "APSIM Command Language (old to new)"
draft: false
weight: 60
---

The APSIM command language has changed to make it more intuitive. [The new language is described here](/usage/commandline/commandlanguage). Most of the changes are to the ```add``` command. The other commands remain unchanged.

Examples that show how to convert new old syntax to the new syntax.

## Old -> New

- __add__ - add a new or existing model to another model.

    ```add [Zone] Report``` ->  ```add new Report to [Zone]```
    ```add [Zone] Report MyReport``` -> ```add new Report to [Zone] name MyReport```
    ```add [Zone] soils.apsimx;[Soil1] Soil``` -> ```add [Soil1] from soils.apsimx to [Zone] name Soil```
- __duplicate__ - duplicate a model.
    - ```duplicate [Zone].Report NewReport``` -> ```duplicate [Zone].Report name NewReport```
- __comment lines__ - only ```#``` is supported as a comment character.
