---
title: "Command Line"
draft: false
weight: 10
---

To run APSIM from the command line you need to locate the Models.exe binary file. On Windows this is located in

```
C:\Program Files\APSIM<Version-number>\bin\Models.exe
```

on LINUX it is located in

```
/usr/local/bin/Models.exe
```

## Command line usage.

To run simulation specific files from the command line:
  ```Models.exe file.apsimx file2.apsimx```

* __--recursive__ - Recursively search through subdirectories for files matching the file specification.
  ```Models --recursive dir/*.apsimx```
* __--upgrade__ -  Upgrade a file to the latest version of the .apsimx file format without running the file.
* __--run-tests__ - After running a file, run all tests inside the file.
* __--verbose__ - Write detailed messages to stdout when a simulation starts/finishes.
* __--csv__ - After running all files, export all reports to .csv files
* __--merge-db-files__ - Merge multiple .db files into a single .db file.
  ```Models.exe --merge-db-files site1.db site2.db```
* __--list-simulations__ - write the names of all simulations in a .apsimx file to stdout. The files are not run.
* __--list-enabled-simulations__ - write the names of all _enabled_ simulations in a .apsimx file to stdout. The files are not run.
* __--list-referenced-filenames__ -   write the names of all files that are referenced by an .apsimx file(s) to stdout e.g. weather files, xlsx files.
* __--single-threaded__ - Run all simulations sequentially on a single thread.
* __--simulation-names__ -  Only run simulations if their names match a regular expression.
  ```Models.exe file1.apsimx --simulation-name *Australia*```
* __--apply__ -  Apply commands from a .txt file. Can be used to create new simulations and modify existing ones. [Click here for more info](/usage/commandline/commandlanguage)
  ```
  Models.exe --apply commands.txt
  Models.exe file1.apsimx --apply commands.txt
  ```
* __--playlist__ -  Allows a group of simulations to be selectively run. Requires a playlist node to be present in the APSIM file. [Click here for more info](/usage/commandline/playlist)
  ```
  Models.exe file1.apsimx --playlist playlist1
  ```
* __--log__ - Change the log (summary file) verbosity.
  ```
  Models.exe example.apsimx --log error
  Models.exe example.apsimx --log warning
  Models.exe example.apsimx --log information
  Models.exe example.apsimx --log diagnostic
  Models.exe example.apsimx --log all
  ```
* __--in-memory-db__ - Use an in memory database rather than writing simulation output to a .db file.
  ```Models.exe example.apsimx --in-memory-db```
* __--batch__ -  Allows the use of a .csv file to specify values of variables than can be substituted into a command file (--apply). [Click here for more info](/usage/commandline/batch)
  ```Models.exe --apply command.txt --batch values.csv```
* __--file-version-number__ - Write the file version number of an apsimx file to stdout.
  ```Models.exe File1.apsimx --file-version-number```
* __--help__ - Write all command line switches to stdout.
* __--version__ - Write the APSIM version number to stdout.
