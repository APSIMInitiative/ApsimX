# Instructions for AI Agents working with Models/CLEM

## Overview

- CLEM is one of the model of APSIMX which performs the simulation of a whole-farm handling all resources required and activities performed
- The model has a ruminant sub-model able to simulate individuals in the herd which can include thousands of individuals
- CLEM does not interact with other models in APSIMX except AgPasture, but it can be used as a standalone model or as a sub-model of other models.

## Coding conventions to follow

- Use clear naming for daily vs timestep quantities in activities and resources code to enhance readability and maintainability.

## Agent efficiency tips

- Prioritize lightweight per-ruminant timestep logic in modelling to enhance performance and clarity.
- Use existing APSIMX features and libraries to avoid unnecessary code duplication and to leverage tested functionalities.

## permissions

- Ask me before making any changes outside of the Models/CLEM folder, as this may affect other models in APSIMX.
-