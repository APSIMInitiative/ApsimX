---
title: "Report Node"
draft: false
---
## How to create a basic report
>Here we will describe how to create a basic report with the Wheat example.
>
>By the end of this short tutorial you'll know how to:

> - Understand the anatomy of the report node.
> - Find relevant variables to report.
> - Add variables to a report.
> - Find simulation events that will record the variables when that occurs.
> - Add events to report.
> - How to set an alias/nickname for a report variable.

### The Report node 

1. Go ahead and open the example Wheat simulation. You can do this by pressing the 'Open an example' menu button to the top left of the main menu. This will open a file explorer, The Wheat simulation is a file called 'Wheat.apsimx'.
2. Once the example simulation is opened, double-click the 'Field' node and click the child node 'Report'. This will show the code that is used to generate a report when a simulation is run.
3. There are our sections in the report node view.

#### Report Variables window

- Top left window is the reports variables window. 
    - These are the variables that are reported on when the simulation is run. 
    - A typical variable starts with a simulation node name enclosed in square brackets followed by child node names or node properties separated by periods. 
        - An example is ```[Wheat].LAI```.
    - When you begin typing the window to the left's content will change if there are matching reporting variables.

#### Common report variables window

- Top right window is the common report variables window. 
    - These initially display any variables relevant to the entire simulation. 
    - When you begin typing on any line, this window's contents will filter common variables (if any exist) based on simulation node names typed and text that may appear match the description of common reporting variables.

#### Report events window

- The bottom left window is the report events window. 
    - Code is written here that determines when the report variables are reported. 
    - When you begin typing, the common report events content will change, showing relevant events if any exist.

#### Common report events window

- The bottom right window is the common report events window.
    - Initially all relevant events for the simulation show up here.
    - When you begin typing in the report events window, the contents of this window will change to show events that match either the description of a common report event or a node within your current simulation.

### Finding relevant report variables

- The most efficient way to find variables is to simply start typing a nodes name. Doing this will filter out common variables on the right. 
- If you cannot find it in the common report variables list you may have to type something more specific to find what you are looking for.
- If you know the property you want to report on's node. You can use the 'intellisense' pop-up window to help find a relevant property.
    - To bring up the intellisense window, type a node's name encased in square brackets followed by a period.

### Adding variables to a report

- To add variables you can do this in 3 different ways:
    1. Double-clicking a variable from the list on the right.
    2. Dragging a variable from the common report variables list to the report variables window.
    3. Typing the code directly into the reporting variables window.







## Using report row filters

To filter the row data in your report node, you can use row filters.

To do so you type the name of the column header along with a conditional statement in the "Row Filter" field.

- Some examples:

```
"Clock.Today" = "1900-11-10"
```

- The Clock time must be a complete date of the format "YYYY-MM-DD". However, you can include a wildcard character (*) in place of any number in the Clock time.
- You can use any greater than or less than combination too. Examples: 

```
"Clock.Today" > "1900-01-01"
```

```
"Wheat.AboveGround.Wt" > 900
```

- You can have multiple filters. An Example:

```
[Clock.Today] > "1905-**-**" and [Wheat.AboveGround.Wt] > 1000
```

## Using report column filters

To filter the column data in you report node, you can use a column filter.

To do so you type the name of the column header. Some examples are:

```
Clock.Today
``` 

``` 
Wheat.AboveGround.Wt.
```
### Multiple columns
It's possible to have all fields that contain a common keyword to be displayed.

If your report variables have many child properties you can use the name of the parent to filter all columns that contain the parent's name.

An example:

```
Wheat
```

![report variables](/images/report-vars.png)
*Report variables in the simulation:*


![column filter results](/images/report-column-filter-result.png)
*Results of using a common keyword*

Note: Only one column filter can be used at one time.
