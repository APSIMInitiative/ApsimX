---
title: "Accessing non-standard columns of data from .met file"
draft: false
---

It is possible to access custom data stored in a .met file from a manager script.

Met file:

```
[weather.met.weather]
latitude = -27  (DECIMAL DEGREES)
longitude = 150  (DECIMAL DEGREES)
tav =  19.09 (oC)
amp =  14.63 (oC)
year  day radn  maxt   mint  rain  pan    vp      code    my_column_name
 ()   () (MJ/m^2) (oC) (oC)  (mm)  (mm)   (hPa)     ()                ()
1900   1   24.0  29.4  18.6   0.0   8.2  20.3 300070                   6
1900   2   25.0  31.6  17.2   0.0   8.2  16.5 300070                   7
1900   3   25.0  31.9  16.6   0.0   8.2  14.8 300070                   2
1900   4   24.0  33.8  16.8   0.0   8.2  17.5 300070                   3
1900   5   24.0  33.3  19.3   0.0   8.4  18.3 300070                   4
```

Manager script:

```csharp
using System;
using Models.Core;
using Models.Climate;

namespace Models
{
    [Serializable]
    public class Script : Model
    {
        [Link] private Weather weather;
        
        public double MyColumn
        {
            get
            {
                return weather.GetValue("my_column_name");
            }
        }
    }
}
```

Then the MyColumn variable can be reported (e.g. as `Manager.Script.MyColumn`).

![Screenshot of report data](/images/CustomMetDataReport.png)