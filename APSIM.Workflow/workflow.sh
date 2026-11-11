#!/bin/bash

function initialise {
  # Install docker on the Azure node
  if ! pgrep -x "dockerd" > /dev/null;then
    sudo snap install docker
    sleep 10
  fi
  # Installs jq if missing for A.V.A job complete call.
  if ! command -v jq > /dev/null;then
    sudo apt-get install -y jq
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

# If a finally function exists run it.
if [[ $(type -t $1_finally) == function ]]; then
  $1_finally &>> local.stdout.txt
  finally_exit_code=$?
  if [[ $exit_code -eq 0 && $finally_exit_code -ne 0 ]]; then
    exit_code=$finally_exit_code
  fi
fi

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

# Notify the A.V.A server that the job is complete.
# A job here is a single APSIM file which will be a single experiment.
function notify_job_complete {
  local rel_path="${Path#/wd/}"
  local payload
  payload=$(jq -n \
    --arg pr "$PR_NUMBER" \
    --arg commit "$FULL_COMMIT_HASH" \
    --arg path "$rel_path" \
    --arg time "$(printf '%.1f' "$elapsed")" \
    --slurpfile data <(jq -R . local.stdout.txt | jq -s .) \
    '{pullRequest: $pr, commit: $commit, path: $path, time: $time, data: $data[0]}')

  curl -sS -X POST "https://ava.apsim.info/api/pr/jobcomplete" \
    -H "Content-Type: application/json" \
    -d "$payload" >> local.stdout.txt 2>&1 || true
}

notify_job_complete
if [ $exit_code -gt 0 ]; then
  exit $exit_code
fi