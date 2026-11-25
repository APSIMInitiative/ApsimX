#!/bin/bash

apsimng_image="apsiminitiative/apsimng:latest"

function initialise {
  # Install docker on the Azure node
  if ! pgrep -x "dockerd" > /dev/null;then
    sudo snap install docker
    sleep 10
  fi
  # Pull down the apsim next gen and apsim performance stats collector docker images
  sudo docker pull $apsimng_image
  sudo docker pull apsiminitiative/postats2-collector:latest
}

function run_00001 {
  echo ------------------------------ >> metadata.txt
  echo Date/time: `date +"%Y-%m-%d %T"` >> metadata.txt
  tar -xvf upload.tar.zst

  echo "Extracting password protected datasets..."

  mv sevenzip/7zz ./7zz

  soybean=./Tests/Validation/Soybean
  7zz x $soybean/ObservedFACTS.7z -o$soybean -p"$SOYBEAN_PASSWORD"

  cornsoy=./Tests/Validation/System/FACTS_CornSoy
  7zz x $cornsoy/FACTS_CornSoy.7z -o$cornsoy -p"$CORNSOY_PASSWORD"

  swim=./Tests/Validation/SWIM
  7zz x $swim/WaterTableSWIM_ISU_tests_May2022.7z -o$swim -p"$SWIM_PASSWORD"

  maize=./Tests/Validation/Maize
  7zz x $maize/5x5x5/obs.7z -o$maize/5x5x5 -p"$MAIZE_PASSWORD"
  7zz x $maize/EarManipulation/obs.7z -o$maize/EarManipulation -p"$MAIZE_PASSWORD"
  7zz x $maize/RM/obs.7z -o$maize/RM -p"$MAIZE_PASSWORD"

  rice=./Prototypes/Rice/Rice2026
  7zz x $rice/ObservedRice.7z -o$rice -p"$RICE_PASSWORD"

  thinzar_ryegrass=./Prototypes/ThinzarExperiment
  7zz x $thinzar_ryegrass/ThinzarObserved.7z -o$thinzar_ryegrass -p"$THINZAR_RYEGRASS_PASSWORD"

  uom_winter_spring=./Tests/Validation/Wheat/UoM_WinterVsSpring
  7zz x $uom_winter_spring/Observed.zip -o$uom_winter_spring -p"$UOM_WINTER_SPRING_PASSWORD"

  sudo --preserve-env docker run --rm -v $PWD/ApsimX:/wd -w=/wd --entrypoint dotnet $apsimng_image bin/Release/net10.0/Models.dll "$Path" --verbose
}

# Notify the A.V.A server that the job is complete.
# A job here is a single APSIM file which will be a single experiment.
function notify_job_complete {
  # Runs APSIM.Workflow from the apsimplusr container to post the result to A.V.A.
  # A failed notification must not change the job's exit code.
  local job_time
  job_time=$(printf '%.1f' "$elapsed")
  local log_file=/wd/local.stdout.txt
  sudo docker run --rm -v $PWD/ApsimX:/wd -w=/wd --entrypoint dotnet $apsimng_image bin/Release/net10.0/APSIM.Workflow.dll \
  job-complete \
  --pr-number "$PR_NUMBER" \
  --full-commit-hash "$FULL_COMMIT_HASH" \
  --job-path "$Path" \
  --job-time "$job_time" \
  --log-file "$log_file" || true
}

function run_00001_finally {
  # Function always called, regardless of any errors.
  sudo --preserve-env docker run --rm \
    -v $PWD/ApsimX:/wd \
    -w=/wd \
    -e POSTATS_UPLOAD_URL \
    apsiminitiative/postats2-collector:latest \
    upload "$PR_NUMBER" "$FULL_COMMIT_HASH" "$AUTHOR" "$TIME" "$PR_NUMBER-$COMMIT_SHA" "$Path"
  local postats_exit_code=$?
  notify_job_complete
  # Preserve the PO Stats upload result as this function's exit code.
  return $postats_exit_code
}

# ==============================================================
# Entry point for script.
start_time=$(date +%s.%N)
(
  echo ------------------------------------------------------------ &>> local.stdout.txt
  echo Running $1 &>> local.stdout.txt
  set -e;
  $1 &>> local.stdout.txt
);

exit_code=$?

# Return a failure exit code if the database is missing.
if [[ "$1" == run_00001 ]]; then
  database_path=".${Path#/wd}"
  database_path="${database_path%.apsimx}.db"
  if [[ "$Path" != /wd/*.apsimx || ! -f "$database_path" ]]; then
    echo "Database not found at $database_path; custom datastore paths are not supported." &>> local.stdout.txt
    if [[ $exit_code -eq 0 ]]; then
      exit_code=1
    fi
  fi
fi
end_time=$(date +%s.%N)
elapsed=$(echo "$end_time - $start_time" | bc -l)
echo "Elapsed time: $elapsed seconds" &>> local.stdout.txt

# If a finally function exists run it.
# This is done last so the finally function can use the complete log, exit code and elapsed time.
if [[ $(type -t $1_finally) == function ]]; then
  $1_finally &>> local.stdout.txt
  finally_exit_code=$?
  if [[ $exit_code -eq 0 && $finally_exit_code -ne 0 ]]; then
    exit_code=$finally_exit_code
  fi
fi

if [ $exit_code -gt 0 ]; then
  exit $exit_code
fi