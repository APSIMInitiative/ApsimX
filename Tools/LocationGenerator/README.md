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

The script modifies the `.apsimx` in place and places the `.met` file in the same directory as the `.apsimx` file.