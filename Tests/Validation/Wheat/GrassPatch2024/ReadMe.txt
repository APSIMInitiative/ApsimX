- *What?* Base simulation copied from https://github.com/FAR-Australia/UOM2312-001RTX/tree/master 
- *When?* 2026-02-09
- *Who?* Ben Jones ben.jones@faraustralia.com.au
- *Changes requested* by Hamish hamish.brown@plantandfood.co.nz:
	- Average the 4 reps and add into sim as 1 point
	- Force fit of Haun Stage with Observation (as model input)
	- Force fit of phenology from observations (as model input)
	
*Changes made* to original "Grass Patch.apsimx" (now GrassPatch2024.apsimx):
- Removed graph filtering TOS = 1 from json (unused label that led to crash)
- Pointed to Observed_GrassPatch2024.xlsx (mean of 4 reps) as instead of Observed.xlsx (all rep values)
- Disabled X:Y components for 1:1 graphs "DailyObsPred" and "HarvestObsPred" due to crash when asking for Wheat.Phenology.Stage