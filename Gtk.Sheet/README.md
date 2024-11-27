# Description
The files in this directory implement a GTK sheet (grid) widget. The widget displays data in columns and rows and allows user interaction in a similar way to Microsoft Excel and other grid controls.

# Creating an instance of the SheetWidget
To create an instance of the widget, call the constructor like this:

```c#
 grid = new SheetWidget(sheetContainer.Widget,
                        dataProvider,
                        multiSelect: true,
                        onException: (err) => ViewBase.MasterView.ShowError(err),
                        gridIsEditable: gridIsEditable,
                        blankRowAtBottom: gridIsEditable);
```

* The first argument is a Gtk.Container that will be the parent of the sheet widget.
* The second argumnet is an instance of an IDataProvider. See *Providing data to sheet widget* below.
* The third argument denotes whether the user can select multiple cells.
* The forth argument is a callback that will be called if the sheet widget encounters an error.
* The fifth argument denotes whether the grid is editable by the user.
* The sixth argument denotes whether a blank row is added to the bottom of the sheet widget, allowing the user to add rows.

# Providing data to the sheet widget

The sheet widget uses an instance of IDataProvider to get the data to display. There are several ways to create an instance of this this interface.

__DataTableProvider__

This data provider wraps a .NET DataTable and allows it to be the source of data for a SheetWidet. An instance can be created by calling the constructor like this:

```c#
DataTable data = new DataTable();
DataTableProvider provider = new DataTableProvider(data);
```

__Create an instance of IDataProvider using an object with array properties__

An IDataProvider instance can be created by using reflection on an object instance. A ```Display``` attribute can be added to properties to indicate they should be columns in a grid.

```c#
    class ClassWithUnits
    {
        [Display)]
        [Units("mm")]
        public string[] Col1 { get; set; } = new string[] { "0-100", "100-200", "200-300" };

        [Display]
        [Units("kg/ha")]
        public string[] Col2 { get; set; } = new double[] { 1456, 3542, 345 };
    }
```
A IDataProvider instance can then be created by calling the static Create method of DataProviderFactory.

```c#
    ClassWithUnits classWithUnits = new()
    var dataProvider = DataProviderFactory.Create(classWithUnits);
```

This will result in a 2 column grid with 3 rows.

__Create an instance of IDataProvider using an object with a List object property__

An alternative way of creating an IDataProvider instance is a via a variation on the above mechanism.

```c#
class ClassWithUnits
{
    [Display]
    public List<Layer> Profile { get; set; }

    public class Layer
    {
        [Display]
        [Units("mm")]
        public string Col1 { get; set; }

        [Display]
        [Units("mm")]
        public double Col2 { get; set; }
    }
}
```
Creating the instance of IDataProvider is the same as the previous example.
```c#
    // Create and initialise an instance of ClassWithUnits.
    ClassWithUnits classWithUnits = new()
    {
        Profile = new()
        {
            new() { Col1 = "0-100", Col2 = 1456 },
            new() { Col1 = "100-200", Col2 = 3542 },
            new() { Col1 = "200-300", Col2 = 354 }
        }
    };

    var dataProvider = DataProviderFactory.Create(classWithUnits);
```
This will produce the same grid as the previous example.

__Create an instance of IDataProvider using an object with a DataTable property__

```c#
    class ClassWithDataTable
    {
        [Display]
        public DataTable Data { get; set; }
    }
```

Calling ```DataProviderFactory.Create``` with and instance of this class will produce an IDataProviderInstance using the DataTable as the grid data source.