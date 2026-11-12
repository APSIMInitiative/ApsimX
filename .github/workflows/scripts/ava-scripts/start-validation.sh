#!/bin/bash

set -euo pipefail

# Require the pull request number and commit SHA used to open the validation record.
test -z "${PULL_REQUEST:-}" && echo "PULL_REQUEST is empty" && exit 1
test -z "${COMMIT:-}" && echo "COMMIT is empty" && exit 1

# Use APSIM.Workflow to obtain the total simulation count and the list of validation
# paths. The count verifies the workflow is able to discover validation jobs.
workflow=./bin/Release/net10.0/APSIM.Workflow.dll
jobcount=$(dotnet "$workflow" sim-count)
echo "jobcount: ${jobcount}"
if [[ ! "$jobcount" =~ ^[0-9]+$ ]] || [[ "$jobcount" -eq 0 ]]; then
  echo "::error::The simulation count is empty or invalid"
  exit 1
fi

# Encode each discovered validation path as a JSON array, and reject an empty list
# rather than asking the API to start a validation with no work.
paths=$(dotnet "$workflow" locations | jq -Rsc 'split("\n") | map(select(length > 0))')
if [[ "$paths" == "[]" ]]; then
  echo "::error::No validation paths found"
  exit 1
fi

# Build the request body with jq so pull request metadata and paths are safely
# escaped and retain the JSON types expected by the API.
payload=$(jq -n \
  --arg pullRequest "$PULL_REQUEST" \
  --arg commit "$COMMIT" \
  --argjson paths "$paths" \
  '{pullRequest: $pullRequest, commit: $commit, paths: $paths}')

# Tell A.V.A to start tracking validation for the registered commit. Fail the job
# on HTTP errors or transport failures so a rejected start is visible in Actions.
curl --silent --show-error --fail-with-body --max-time 30 \
  --request POST \
  --header "Content-Type: application/json" \
  --data "$payload" \
  https://ava.apsim.info/api/pr/start
