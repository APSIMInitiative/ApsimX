For each test

* Run Ausfarm simulation on Bitzer (remote desktop) from RAD Studio so that code can be debugged.
* Export outputs to sqlite db file on bitzer
* Copy exported .db file to local PC.
* Use SqLiteBrowser to export a table to a .csv.
* The APSIM simulation will import csv file (via Input under DataStore).

* I had to offset the rainfall data by a day. Seems that Ausfarm pasture is using tomorrows rain data.
* In Ausfarm, the VPD calculation uses SVPfrac = 0.75 instead of 0.66 in APSIM (weather.cs)
* pastureWaterDemand (potentialET) is very different on day 1 of simulation 2.3 (ausfarm) vs 0.2 (apsim) - unit conversion problem - don't think so.