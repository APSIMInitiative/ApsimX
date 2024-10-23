# Location Generator

This tool takes an existing `.apsimx` file and generates a field with weather data and soil profile based on GPS location. The weather data comes from [NASA POWER](https://power.larc.nasa.gov/). The soil profile comes from [SSURGO](https://www.nrcs.usda.gov/resources/data-and-reports/soil-survey-geographic-database-ssurgo).

## Installing required packages

Install the required packages with the following command.

```bash
Rscript Requirements.R
```

You may need to run the install command manually and answer prompts to install to user directory.

```bash
R
> install.packages("jsonlite")
```

Required to install `udunits` for the `sp` package and `gdal` for the `sf` package.

## Example

In the directory are `fallow1.apsimx` and `fields.json` that can be used to run the example. Files `fallow1.apsimx` and `fallow1-bak.apsimx` are duplicates since the script modifies them in-place. After running the script `fallow1.apsimx` will contain updates soil parameters.

```bash
Rscript LocationGenerator.R
```

The script modifies the `.apsimx` in place and places the `.met` file in the same directory as the `.apsimx` file. The following is a typical output:

```
Linking to GEOS 3.12.2, GDAL 3.9.1, PROJ 9.4.1; sf_use_s2() is TRUE
To access larger datasets in this package, install the spDataLarge
package with: `install.packages('spDataLarge',
repos='https://nowosad.github.io/drat/', type='source')`
$basefile
[1] "MetompkinFarm.apsimx"

$basedir
[1] "."

$start_date
[1] "2022-07-19"

$end_date
[1] "2023-11-09"

$lat
[1] 37.7448

$lon
[1] -75.5833

Edited (node):  Clock 
Edited (child):  none 
Edited parameters:  Start End 
New values:  2022-07-19 2023-11-09 
Created:  ./MetompkinFarm.apsimx 
Created:  ./MetompkinFarm.apsimx 
Warning message:
In amp_apsim_met(pwr) :
  Year: 2022 was not used for calculating 'amp' because less than 300 days were present
Edited (node):  Weather 
Edited (child):  none 
Edited parameters:  
New values:  2022-07-19_2023-11-09_-75.5833,37.7448.met 
Created:  ./MetompkinFarm.apsimx 
```