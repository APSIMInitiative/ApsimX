The simulations directory contains old apsim daily outputs from running the validation set with dcapst enabled but no root shoot adjustment (divide by 2).

Running merge.py will combine all .out files into a combined_simulations.csv file.

If you update the .out files, you must remember to re-run merge.py, otherwise the .csv files will still contain the old data.
