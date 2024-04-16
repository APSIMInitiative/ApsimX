---
title: "6. Download Results"
draft: false
---

## Output Files


When the simulations have finished running, all output files are copied into the job output container.

#### Results
An APSIM Classic job produces .out result files. An APSIM Next Generation job produces .db files. 
If you are using a recent version of the job manager, then the result files will first be zipped up before being copied.


#### Debug Files
In addition to the raw result files, the job manager produces a st of 'debug' (.stdout) files, which provide information that's useful when trying to find out what's going wrong with a simulation or job. 
These debugging files are typically much smaller than the output files, and are not zipped up.

The job preparation and release tasks both produce a debug file, and each simulation being run will also produce a debug file.

## Download Options

1. Include Debug Files

The .stdout debugging files will be downloaded if and only if this is checked.

2. Keep raw output files

The raw result files will be saved to the output directory if and only if this is checked.

3. Collate Results

The result files will be combined into a single CSV file if and only if this is checked.

4. Output directory

This option allows the user to select a directory to download the results to. A subdirectory will be created here called %JobName%_Results, and the results will be saved to this subdirectory.

## Job Downloader Logic

* If the user does not want the raw results or a CSV file, then no result files (zipped or unzipped) will be downloaded. 
* If the result files are needed, the job downloader will attempt to download all zip files located in the job output container, but if it finds none, it will download each result file individually.
* Once the zip files are downloaded, their contents will be extracted and the zip files deleted.
* If the user wants a CSV but no raw result files, the result files will be deleted after being combined into a CSV.
* Download progress is calculated as the number of files downloaded out of the total number of files to download; it does not take into account the time needed to summarise (combine into a CSV) or delete the result files.

## Remarks

* Closing the APSIM UI will stop any downloads in progress.
* If the job to be downloaded is not complete, the job downloader thread will do a busy wait until the job is finished and then download the results. 
  * If you did **not** choose to download the job asynchronously, the APSIM UI will be frozen until the job finishes. In this case, you will have to kill the APSIM process or just wait until the job finishes. I would not recommend waiting, as jobs will always take at least 5 minutes (due to the time it takes for the Azure VMs to boot), and because the UI is frozen, you will have no way of knowing how close the job is to finishing.