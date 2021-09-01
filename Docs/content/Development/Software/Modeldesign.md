---
title: "Model design"
draft: false
---

All models are written as normal .NET classes but must be derived from Model

## Properties initialised at startup

Models must be binary and JSON serializable. Serialization is the process where models (objects) are created at startup and their properties / fields are given values from the serialization file (.apsimx). Normally this doesn’t require any extra work by the model developer so long as the data types used are serializable (most .NET types are).
For JSON serialzation to work, fields and properties that are to be given values at startup need to be public. APSIM, though, assumes that only public properties are serialized. Public fields are considered poor programming practice. There should be no public fields in any model. There are two ways to declare properties:

```c#
// Option 1 - auto-generated property
public DateTime StartDate { get; set; }
 
// Option 2 - manual property
private DateTime _StartDate = new DateTime(1940, 12, 31);
public DateTime StartDate
{
    get
    {
        return _StartDate;
    }
    set
    {
        _StartDate = value;
    }
}
```

The setter for a property must not have side affects. In other words, the setter must not invoke behaviour that affects the state of other properties. Remember, the setter will be invoked for each property during deserialisation at startup.

## Communicating with other models - Links

If models need to communicate with other models, they may do so by declaring a public or private field with a [Link] attribute. APSIM will provide a valid reference for the field at simulation start time by finding another model that is in scope and has the same type as the field declaration. Once found, models can then call properties and methods of the referenced model as normal. e.g

```c#
[Link] private Clock clock = null;
[EventSubscribe("StartOfDay")]
private void OnStartOfDay(object sender, EventArgs e)
{
    if (Clock.Today == SowDate)
        // do something
}
```

In order to decouple models from other models, it may be necessary to create interfaces (e.g. IClock) that specify what the public interface for the type of model. This would then allow a different model to be swapped in. This would be particularly important for models where we have different implementations e.g. SoilWater.

Even though there is the ability to have optional links, they should be avoided. It is better to be explicit and say the there is always a dependency on another model. Optional links lead to users not knowing when they need to satisfy a model dependency or not.

There are also other types of links that are useful. 

```c#
[Link(Type = LinkType.Child, ByName = true)] 
private IFunction mortalityRate = null; // Link will be resolved by a child model with a name of 'mortalityRate'
```

```c#
[Link(Type = LinkType.Child)]   
public GenericTissue[] Tissue; // All child models of type 'GenericTissue' will be stored in the 'Tissue' array
```

```c#
[Link(Type=LinkType.Ancestor)]    // Link will be resolved by looking for a parent model of type 'PastureSpecies'
private PastureSpecies species = null;
```

```c#
[Link(Type = LinkType.Path, Path = "[Phenology].DltTT")] 
protected IFunction DltTT = null;   // Link will be resolved by the model on path '[Phenology].DltTT'
```		

## Published events and subscribing to events

APSIM uses the .NET event mechanism to signal when things happen during a simulation. Models can create their own events using the normal .NET event syntax:

```c#
public event EventHandler MyNewEvent;
 ...
if (MyNewEvent != null)
    MyNewEvent.Invoke(this, new EventArgs());
```

In this code snippet, an event is declared called 'MyNewEvent' (first line). Elsewhere, the event is published (invoked).

Models can subscribe to these events by putting an EventSubscribe attribute immediately before a private event handler method. The code example at the top of this page shows an event handler (OnStartOfDay) and an example of how to subscribe to the "StartOfDay" event. The event handlers will automatically be connected to the event publishers. There is no need to disconnect events. The APSIM infrastructure will take care of this.

Models in APSIM (particularly the CLOCK model) produce many events that may be useful for models. For a complete list see the sequence diagram of a running simulation. One event in particular is OnSimulationCommencing. This provides a model with an opportunity to perform setup functions. By the time this method is called by the infrastructure, APSIM will have satisfied all link references so a model is free to call properties on the linked models. Be aware though that the linked models may not have had their OnSimulationCommencing method called yet and so may not be in a consistent state.

Another useful event is called OnSimulationCompleted which will be invoked immediately after the simulation has completed running. This provides an opportunity for a model to perform cleanup.  

## A hierarchy of models

