---
title: "Build a model tutorial"
draft: false
---

In this step-by-step guide we will show how to create a new model for APSIM. The model we will be building is a rainfall modifier. This will be a simple model that changes the rainfall in a simulation, after a user specified date, using a simple multiplier and addition. It will modify the rainfall before the other models in APSIM use the rainfall value. This will allow you to see the effects on the crop of reducing or increasing the rainfall.

The sections in this guide increase in complexity. To follow along with this tutorial you can build the model using the manager component in the user interface (manager scripts are just models). Alternatively, you can install the necessary tools to compile and build APSIM using these steps:

* get all source code from GitHub via [a Git client](/contribute/sourcetree) or via [the command line](/contribute/cli)
* install [compilers](/contribute/compile)

# 1. Create a basic model

The simplest APSIM model looks like this:

```c#
using Models.Core;
using System;
namespace Models
{
    /// <summary>This is a simple rainfall modifier model.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    public class RainfallModifier : Model
    {
    }
}
```

1. All models in APSIM must derive from *Model* - ```public class SimpleModel : Model```
2. To allow users, via the user interface, to add the model to another model (as a child), the model developer needs to specify the valid parent models - ```[ValidParent(ParentType = typeof(Simulation))]```. This will allow users to add *RainfallModifier* to a *Simulation* model. Other common options could have been *Zone*. All models can be added to a *Folder* so there is no need to specify *Folder* as a valid parent. Multiple valid parents can be specified by duplicating the *ValidParent* attribute.
3. The ```[Serializable]``` attribute is needed for all models and indicates that the model instance can be converted to a string (JSON) and written to the .apsimx file.
4. This model, in its current form, can be compiled and run in APSIM, even though it doesn't do anything yet.

# 2. Add input parameters.

Models can define user specified (via the user interface) input parameters.

```c#
using Models.Core;
using System;

namespace Models
{
    /// <summary>This is a simple rainfall modifier.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class RainfallModifier : Model
    {
        /// <summary>Start date for modifying rainfall</summary>
        [Description("Start modifying rainfall from date:")]
        public DateTime StartDate { get; set; }

        /// <summary>Rainfall muliplier</summary>
        [Description("Rainfall multiplier: ")]
        public double RainfallMultiplier { get; set; }

        /// <summary>Rainfall addition</summary>
        [Description("Rainfall addition (mm): ")]
        public double RainfallAddition { get; set; }
    }
}
```

The above code defines three parameters: StartDate, RainfallMultiplier and RainfallAddition. The model will ultimately modify (using the multipler and addition properties) the simulation's rainfall after the user specified StartDate. For now these three properties are placeholders and aren't being used yet.

1. The *Description* attributes define the text the user will see in the user interface.
2. The *ViewName* and *PresenterName* attributes on the *RainfallModifier* class instruct the APSIM user interface to use a grid view and a property presenter (keyword / value) when the user clicks on the *RainfallModifier* model.

# 3. Add an output property

This step will add a single output to return the unmodified rainfall variable, that is the original rainfall value before this model changed it. This will be a useful output for diagnosis purposes.

```c#
using Models.Core;
using System;

namespace Models
{
    /// <summary>This is a simple rainfall modifier.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class RainfallModifier : Model
    {
        /// <summary>Start date for modifying rainfall</summary>
        [Description("Start modifying rainfall from date:")]
        public DateTime StartDate { get; set; }

        /// <summary>Rainfall muliplier</summary>
        [Description("Rainfall multiplier: ")]
        public double RainfallMultiplier { get; set; }

        /// <summary>Rainfall addition</summary>
        [Description("Rainfall addition (mm): ")]
        public double RainfallAddition { get; set; }

        /// <summary>An output variable.</summary>
        public double OriginalRain { get; private set; }
    }
}
```

The new property is call *OriginalRain*. APSIM makes all public properties visible and accessible to other models in a simulation. For example, the user can use the *Report* model to output the value of this property by specifying the output: ```[RainfallModifier].OriginalRain```

The *OriginalRain* property in this example also defines a private setter so that other models cannot modify the value of this property. Only the *RainfallModifier* model is allowed to modify *OriginalRain*. To allow other models to modify the property. The *private* designator can be removed leaving:

 ```public double OriginalRain { get; set; }``` 
 
# 4. Add links to other models

This model will need to access properties from the *Clock* and *Weather* models in APSIM. To allow this, links are specified to each of these models. 

```c#
using Models.Climate;
using Models.Core;
using System;

namespace Models
{
    /// <summary>This is a simple rainfall modifier.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class RainfallModifier : Model
    {
        [Link]
        Clock clock = null;

        [Link]
        Weather weather = null;

        /// <summary>Start date for modifying rainfall</summary>
        [Description("Start modifying rainfall from date:")]
        public DateTime StartDate { get; set; }

        /// <summary>Rainfall muliplier</summary>
        [Description("Rainfall multiplier: ")]
        public double RainfallMultiplier { get; set; }

        /// <summary>Rainfall addition</summary>
        [Description("Rainfall addition (mm): ")]
        public double RainfallAddition { get; set; }

        /// <summary>An output variable.</summary>
        public double OriginalRain { get; private set; }
    }
}
```

The *[Link]* attribute indicates a dependency on another model. In this case the *RainfallModifier* model depends on (uses) *Clock* and *Weather*. APSIM will ensure these dependencies are met at runtime. It will find a clock and weather model and set the values of these two fields so that *RainfallModifier* can communicate with the two external models.

Note: Clock is the model in APSIM that provides the current simulation date which will be needed in the next step.


# 5. Add implementation.

The last step in creating the model is to write the implementation code that modifies rainfall after the user specified *StartDate*.

```c#
using Models.Climate;
using Models.Core;
using System;

namespace Models
{
    /// <summary>This is a simple rainfall modifier model.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class RainfallModifier : Model
    {
        [Link] 
        Clock clock = null;

        [Link]
        Weather weather = null;

        /// <summary>Start date for modifying rainfall</summary>
        [Description("Start modifying rainfall from date:")] 
        public DateTime StartDate { get; set; }

        /// <summary>Rainfall muliplier</summary>
        [Description("Rainfall multiplier: ")] 
        public double RainfallMultiplier { get; set; }

        /// <summary>Rainfall addition</summary>
        [Description("Rainfall addition (mm): ")] 
        public double RainfallAddition { get; set; }

        /// <summary>An output variable.</summary>
        public double OriginalRain { get; private set; }

        /// <summary>Handler for event invoked by weather model to allow modification of weather variables.</summary>
        [EventSubscribe("PreparingNewWeatherData")]
        private void ModifyWeatherData(object sender, EventArgs e)
        {
            OriginalRain = weather.Rain;
            if (clock.Today >= StartDate)
                weather.Rain = RainfallMultiplier * weather.Rain + RainfallAddition;
        }
    }
}
```

The above code introduces a method (function) called *ModifyWeatherData*. The *EventSubscribe* attribute indicates that the method is an event handler that will be invoked (called) whenever a *PreparingNewWeatherData* event is published. In APSIM this event is published by a weather model after it has read the days weather data and before other models access the weather data. The event is published to allow models like *RainfallModifier* the change weather data e.g. for climate change simulations.

The implementation is quite simple: 

1. It stores the weather models rainfall into the *OriginalRain* property.
2. It tests the current simulation date (*clock.Today*) to see if it is greater than the user specified *StartDate*
3. If the simulation date is greater than the start date it modifies the rainfall.

