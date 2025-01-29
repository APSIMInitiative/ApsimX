# Process for debugging models side by side

* Run Ausfarm simulation on Bitzer (remote desktop) from RAD Studio so that code can be debugged (Models.groupproj - RAS studio project file)
* Log (log-ausfarm.txt) is written to desktop. Copy to local PC.
* To get output, right click on 'Output' in Ausfarm simulation tree and export to SQLITE DB. Copy the DB to local computer and using DBBrowser for SQLite export the Simulation table to AusfarmOutputs.csv.

* APSIM simulations are in ApsimX repo under Prototypes\Pasture\Comparisons\xxxxx

# Bugs:

Line 2501 pasture.cs: FInputs.Precipitation = locWtr.TomorrowsMetData.Rain;   // AUSFARM uses tomorrows rain data.
Line 2805 pasture.cs: FWeather[TWeatherData.wdtEpan] = locWtr.TomorrowsMetData.PanEvap;  // AUSFARM uses tomorrow's pan evap.
Line 2757 pasture.cs: FWeather[TWeatherData.wdtWind] = 0;   // AUSFARM DOESN'T HAVE DEFAULT WINDSPEED !!!!!
Line 2545 pasture.cs: In Ausfarm, the VPD calculation uses SVPfrac = 0.75 instead of 0.66 in APSIM (weather.cs)
Line 2736 pasture.cs: if (systemClock.Today != systemClock.StartDate) // Ausfarm doesn't seem to drop dead roots to soil on day 1.

Ausfarm doesn't take inert fraction into account when initialising humic pool

# Pasture_Wrap.pas

Ine 750: pasture-vars.inc: water demand and max water avail (water supply).

Line 535: TPastureInstance.passDrivers - pass driving variables to FModel.

Line 802: TPastureInstance.readProperty
    Called by infrastructure to get the value of a property.
    Calls: pasture_vars.pas (line 1091) getPastureValue that returns a TPropertyInfo

Line 895: TPastureInstance.processEventState Main wrapper entry point.
    Called by infrastructure to perform daily timestep, get, set variables.

    Sequence of (clock like) events:
    evtINITSTEP, evtWATER

    Line 985: Initialise time step, pass driving variables to FModel (TPasturePopulation - grazPASTPOPN.pas)

Line 1884: TPastureInstance.log - write to log file.

