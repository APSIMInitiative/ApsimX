# Background

The .apsimx file is deserialised into a _IModel_ instance hierarchy where each instance maintains a reference to a
parent _IModel_ and a list of _IModel_ child instances. This hierarchy requires all instances to be derived from
_IModel_ which has the disadvantage of excluding POCO (plain old class objects).

# Design

The code in this directory allows POCO's to be added. To enable this, POCO's are wrapped in a _ClassAdaptor_ instance which has a
single property _Obj_ which contains the reference to the POCO instance. For example, _APSIM.Soils.Organic_ is a POCO
so it is wrapped inside a _ClassAdaptor_. The .apsimx file looks like this:

```json
{
    "$type": "Models.Core.ClassAdaptor, Models",
    "Obj": {
        "$type": "APSIM.Soils.Organic, APSIM.Soils",
        "FOMCNRatio": 40.0,
        "Thickness": [
            ...
        ],
        "Carbon": [
            ...
        ],
        "SoilCNRatio": [
            ...
        ],
        "FBiom": [
            ...
        ],
        "FInert": [
            ...
        ],
        "FOM": [
            ...
        ],
    },
    "Name": "ClassAdaptor",
    "ResourceName": null,
    "Children": [],
    "Enabled": true,
    "ReadOnly": false
    },
```

This allows the POCO to be deserialised from the .apsimx file. To have the POCO show in the user interface and in the running simulation, the code in this directory converts the above _IModel_ hierarchy into a _ModelNodeTree_ where each _ModelNode_ contains the name of the APSIM model and its parent and child nodes. All other code in APSIM (excluding serialising / deserialising / converter code) uses
this _ModelNodeTree_ rather than the _IModel_ hierarchy.

For example, given this simulation:

```
Simulation
   + Clock
   + Zone
     + ClassAdapter
       + POCO
   + ...
```

the _ModelNodeTree_ would look like this:

```json
{
    "Name": "Simulation",
    "Parent": null,
    "FullPath": ".Simulation",
    "Instance": Simulation instance,
    "Children": [
        {
            "Name": "Clock",
            "Parent": Simulation,
            "FullPath": ".Simulation.Clock",
            "Instance": clock instance,
            "Children": null
        },
        {
            "Name": "Zone",
            "Parent": Simulation,
            "FullPath": ".Simulation.Zone",
            "Instance": Zone instance,
            "Children": [
                {
                    "Name": "POCO",
                    "Parent": zone,
                    "FullPath": ".Simulation.Clock",
                    "Instance": "POCO instance"
                    "Children": null
                }
            ]
        }
    ]
}
```

Note how the _ClassAdaptor_ disappears in _ModelNodeTree_. This provides a cleaner look in the GUI and for reporting, intellisense etc.
