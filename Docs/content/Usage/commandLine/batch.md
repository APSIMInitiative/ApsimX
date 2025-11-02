---
title: "Batch files"
draft: false
weight: 30
---

## Making repeated changes to many files (batching)
* For situations where you need to make the same changes to many apsim files but need specific nodes or parameters changed,
the `--apply` switch can be used in conjunction with the `--batch` switch.
* An example where this would be useful is when you want to change the soil and weather for each individual APSIM file and you have 10s to 100s to 1000s of APSIM files.

* To do this you will need two specific files along with any APSIM files you want to change, these files are:

    * A config file containing 'placeholders'
        * the placeholders are the values that will be replaced by the values in the batch file
        * a placeholder is a name that starts with a ```$``` symbol. An example would be `$weather-file-name`.
        * placeholders cannot contain spaces.
        * an example config file:

        ```
        load BaseCl.apsimx
        [Soil]=SoilLibrary.apsimx;[$soil-name]
        [Weather].FileName=$weather-file-name
        [SimulationExp].Name=$sim-name
        run
        ```

    * A batch file, this is a csv file with headers that match the placeholders (minus the ```$``` symbols)
        * for each row in the batch file a run through of the config file is completed.
        * an example batch file:


        |soil-name|weather-file-name|sim-name|
        |----|----|----|
        |Ahiaruhe_1a1|16864.met|Sim0001|
        |Ahuriri_7a1|19479.met|Sim0002|
        |Ailsa_5a1|19479.met|Sim0003|


* To run this we would run something like: `"C:\Program Files\APSIM<your version number>\bin\Models.exe" --apply config-file-name.txt --batch batch-file-name.csv`
