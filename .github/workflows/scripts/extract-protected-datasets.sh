echo "Extracting password protected datasets..."
test -z "$SOYBEAN_PASSWORD" && echo "SOYBEAN_PASSWORD is empty" && exit 1
test -z "$CORNSOY_PASSWORD" && echo "CORNSOY_PASSWORD is empty" && exit 1
test -z "$SWIM_PASSWORD" && echo "SWIM_PASSWORD is empty" && exit 1

soybean=/home/runner/work/ApsimX/ApsimX/Tests/Validation/Soybean
cornsoy=/home/runner/work/ApsimX/ApsimX/Tests/Validation/System/FACTS_CornSoy
swim=/home/runner/work/ApsimX/ApsimX/Tests/Validation/SWIM

7z -p"$SOYBEAN_PASSWORD" x $soybean/ObservedFACTS.7z -o$soybean
7z -p"$CORNSOY_PASSWORD" x $cornsoy/FACTS_CornSoy.7z -o$cornsoy
7z -p"$SWIM_PASSWORD" x $swim/WaterTableSWIM_ISU_tests_May2022.7z -o$swim