---
title: "Report Node"
draft: false
---
## How to create a basic report
>Here we will describe the features of the report node and some additional information on how to use it.
>
> - Understand the anatomy of the report node.
> - Find relevant report variables.
> - Add report variables to a report.
> - Find report events that will record the variables when that occurs.
> - Add events to report.
> - How to set an alias/nickname for a report variable.

### Report variables and Report events

- Report variables are the data you want to record.
    - Examples are plant leaf area index, grain weight, grain size, yield and many others.
- Report events are when report variables are recorded.
    - This can be a time of day, week, month, year or when harvesting as well as many other events.

### The Report node 

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
    - When you begin typing in the report events window, the contents of this window will change to show events that match either the description of a common report event or a node within your Apsimx file.

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

### Giving a report variable a nickname/alias

To do this simply add the phrase: 'as alias' at the end of a line to give a variable a specific column name in your report.
This is a good idea for variables that are long and complex.

### Notes about report

If you try to run a report with a variable or event that is not valid you will see an error message appear in the message box at the bottom of the ApsimX window.

### Adding an event or variable to the common report variable/event list.

Anyone can submit a new common report variable or event to the lists for anyone to use.
Mulitple events and variables can be added at one time.

There are two ways to do this:

1. Use the 'submit new event/variable' button in the common report events window. 
    - This will take you to the ApsimX github page where you can submit a new 'issue' where you can add the information required to add a new variable or event.
2. Navigate directly to https://github.com/APSIMInitiative/ApsimX/issues/new?assignees=&labels=New+common+report+event%2Fvariable&projects=&template=new-common-report-event-variable.yml and add your details there.

Once this is submitted the event or variable will be reviewed. If accepted it will be added to the list for anyone to use.


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
