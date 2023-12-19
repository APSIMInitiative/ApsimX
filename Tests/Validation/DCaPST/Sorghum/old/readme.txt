The fixed directory contains old apsim daily outputs from running the validation set with fixed tillering.

The dynamic directory contains old apsim daily outputs from running the validation set with dynamic tillering.

Running merge.py will combine all .out files into two .csv files (one for fixed tillering, one for dynamic).

If you update the .out files, you must remember to rerun merge.py, otherwise the .csv files will still contain the old data.
