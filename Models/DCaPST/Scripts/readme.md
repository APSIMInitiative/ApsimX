# Manager Scripts

The manager scripts are an interface between APSIM classic and the DCaPST library. Because each crop is handled differently in APSIM, a different manager script is necessary for each crop type. Note, however, that the differences between pathways (e.g. C3, C4), comes down only to how the scripts are parameterised. For example, in the sample C3 and CCM wheat scripts, only the values of the parameters are changed.

The provided sorghum script is to highlight that each crop may have differences in the calling script. However, these differences should be limited to the values accessed from APSIM itself - the calls to the DCaPST library should be indentically structured across all scripts.

## DCaPST parameters

Provided are lists of all the initial parameters DCaPST requires values for. Note that highlighted parameters are only required the CCM pathway, and can be set to 0 in C3 / C4. Be sure to use the correct values for your situation.

### Canopy Parameters:
- Canopy type (C3, C4, CCM)
- CO2 partial pressure
- Empirical curvature factor
- **Diffusivity-solubility ratio**
- O2 partial pressure
- PAR diffuse extinction coefficient
- NIR diffuse extinction coefficient
- PAR diffuse reflection coefficient
- NIR diffuse reflection coefficient
- Leaf angle
- PAR leaf scattering coefficient
- NIR leaf scattering coefficient
- Leaf width
- SLN ratio at canopy top
- Minimum structural nitrogen
- Wind speed
- Wind speed profile distribution coefficient

### Pathway Parameters:
- Electron transport minimum temperature
- Electron transport optimum temperature
- Electron transport maximum temperature
- Electron transport scaling constant
- Electron transport Beta value
- Mesophyll conductance minimum temperature
- Mesophyll conductance optimum temperature
- Mesophyll conductance maximum temperature
- Mesophyll conductance scaling constant
- Mesophyll conductance Beta value
- Michaelis Menten constant of Rubisco carboxylation at 25 degrees C
- Michaelis Menten constant of Rubisco carboxylation temperature response factor
- Michaelis Menten constant of Rubisco oxygenation at 25 degrees C
- Michaelis Menten constant of Rubisco oxygenation temperature response factor
- Rubisco carboxylation to oxygenation at 25 degrees C
- Rubisco carboxylation to oxygenation temperature response factor
- **Michaelis Menten constant of PEPc activity at 25 degrees C** 
- **Michaelis Menten constant of PEPc activity temperature response factor**
- Rubisco carboxylation temperature response factor
- Respiration temperature response factor
- **PEPc activity temperature response factor**
- **PEPc regeneration**
- Spectral correction factor
- **Photosystem II activity fraction**
- Bundle sheath CO2 conductance 
- Max Rubisco activity to SLN ratio
- Max electron transport to SLN ratio
- Respiration to SLN ratio
- Max PEPc activity to SLN ratio
- Mesophyll CO2 conductance to SLN ratio
- Extra ATP cost
- Intercellular CO2 to air CO2 ratio