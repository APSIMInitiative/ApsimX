The fixed directory contains old apsim daily outputs from running the validation set with fixed tillering.

The dynamic directory contains old apsim daily outputs from running the validation set with dynamic tillering.

Running merge.py will combine all .out files into two .csv files (one for fixed tillering, one for dynamic).

If you update the .out files, you must remember to rerun merge.py, otherwise the .csv files will still contain the old data.

Note: I think the python script spits out dates as dd/MM/yyyy, which doesn't work too well in non-australian locales

Need to adjust the python script at some point. For now, the quick and dirty fix is to find-replace with this regex. Find:

^(\d{2})/(\d{2})/(\d{4})

Replace:

$3-$2-$1