In all APSIM simulations, models are run under a parent model and ultimately a parent ‘Zone’ (a core model that looks after a collection of models (used to be called a Paddock). This zone model is itself contained within a  ‘Simulation’, which in turn is parented by a 'Simulations' model. Interfaces for these are provided in the reference documentation. If a model needs to communicate with its Zone or Simulation, it may do so via the normal link mechanism.

## Methods provided by the Model base class.

As stated above, all models must be derived from Model. This base class provides a number of methods for interacting with other models at runtime and discovering and altering the simulation structure.

```c#
/// <summary>
/// Get or set the name of the model
/// </summary>
public string Name { get; set; }
 
/// <summary>
/// Get or set the parent of the model.
/// </summary>
public Model Parent { get; set; }
 
/// <summary>
/// Get or sets a list of child models.
/// </summary>
public List<Model> Children { get; set; }
```

All models have a name, a parent model (except the Simulation model which has a null Parent) and child models.

## Errors

To flag a fatal error and immediately terminate a simulation, a model may simply throw an 'ApsimXException'. The framework guarantees that the OnSimulationCompleted method will still be called in all models after the exception has been raised.

## Writing summary (log) information

To write summary information to the summary file, link to a model implementing the

```c#
ISummary interface:
[Link] private ISummary Summary = null;
ISummary has two methods for writing information:
/// <summary>
/// Write a message to the summary
/// </summary>
void WriteMessage(string FullPath, string Message);
/// <summary>
/// Write a warning message to the summary
/// </summary>
void WriteWarning(string FullPath, string Message);
```

WriteMessage will simply write a message, attaching the current simulation date (Clock.Today) to the message. WriteWarning can be used to write a warning (e.g. out of bounds) message to the summary.

## Simulation API

While the preference is for models to use the [Link] mechanism above for communicating with other models, sometimes it is necessary to dynamically work with models in a simulation in a more flexible way. For example, the REPORT model needs to get the values of variables from models where the interface to a model isn't known. To enable this type of interaction, a static API exists that contains methods that may be called by a model during a simulation. The API class is called 'Apsim' and it contains many useful methods:

```c#
// Get the value of the SW variable from a soil model.
double[] sw = Apsim.Get(this, "[Soil].Water);
 
// Find a crop model that is in scope.
ICrop crop = Apsim.Find(this,, typeof(ICrop)) as ICrop;
 
// Loop through all crop models that are in scope.
foreach (ICrop crop in Apsim.FindAll(this, typeof(ICrop)))
{ }
 
// Find the parent simulation
Simulation simulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;
```

## Attributes 

```c#
[Bounds(Lower = 0.0, Upper = 10.0)]  // Specifies lower and upper bound of 'a_interception'
public double a_interception { get; set; }
```		
```c#
[Description("Frequency of grazing")]   // Specifies the text displayed to the user in the GUI for a model
public string SimpleGrazingFrequencyString { get; set; }
```
```c#
[ViewName("UserInterface.Views.GridView")]  // Use this view and presenter when user clicks on model.
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
public class PastureSpecies : ModelCollectionFromResource, IPlant, ICanopy, IUptake, IPlantDamage
```
```c#
[Separator("Grazing parameters")] // Show a separator description to user
```
```c#
[Summary]  // Write 'Thickness' to summary file
public double[] Thickness { get; set; }
```
```c#
[Tooltip("Name of the predicted table in the datastore")] // Show tool tip to user
public string PredictedTableName { get; set; }
```
```c#
[Units("kg/ha")] // Specify units that are shown on graphs.
public double Wt { get; set; }
```
```c#
[ValidParent(ParentType=typeof(Simulation))] // Specify models that 'weather' can be dropped in.
[ValidParent(ParentType = typeof(Zone))]
public class Weather : Model, IWeather, IReferenceExternalFiles
```

## Display attributes

```c#
[Display]  // Show this property to user.
public bool CalcStdDev { get; set; } = true;
```

```c#
[Display(Type = DisplayType.DirectoryName)] // Allows the user to click on a button to choose a directory name
public string OutputPath { get; set; }  
```
```c#
[Display(EnabledCallback = "IsSpecifyYearsEnabled")] 
public double[] Years { get; set; } // Will call c# property 'IsSpecifyYearsEnabled' to enable/disable option.

public bool IsSpecifyYearsEnabled { get { return TypeOfSampling == RandomiserTypeEnum.SpecifyYears; } }
```
```c#
[Display(Values = "GetValues")] // Show a drop down to user. Call 'GetValues' method to get dropdown values.
public string ColumnName { get; set; }

public string[] GetVales() { return new string[] {"A", "B" };
```		
```c#
[Display(Type = DisplayType.Model, ModelType = typeof(IPlantDamage))]
public IPlantDamage HostPlant { get; set; } // Show a drop down to user with matching model names.
```
```c#
[Display(Type = DisplayType.TableName)] // Show a drop down to user containing table names from datastore
public string PredictedTableName { get; set; }
```
```c#
[Display(Type = DisplayType.FieldName)] // Show a drop down to user with field names from a table.
public string FieldNameUsedForMatch { get; set; }
```
```c#
[Display(Format = "N2")] // Show 2 decimals to user.
public double[] LL15 { get; set; }
```
```c#
[Display(Type = DisplayType.ResidueName)] // Show a drop down containing surface residue types.
public string InitialResidueType { get; set; }
```




