---
title: "Module 5: Long-term simulations"
draft: false
---
<p style="font-size: 10px">Created 07/03/2023</p>

<em style="color: red"> IMPORTANT NOTE: It is highly recommended that you upgrade your APSIM Next Gen version to at least version 2023.2.7164.0 or later.</em>

## Long-term Simulations

### Chickpea sowing rates - 40 year runs

In this exercise you will use sowing rules to plant Chickpea crops and observe yield probabilities for a 40 year period given a half full soil moisture profile at sowing. 
We will compare two sowing rate strategies for these conditions with the goal of maximising yield. 
The weather will be different each year but the soil starting conditions will be the same.

By default, in long term simulations (i.e. longer than one year), the end of one years' simulation becomes the starting point of the next year. 
This is useful if you are interested in seeing the degradation or improvement of the soil over a long time period. 
But what if you wanted to work out what the best strategy would be for the current year using weather scenarios from the past 40 years? 
To do this we could create 40 different simulations all with the same starting conditions but a different weather file (which would be a lot of work), 
or we could run the same simulation over 40 years and just reset the starting conditions each year (much simpler).

We can then try different management strategies to see which one would have worked best under the past 10 years weather scenarios.

1. Start a new simulations using `Wheat` example. 
2. Rename the `simulation` as `Chickpea` and save the simulation as `Module5`.
3. Change the `Clock` nodes' start and end date values to: `1/01/1940` and `31/12/1980`
4. Copy the `Heavy clay` soil from the `Training toolbox` and delete the exisiting `Soil` node.
5. Set the starting `Water` to `50%` full - filled from top.
6. Set NO3 `Depth` to `0-1800` and `initial value` to `20kg/ha` (right-click heading to change units).
7. Set NH4 `Depth` to `0-1800` and `initial value` to `0kg/ha`.
8. Change the `SurfaceOrganicMatter` node's type to sorghum (don't forget to rename the pool name to `sorghum` as well), 
initial surface residue: `550 kg/ha`, Carbon:Nitrogen ratio of `76`, leave the Fraction of residue standing as is.
8. Right-click `Field` and add a `Chickpea` plant node.
9. In `SowingRule1` change the values to match the following:
![SowingRule1 values](/images/moduleFiveImages/img1.png)
10. In addition to this, to make it work correctly with the Chickpea node, we need to modify the underlying Script.
11. Clear the script completely and copy and paste this script into the `Script` input for `SowingRule1`:
		
        using Models.Interfaces;
        using System.Linq;
        using System;
        using Models.Core;
        using Models.PMF;
        using Models.Soils;
        using Models.Soils.Nutrients;
        using Models.Utilities;
        using APSIM.Shared.Utilities;
        using Models.Climate;

        namespace Models
        {
            [Serializable]
            public class Script : Model
            {
                [Link] private Clock Clock;
                [Link] private Fertiliser Fertiliser;
                [Link] private Summary Summary;
                [Link(ByName = true)] private Plant Chickpea;
                [Link] private Soil Soil;
                private Accumulator accumulatedRain;
                [Link]
                private ISoilWater waterBalance;
        
                [Description("Start of sowing window (d-mmm)")]
                public string StartDate { get; set;}
                [Description("End of sowing window (d-mmm)")]
                public string EndDate { get; set;}
                [Description("Minimum extractable soil water for sowing (mm)")]
                public double MinESW { get; set;}
                [Description("Accumulated rainfall required for sowing (mm)")]
                public double MinRain { get; set;}
                [Description("Duration of rainfall accumulation (d)")]
                public int RainDays { get; set;}
                [Description("Cultivar to be sown")]
                [Display(Type=DisplayType.CultivarName, PlantName = "Chickpea")]
                public string CultivarName { get; set;}
                [Description("Sowing depth (mm)")]
                public double SowingDepth { get; set;}        
                [Description("Row spacing (mm)")]
                public double RowSpacing { get; set;}    
                [Description("Plant population (/m2)")]
                public double Population { get; set;}    
        
        
                [EventSubscribe("StartOfSimulation")]
                private void OnSimulationCommencing(object sender, EventArgs e)
                {
                    accumulatedRain = new Accumulator(this, "[Weather].Rain", RainDays);
                }
        

                [EventSubscribe("DoManagement")]
                private void OnDoManagement(object sender, EventArgs e)
                {
                    accumulatedRain.Update();
            
                    if (DateUtilities.WithinDates(StartDate,Clock.Today,EndDate) &&
                        !Chickpea.IsAlive &&
                        MathUtilities.Sum(waterBalance.ESW) > MinESW &&
                        accumulatedRain.Sum > MinRain)
                    {
                       Chickpea.Sow(population:Population, cultivar:CultivarName, depth:SowingDepth, rowSpacing:RowSpacing);    
                    }
        
                }
        
            }
        }

10. Run the simulation.
11. You should get an error with the message: Cannot find a soil crop parameterisation called ChickpeaSoil
	- This is caused by using a soil that is missing a `SoilCrop` node (located under `Physical`) with the same name as the plant node you are using.
12. To fix this error, copy and paste the `WheatSoil` node, located under `Heavy Clay` > `Physical`. Rename `WheatSoil1` to `ChickpeaSoil`.
13. Your simulation should now run successfully and you should have data in your `DataStore` node.
![Datastore node](/images/moduleFiveImages/img2.png)
14. Add a management script called `Reset on date` to the `Chickpea` simulation (found in `Management Toolbox` under `Other`)
![reset on date location](/images/moduleFiveImages/img3.png)
15. Leave all parameters as yes and change the `date to reset on` to `1-may`. 
    - <strong style="color: red;">note:</strong> make sure this management script sits above all other management scripts in the tree as they are ran in the order they are arranged from top to bottom.
16. Add the below to the Report variables.   

        [Clock].Today
        [Chickpea].Grain.Total.Wt*10 as Yield

17. Make report frequency as:

        [Chickpea].Harvesting

17. Remove `SowingFertiliser` management node.
16. Rename the `Chickpea` simulation as `Chickpea 10 plants`.
17. Copy `Chickpea 10 plants` and rename it `Chickpea 15 plants`.
18. Change the `Plant population(/m2)` to `15` in the `SowingRule1` management script.
19. Run the simulations.
20. Create a graph. Rename it `Total chickpea yield time series` with a series that plots `[Clock].Today` and `Yield`.
21. Now you have a graph showing yield when comparing differing plant density of 40 years.
![Final total yield graph](/images/moduleFiveImages/img4.png)



