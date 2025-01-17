For each test

* Run Ausfarm simulation on Bitzer (remote desktop) from RAD Studio so that code can be debugged.
* Log (log-ausfarm.txt) is written to desktop. Copy to local PC.
* Outputs are written to L:\projects\ApsimX\Prototypes\Pasture\Validation\ryegrass_daily.txt and L:\projects\ApsimX\Prototypes\Pasture\Validation\ryegrass_daily_mass.txt. Copy the contents of these to worksheets in ausfarm-outputs.xlsx

* APSIM simulations are under ApsimX\Prototypes\Pasture\Comparisons\xxxxx

# Bugs:

Line 2501 pasture.cs: FInputs.Precipitation = locWtr.TomorrowsMetData.Rain;   // REPRODUCE BUG IN AUSFARM !!!!!
Line 2757 pasture.cs: FWeather[TWeatherData.wdtWind] = 0;   // AUSFARM DOESN'T HAVE DEFAULT WINDSPEED !!!!!
Line 2545 pasture.cs: In Ausfarm, the VPD calculation uses SVPfrac = 0.75 instead of 0.66 in APSIM (weather.cs)

# Pasture_Wrap.pas

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

