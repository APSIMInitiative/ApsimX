#!/bin/bash

apsimng_image=apsiminitiative/apsimng:latest
stdout_file=local.stdout.txt

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

function run_setup {
  tar -xvf upload.tar.zst

  echo "Extracting password protected datasets..."

  mv sevenzip/7zz ./7zz

  soybean=./ApsimX/Tests/Validation/Soybean
  7zz x $soybean/ObservedFACTS.7z -o$soybean -p"$SOYBEAN_PASSWORD"
  unset SOYBEAN_PASSWORD

  cornsoy=./ApsimX/Tests/Validation/System/FACTS_CornSoy
  7zz x $cornsoy/FACTS_CornSoy.7z -o$cornsoy -p"$CORNSOY_PASSWORD"
  unset CORNSOY_PASSWORD

  swim=./ApsimX/Tests/Validation/SWIM
  7zz x $swim/WaterTableSWIM_ISU_tests_May2022.7z -o$swim -p"$SWIM_PASSWORD"
  unset SWIM_PASSWORD

  maize=./ApsimX/Tests/Validation/Maize
  7zz x $maize/5x5x5/obs.7z -o$maize/5x5x5 -p"$MAIZE_PASSWORD"
  7zz x $maize/EarManipulation/obs.7z -o$maize/EarManipulation -p"$MAIZE_PASSWORD"
  7zz x $maize/RM/obs.7z -o$maize/RM -p"$MAIZE_PASSWORD"
  unset MAIZE_PASSWORD

  rice=./ApsimX/Prototypes/Rice/Rice2026
  7zz x $rice/ObservedRice.7z -o$rice -p"$RICE_PASSWORD"
  unset RICE_PASSWORD

  thinzar_ryegrass=./ApsimX/Prototypes/ThinzarExperiment
  7zz x $thinzar_ryegrass/ThinzarObserved.7z -o$thinzar_ryegrass -p"$THINZAR_RYEGRASS_PASSWORD"
  unset THINZAR_RYEGRASS_PASSWORD

  uom_winter_spring=./ApsimX/Tests/Validation/Wheat/UoM_WinterVsSpring
  7zz x $uom_winter_spring/Observed.zip -o$uom_winter_spring -p"$UOM_WINTER_SPRING_PASSWORD"
  unset UOM_WINTER_SPRING_PASSWORD

}

function run_validation {
  sudo --preserve-env docker run --rm -v $PWD/ApsimX:/wd -w=/wd --entrypoint dotnet $apsimng_image bin/Release/net10.0/Models.dll "$Path" --verbose
}

# Notify the A.V.A server that the job is complete.
# A job here is a single APSIM file which will be a single experiment.
function notify_job_complete {
  # Runs APSIM.Workflow from the apsimplusr container to post the result to A.V.A.
  # A failed notification must not change the job's exit code.
  local job_time
  job_time=$(printf '%.1f' "$elapsed")
  local log_file=/wd/$stdout_file
  sudo docker run --rm -v $PWD/ApsimX:/wd -w=/wd --entrypoint dotnet $apsimng_image bin/Release/net10.0/APSIM.Workflow.dll \
  job-complete \
  --pr-number "$PR_NUMBER" \
  --full-commit-hash "$FULL_COMMIT_HASH" \
  --job-path "$Path" \
  --job-time "$job_time" \
  --log-file "$log_file" || true
}

function run_finally {
  # Function always called, regardless of any errors.
  sudo --preserve-env docker run --rm \
    -v $PWD:/wd \
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
exit_code=0

if [[ "$1" == initialise ]]; then
  initialise &>> "$stdout_file"
  exit_code=$?
fi

if [[ "$1" == run_validation ]]; then
  start_time=$(date +%s.%N)

  run_setup &>> "$stdout_file"
  exit_code=$?

  if [[ $exit_code -eq 0 ]]; then
    echo -n > "$stdout_file"
    run_validation &>> "$stdout_file"
    exit_code=$?
  fi

  end_time=$(date +%s.%N)
  elapsed=$(echo "$end_time - $start_time" | bc -l)
  echo "Elapsed time: $elapsed seconds" &>> "$stdout_file"

  run_finally &>> "$stdout_file"
  finally_exit_code=$?

  if [[ $exit_code -eq 0 && $finally_exit_code -ne 0 ]]; then
    exit_code=$finally_exit_code
  fi
fi

exit $exit_code