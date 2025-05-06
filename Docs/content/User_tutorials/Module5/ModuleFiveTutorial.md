---
title: "Module 5: Long-term simulations"
draft: false
---
<p style="font-size: 10px">Created 07/03/2023 - Last updated 05/03/2023</p>

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
8. Right-click `Field` and add a `Chickpea` plant node. All plants are found under the `PMF` folder.
9. Delete `Wheat`.
10. In `Sow using a variable` change the values to match the following:
![Sow using a variable's values](/images/moduleFiveImages/img1.png)
11. Run the simulation.
12. You should get an error that at it's end says: `.Chickpea.Field.Report can not find the component: [Wheat]`. This means we need to update the report variables and report events in `Report`. Let's change any that have `[Wheat]` to `[Chickpea]`. 
    - Remove `[Chickpea].Grain.Protein`.
    - Change the report variable `[Wheat].Phenology.Zadok.Stage` to `[Chickpea].Phenology.Zadok`. 
    - Also, remember to change the report event down the bottom `[Wheat].Harvesting`.
13. Run the simulation again.
14. You should get an error with the message: Cannot find a soil crop parameterisation called ChickpeaSoil
	- This is caused by using a soil that is missing a `SoilCrop` node (located under `Physical`) with the same name as the plant node you are using. For now, create a copy of `WheatSoil` under `Physical` and rename it `ChickpeaSoil` to fix this exception.
15. Before we run again, let's update our `Harvest` management node to look for the correct crop, now that `Wheat` has been removed and `Chickpea` has ben added.
16. Your simulation should now run successfully and you should have data in your `DataStore` node.
![Datastore node](/images/moduleFiveImages/img2.png)
17. Add a management script called `Reset on date` to the `Chickpea` simulation (found in `Management Toolbox` under `Other`)
![reset on date location](/images/moduleFiveImages/img3.png)
18. Leave all parameters as yes and change the `date to reset on` to `1-may`. 
    - <strong style="color: red;">note:</strong> make sure this management script sits above all other management scripts in the tree as they are ran in the order they are arranged from top to bottom.
20. Remove the `Fertilise at sowing` manager node.
21. Rename the `Chickpea` simulation as `Chickpea 10 plants`.
22. Copy `Chickpea 10 plants` and rename it `Chickpea 15 plants`.
23. Change the `Plant population(/m2)` to `15` in the `Sowing using a variable` management script.
24. Right-click the `DataStore` and click `Empty the datastore`. This makes sure that we have the most up to date data once we run, then run the simulations.
25. Create a graph under `Simulations`. Rename it `Total chickpea yield time series` with a series that plots `[Clock].Today` and `Yield`.
26. Now you have a graph showing yield when comparing differing plant density over 40 years. 
    - If you've copied the graph settings from the image below and you can't see both simulations data displayed. Make sure `Chickpea 15 plants` sits above `Chickpea 10 plants` in the tree.
![Final total yield graph](/images/moduleFiveImages/img4.png)


<i><span style="color:red;">Note:</span> If you found any incorrect/outdated information in this tutorial. Please let us know on GitHub by <a href="https://www.github.com/APSIMInitiative/ApsimX/issues/new/choose/">submitting an issue.</a></i>




