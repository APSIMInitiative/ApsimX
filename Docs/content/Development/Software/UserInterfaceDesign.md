---
title: "User interface design"
draft: false
---

# Design characteristics

* The ‘Command pattern’ is used for all user interface actions (e.g. select a node, edit the clock start date, change the format of a graph). A history of commands will be stored for each .apsimx file loaded. This will allow undo/redo as each command will have the ability to undo itself. This also allows a script of commands to be sent to the user interface via the command line, allowing some automated user testing of the user interface.
* The user interface shall run on Windows, LINUX, OSX.
* GTK# (rather than Windows.Forms) is used to provide cross-platform compatibility.
* A ‘Model-View-Presenter’ pattern shall be used to disconnect views (forms) from the models. 

# Commands

The requirement for Undo/Redo has led to the adoption of the 'command' pattern in the user interface. This pattern dictates that all changes to all 'Model' classes must be done via a command. Commands are also used by the user interface when the user interacts with tree nodes. Each command has two methods, one for performing the command, another for undoing the command. The interface (ICommand.cs) looks like this:

```c#
namespace UserInterface.Commands
{
    public interface ICommand
    {
        object Do();
        object Undo();
    }
}
```

If the command alters the state of a model during a ‘Do’ or ‘Undo’, it should return the altered model to the caller (CommandHistory). The CommandHistory will then invoke a ‘ModelChanged’ event that the views can subscribe to and update their screens. As an example of a concrete command, the ‘ChangePropertyCommand’ is given below. This command is used to change a property value in a model. Before doing this though, it will retrieve the original value so that it can reapply this value during an Undo operation. 

```c#
namespace UserInterface.Commands
{
    class ChangePropertyCommand : ICommand
    {
        private object Obj;
        private string Name;
        private object Value;
        private object OriginalValue;
 
        public ChangePropertyCommand(object Obj, string PropertyName, object PropertyValue)
        {
            this.Obj = Obj;
            this.Name = PropertyName;
            this.Value = PropertyValue;
        }
 
        public object Do()
        {
            // Get original value of property so that we can restore it in Undo if needed.
            OriginalValue = Utility.Reflection.GetValueOfFieldOrProperty(Name, Obj);
 
            // Set the new property value.
            if (Utility.Reflection.SetValueOfFieldOrProperty(Name, Obj, Value))
                return Obj;
            return null;
        }
 
        public object Undo()
        {
            if (Utility.Reflection.SetValueOfFieldOrProperty(Name, Obj, OriginalValue))
                return Obj;
            return null;
        }
    }
}
```

# Model View Presenter

A 'Model' in this context is self explanatory. It is the class that holds the problem domain data (deserialised from the json files) that is editable by the user and executes during a simulation run. Some examples include SoilWater, Clock and Graph.

A 'View' is a form that allows user interaction. It doesn't have any functionality beyond the display of information and receiving user input. It does not have any functionality that determines what data gets put on the screen. i.e. it doesn't talk to the model. A 'view' does not have a reference to a model or presenter. It is essentially a very passive (humble) form that is told what to do by the presenter. It does not contain any logic that describes what to do when the user interacts with it. In short, the idea is to keep it as simple as possible.

A 'Presenter' is a class that tells the view what to display, asking the model for that data. It acts as a go-between between a view and a model. It is also responsible for determining what to do when the user does something. A Presenter should not have code that assumes a particular display technology i.e. no using System.Windows.Forms or System.Web.Forms.

In theory, the user interface should be able to be recoded from a windows app to a web app by just recoding the 'views' and keeping everything else the same. It should also be noted that a view could have multiple presenters in different situations. For example, a 'GridView' (form with a grid on it) may have 1 presenter that populates the grid with property type info (like what the user sees when they click on a manager component). It might have another presenter that displays soil profile information. A third presenter might display the contents of an APSIM output file.

