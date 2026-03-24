echo "Extracting password protected datasets..."
test -z "$SOYBEAN_PASSWORD" && echo "SOYBEAN_PASSWORD is empty" && exit 1
test -z "$CORNSOY_PASSWORD" && echo "CORNSOY_PASSWORD is empty" && exit 1
test -z "$SWIM_PASSWORD" && echo "SWIM_PASSWORD is empty" && exit 1

soybean=/home/runner/work/ApsimX/ApsimX/Tests/Validation/Soybean
cornsoy=/home/runner/work/ApsimX/ApsimX/Tests/Validation/System/FACTS_CornSoy
swim=/home/runner/work/ApsimX/ApsimX/Tests/Validation/SWIM
maize=/home/runner/work/ApsimX/ApsimX/Tests/Validation/Maize

7z -p"$SOYBEAN_PASSWORD" x $soybean/ObservedFACTS.7z -o$soybean
7z -p"$CORNSOY_PASSWORD" x $cornsoy/FACTS_CornSoy.7z -o$cornsoy
7z -p"$SWIM_PASSWORD" x $swim/WaterTableSWIM_ISU_tests_May2022.7z -o$swim
7z -p"$MAIZE_PASSWORD" x $maize/5x5x5/obs.7z -o$maize/5x5x5
7z -p"$MAIZE_PASSWORD" x $maize/EarManipulation/obs.7z -o$maize/EarManipulation
7z -p"$MAIZE_PASSWORD" x $maize/RM/obs.7z -o$maize/RM