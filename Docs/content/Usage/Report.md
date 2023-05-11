---
title: "Using the Report Node"
draft: false
---
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