For more info on the Model/View/Presenter pattern visit here: [http://codebetter.com/jeremymiller/2007/07/26/the-build-your-own-cab-series-table-of-contents](http://codebetter.com/jeremymiller/2007/07/26/the-build-your-own-cab-series-table-of-contents)

# ExplorerView / ExplorerPresenter

The central concept in the user interface is the ‘ExplorerView’, a form with a simulation tree on the left and a right hand panel where model views are displayed. The associated ‘ExplorerPresenter’ is responsible for populating the controls on the view and for responding to input from the user. When the user selects a model in the simulation tree, an event handler is called in the presenter, which will in turn look for two reflection tags in the model.

```c#
[ViewName("UserInterface.Views.GridView")]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
```

The ViewName tag tells the presenter the full name (including the namespace) of the ‘view’ class to display on the screen. Each view class needs a corresponding presenter class and the PresenterName specifies this. With these two class names, the ExplorerPresenter can create instances of these and tell the ExplorerView to display the view in the right hand panel.

The presenter also maintains a 'CommandHistory' containing all executed commands and this is passed to each presenter that it creates so that they can create commands as required. This is done via the ‘Attach’ method in IPresenter.

```c#
namespace UserInterface.Presenters
{
    public interface IPresenter
    {
        void Attach(IModel Model, object View, CommandHistory CommandHistory);
    }
}
```

#Example Model / View / Presenter

The Axis model is a simple data container for storing properties associated with an axis on a graph. The data deserialised from the json looks like this:

```json
	{
	  "$type": "Models.Axis, Models",
	  "Type": 3,
	  "Title": "Y axis title",
	}
```

The axis model source looks like this:

```c#
namespace Model.Components.Graph
{
    public class Axis
    {
        public enum AxisType { Left, Top, Right, Bottom };
 
        /// <summary>
        /// The 'type' of axis - left, top, right or bottom.
        /// </summary>
        public AxisType Type { get; set; }
 
        /// <summary>
        /// The title of the axis.
        /// </summary>
        public string Title { get; set; }
    }
}
```

This model has 2 properties, type and title. 

The Axis view is very simple with a single text box that displays the axis title. The AxisPresenter that connects the model to the view looks like this:

```c#
using Model.Components.Graph;
using UserInterface.Views;
 
namespace UserInterface.Presenters
{
    /// <summary>
    /// This presenter connects an instance of a Model.Graph.Axis with a
    /// UserInterface.Views.AxisView
    /// </summary>
    class AxisPresenter : IPresenter
    {
        private Axis Axis;
        private IAxisView View;
        private CommandHistory CommandHistory;
 
        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        public void Attach(object model, object view, CommandHistory commandHistory)
        {
            Axis = model as Axis;
            View = view as AxisView;
            CommandHistory = commandHistory;
 
            // Trap change event from the model.
            CommandHistory.ModelChanged += OnModelChanged;
 
            // Trap events from the view.
            View.OnTitleChanged += OnTitleChanged;
 
            // Tell the view to populate the axis.
            View.Populate(Axis.Title);
        }
 
        /// <summary>
        /// The 'Model' has changed so we need to update the 'View'.
        /// </summary>
        private void OnModelChanged(object Model)
        {
            if (Model == Axis)
                View.Populate(Axis.Title);
        }
 
        /// <summary>
        /// The user has changed the title field on the form. Need to tell the model this via
        /// executing a command.
        /// </summary>
        void OnTitleChanged(string NewText)
        {
            CommandHistory.Add(new Commands.ChangePropertyCommand(Axis, "Title", NewText));
        }
    }
}
```

In the Attach method, the Axis presenter traps the model’s OnChanged event (caused by an Undo) and the views OnTitleChanged event (caused by the user). It also tells the view to populate the text box with the value of the title property from the model. When the title changes in the model (OnChanged), the presenter tells the view the new title. When the view changes the title (in OnTitleChanged), the presenter tells the model the new title via a command so that this can be undone at a later time.

All views should have an interface (IAxisView) to decouple the view from the presenter that calls into it. This allows a presenter to use different implementations of a view.
