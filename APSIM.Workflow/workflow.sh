#!/bin/bash

function initialise {
  # Install docker on the Azure node
  if ! pgrep -x "dockerd" > /dev/null;then
    sudo snap install docker
    sleep 10
  fi
  # Pull down the apsim next gen and apsim performance stats collector docker images
  sudo docker pull "apsiminitiative/apsimplusr:pr-$PR_NUMBER"
  sudo docker pull apsiminitiative/postats2-collector:latest
}

function run_00001 {
  echo ------------------------------ >> metadata.txt
  echo Date/time: `date +"%Y-%m-%d %T"` >> metadata.txt
  sudo --preserve-env docker run --rm -v $PWD:/wd -w=/wd -e APSIM_NO_DOCKER "apsiminitiative/apsimplusr:pr-$PR_NUMBER" "$Path" --verbose
}

function run_00001_finally {
  # Function always called, regardless of any errors.
  sudo --preserve-env docker run --rm -v $PWD:/wd -w=/wd -e POSTATS_UPLOAD_URL  apsiminitiative/postats2-collector:latest upload "$PR_NUMBER" "$FULL_COMMIT_HASH" "$AUTHOR" "$TIME" "$PR_NUMBER-$COMMIT_SHA" "$Path"
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
if [[ $(type -t $1_finally) == function ]]; then
  $1_finally &>> local.stdout.txt
  finally_exit_code=$?
  if [[ $exit_code -eq 0 && $finally_exit_code -ne 0 ]]; then
    exit_code=$finally_exit_code
  fi
fi
end_time=$(date +%s.%N)
elapsed=$(echo "$end_time - $start_time" | bc -l)
echo "Elapsed time: $elapsed seconds" &>> local.stdout.txt
if [ $exit_code -gt 0 ]; then
  exit $exit_code
fi