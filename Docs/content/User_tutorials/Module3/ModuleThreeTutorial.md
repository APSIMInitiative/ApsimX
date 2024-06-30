---
title: "Module 3: The Nitrogen Cycle"
draft: false
---
<p style="font-size: 10px">Created 01/03/2023 - Last updated 23/05/2024</p>

<em style="color: red"> IMPORTANT NOTE: It is highly recommended that you upgrade your APSIM Next Gen version to at least version 2023.2.7164.0 or later.</em>

## The Nitrogen Cycle

In this exercise you will observe the cycle of fertiliser nitrogen in a fallow situation; urea to ammonium, ammonium to nitrate and the loss of soil nitrate via denitrification. 
This simulation will introduce editing a simple Manager rule and demonstrate more advanced features of graphing simulation results.

1. Start a new simulation based on the `Wheat` example.
2. Rename this simulation to Nitrogen Cycle.
3. Save this file as `Module3`.
4. The simulation will use a different weather file. To do this:
	- click the `weather` node
	- click the `browse` button
	- double-click `AU_Dalby` (C:\Program Files\APSIM[Version]\Examples\WeatherFiles)
	![Change weather image](/images/moduleThreeImages/img1.png)
5. In the `Clock` node, change the starting date to `1/1/1989` and the end date to `31/12/1989`
	![Clock variables](/images/moduleThreeImages/img2.png)
6. Add the `Heavy Clay` soil from the `Training toolbox`
7. Delete `Soil` node
8. Set `Percent full` to 50 in the `Heavy Clay's` `Water` node.
![change water % full](/images/moduleThreeImages/img3.png)
9. Set starting nitrogen to 19kg/ha NO3 and 0 NH4, evenly distributed. Don't forget to change units to kg/ha (right-click the column header `ppm`). 
Make the depth equal to the entire soil profile (check Water node for the profile depth).
![NO3 amount](/images/moduleThreeImages/img4.png)
![NH4 amount](/images/moduleThreeImages/img5.png)
10. Delete all manager scripts (all have a farmer icon: SowingFertiliser, Harvest, SowingRule1)
11. Copy a `Fertilise on fixed dates` management node to the field node. You can locate this by going to:
	- Home
	- Management toolbox
	- Fertilise folder
	- You can either drag this to your `Field` node or copy and paste it to the `Field` node.
	![Fertilise on fixed date node](/images/moduleThreeImages/img6.png)
12. Change `Type of fertiliser to apply?` to `UreaN`
13. Change fertilisation date to `10-Jan`
14. Change `Amount of fertiliser to be applied (kg/ha)` to `100`
15. In the `Report` node let set up the output variables:

		[Clock].Today
		[Weather].Rain
		[Soil].SoilWater.Drainage
		sum([Soil].SoilWater.ESW)
		sum([Soil].NO3.kgha) as NO3Total
		sum([Soil].NH4.kgha) as NH4Total
		sum([Soil].Nutrient.DenitrifiedN) as Denitrification
16. In the `Report events` section remove existing event variables. Then add:

        [Clock].EndOfDay
		
16. Run the simulation
17. Delete the current graph named `Wheat Yield Time Series`
18. Add another `Graph` node to the `Nitrogen Cycle` simulation node
19. Rename `Graph` to `Nitrogen`
20. Add a `Series` to `Nitrogen` graph
21. Rename `Series` to `NO3 total`
	- Variables for this `Series`
	![NO3 total series variables](/images/moduleThreeImages/img7.png)
22. Add another `Series` node to `Nitrogen`
23. Rename this `Series` to `NH4 total`
	- Variables for this `Series`
	![NH4 total series variables](/images/moduleThreeImages/img8.png)
24. Create a new graph with a series each for `rain`, `sum([Soil].SoilWater.ESW)`, `NO3Total`, `Denitrification`.
	- each series should have `[Clock].Today` as the `X` axis variable.
25. From this chart you can see that significant nitrogen is lost via denitrification when large amounts of nitrate are available in saturated soil conditions.
![Denitrification graph](/images/moduleThreeImages/img9.png)

*Congratulations on finishing the 3rd module!*

<i><span style="color:red;">Note</span>: If you found any incorrect/outdated information in this tutorial, please let us know on GitHub by <a href="https://www.github.com/APSIMInitiative/Apsimx/issues/new/choose/">submitting an issue.</a></i>