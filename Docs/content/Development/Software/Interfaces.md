---
title: "Software interfaces"
draft: false
---

A model needs to be loosely coupled to other models to allow it to be replaced by an alternate implementation. To enable this, it is important that models implement the required interfaces.

| Model type                    | Description |
|-------------------------------|-------------------------------------------------------|
| Plant                         | To allow users to call methods for Sow, Harvest, EndCrop implement [IPlant](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Core/IPlant.cs)|
|                               | To allow MicroClimate to calculate and arbitrate canopy light interception implement [ICanopy](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Interfaces/ICanopy.cs) |     
|                               | To allow SoilArbitrator to calculate and arbitrate water and nutrient uptake implement [IUptake](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Interfaces/IUptake.cs) |
|                               | To allow plant consumers (pest / diseases / stock) to damage a plant implement [IPlantDamage](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/PMF/Interfaces/IPlantDamage.cs) and [IOrganDamage](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/PMF/Interfaces/IOrganDamage.cs)|
| Soil water                    | To allow models to get water variables implement [ISoilWater](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Interfaces/ISoilWater.cs)|
| Nutrient                      | To allow models to get nutrient variables implement [ISolute](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Soils/Nutrients/ISolute.cs) and [INutrient](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Soils/Nutrients/INutrient.cs)|
| Surface organic matter        | To allow models (e.g. Plant, Stock, SimpleGrazing) to transfer biomass to the surface residue pools and to get surface residue variables implement [ISurfaceOrganicMatter](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Interfaces/ISurfaceOrganicMatter.cs) |
|                               | To allow plant consumers (pest / diseases / stock) to damage surface residues implement [IPlantDamage](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/PMF/Interfaces/IPlantDamage.cs) and [IOrganDamage](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/PMF/Interfaces/IOrganDamage.cs)|
| Weather                       | To allow models to get weather data implement [IWeather](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Interfaces/IWeather.cs)|
| Soil temperature models       | To allow models to get soil temperature variables implement [ISoilTemperature](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Interfaces/ISoilTemperature.cs)|


The converse of the above table also holds true. For example, a canopy arbitration model must **look for and use** models that implement [ICanopy](https://github.com/APSIMInitiative/ApsimX/blob/master/Models/Interfaces/ICanopy.cs).
