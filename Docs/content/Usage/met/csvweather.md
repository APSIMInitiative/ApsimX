---
title: "Using a .csv file for weather data"
draft: false
---

It is possible to use weather data stored in a csv file. When doing so, it is necessary to provide an extra plaintext file containing some constants. This document describes the format of the two files. For an example of an .apsimx file which uses a .csv file for its weather data, see the CsvWeather.apsimx example file which is provided with all apsim installations.

**Note that the constants file *must* contain a latitude.**

## Format of the .csv file

The .csv file must be [RFC 4180](https://tools.ietf.org/html/rfc4180)-compliant. The first row of the .csv file must contain the column names, and there is currently no support for specifying units. Expected units are:

- °C for temperatures
- MJ/m2 for radiation
- mm for rain
- hPa for pressure

An example .csv weather file might look like:

```
year,day,radn,maxt,mint,rain,pan,vp,code
1900,1,24.0,29.4,18.6,0.0,8.2,20.3,300070
1900,2,25.0,31.6,17.2,0.0,8.2,16.5,300070
1900,3,25.0,31.9,16.6,0.0,8.2,14.8,300070
```

## Format of the constants file

The constants file can be any plain text file with zero or more lines of the form:

constant_name = value

Units may optionally be specified in parentheses after the value. Any text after an exclamation mark (!) character are treated as a comment.

For example:

```
location = dalby
latitude = -27.18  (DECIMAL DEGREES)
longitude = 151.26  (DECIMAL DEGREES)
tav =  19.09 (oC) ! annual average ambient temperature
amp =  14.63 (oC) ! annual amplitude in mean monthly temperature
```

The only mandatory constant is latitude, however it can often be useful to provide others such as long-term averages for tav and amp.