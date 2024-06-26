---
title: "Module 4: Sowing A Crop"
draft: false
---
<p style="font-size: 10px">Created 23/02/2023 - Last updated 05/03/2023</p>

<em style="color: red"> IMPORTANT NOTE: It is highly recommended that you upgrade your APSIM Next Gen version to at least version 2023.2.7164.0 or later.</em>

## Sowing A Crop

In this exercise you will observe the growth of a crop over a single season. 
You will learn a bit more about how to use APSIM to do a 'what-if' experiment with fertiliser rates. 
These skills can not only be used to experiment with sowing fertiliser rates but also variables such as:

- Time of planting.
- Rate of sowing.
- Crop comparisons and different starting soil moisture conditions.

1. Start a new simulation using the `Wheat.apsimx` example.
![Open example simulation](/images/moduleFourImages/img1.png)
	- you can find this in the `open an example` menu item
2. Rename the simulation as `Wheat`. 
3. Save this simulation as `Module4`.
![Save the simulation](/images/moduleFourImages/img2.png)
4. Make sure that `AU_Dalby.met` is the selected met file under the weather node.
5. Set the start and end dates of the simulation as `1/01/1989 - 31/12/1989` under the `Clock` node.
![Set dates](/images/moduleFourImages/img3.png)
6. Set the starting water to 25% full - filled from top. This is under `Soil` then `Water`.
![Set starting water](/images/moduleFourImages/img4.png)
7. Set the `initial values` column of both `NO3` and `NH4` to `kg/ha`.
	![NO3](/images/moduleFourImages/img5.png)
	![NH4](/images/moduleFourImages/img6.png)
8. Now let's make some changes to the `Fertilise at sowing` management node.
9. Change the `Fertilise at sowing` parameter `Amount of fertiliser to be applied (kg/ha)` to `0`:
![SowingFertiliser to 0](/images/moduleFourImages/img7.png)
10. Let's run the simulation and then inspect the `Graph` graph.
10. Make sure the x and y axes are set to `Clock.Today` and `Yield` respectively.
10. Rename the graph to `Wheat Yield Time Series` and its' `Series` to `Wheat Yield`.
![Graph](/images/moduleFourImages/img8.png)
11. We can see with 0 sowing fertiliser we achieved a yield of almost 900 kg/ha.
12. Next we will create an experiment where we alter the sowing fertiliser amount for this year.
We will see how this affects the yield.

## Creating an experiment

1. First add a `Experiment` node to the `Simulations` node at the top of the simulations tree.
![Add an experiment](/images/moduleFourImages/img9.png)
2. The `Experiment` node will be added to the bottom of the tree, hold `ctrl` key and press `up arrow` several times until it is directly below the `Simulations` node.
3. Add a `Factors` node to this experiment.
![Add a Factors node](/images/moduleFourImages/img10.png)
4. Add a `Factor` node to the `Factors` node.
![Add a Factor node](/images/moduleFourImages/img12.png)
5. To be able to change factors in our experiment, we will have to add our `Wheat` simulation to the experiment as a child node.
6. Drag and drop the `Wheat` simulation node onto the `Experiment` node.
![Add wheat to experiment](/images/moduleFourImages/img13.png)
7. Delete the `Wheat` simulation that is not a child of the experiment node.
![Delete wheat](/images/moduleFourImages/img14.png)
8. Next, we will create several versions of our experiment with varying sowing fertiliser amounts.
9. To do this, add this line to the `Factor` node: `[Fertilise at sowing].Script.Amount = 0 to 60 step 30`
![Create experiment variations](/images/moduleFourImages/img15.png)
10. If we click back on the `Experiment` node, you can see 3 differing amounts of sowing fertiliser.
![See variations](/images/moduleFourImages/img16.png)
11. To see the results lets drag a copy of the `Wheat Yield Time Series` graph onto the `Experiment` node.
Change the variables to reflect the image below:
![Setup graph](/images/moduleFourImages/img17.png)
12. Run the simulation. You'll see that the sowing fertiliser amounts increase the yield of the wheat crop by varying degrees.
![Final graph](/images/moduleFourImages/img18.png)

*Congratulations on completing the 4th module*

<i><span style="color:red;">Note</span>: If you found any incorrect/outdated information in this tutorial, please let us know on GitHub by <a href="https://www.github.com/APSIMInitiative/ApsimX/issues/new/choose/">submitting an issue.</a></i>




	