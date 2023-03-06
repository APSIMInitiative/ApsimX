---
title: "Module 4: Sowing A Crop"
draft: false
---
<p style="font-size: 10px">Created 23/02/2023</p>

<em style="color: red"> IMPORTANT NOTE: It is highly recommended that you upgrade your APSIM Next Gen version to at least version 2023.2.7164.0 or later.</em>

## Sowing A Crop

In this exercise you will observe the growth of a crop over a single season. 
You will learn a bit more about how to use APSIM to do a 'what-if' experiment with fertiliser rates. 
These skills can not only be used to experiment with fertiliser rates but also variables such as:

- Time of planting.
- Rate of sowing.
- Crop comparisons and different starting soil moisture conditions.

1. Start a new simulation using the `sorgum.apsimx` example.
	- you can find this in the `open an example` menu item
2. Rename the simulation as `Sorghum 0kg Fertiliser`. 
3. Save this simulation as `Module4`.
![Save the simulation](/images/moduleFourImages/img1.png)
4. Choose the `Dalby` met under the weather node.
5. Set the start and end dates of the simulation as `1/1/1988 - 30/6/1988` under the `Clock` node.
![Set dates](/images/moduleFourImages/img2.png)
6. Set the starting water to 25% full - filled from top. This is under `HRS` then `Water`.
![Set starting water](/images/moduleFourImages/img4.png)
7. Set the starting nitrogen to 12 kg/ha of NO3 and 3kg/ha of NH4, evenly distributed; refer to water node to get the depth range.
	![NO3](/images/moduleFourImages/img5.png)
	![NH4](/images/moduleFourImages/img6.png)
8. Copy and paste a `Sowing using a variable rule` management node from the management toolbox:
9. Change the rules to reflect the following:
![Set sowing rules](/images/moduleFourImages/img7.png)
10. By setting the sowing window start and end to the same data, we force the crop to be sown on that particular day. 
For other crops there is a specific `sow on a fixed date` management node that is simpler than this one, but we`re sowing sorghum which has extra options so we need to use this one.
11. Change the `Fertilise at sowing` management node to reflect the following:
![Set Fertilise at sowing node](/images/moduleFourImages/img8.png)
12. Remove `Automatic irrigation based on water deficit` manager script.
13. Now that we have our base simulation configured, let's configure our report variables.

	