#!/bin/bash

# Ensure the pull request identity is complete before contacting the validation API.
test -z "$PULL_REQUEST" && echo "PULL_REQUEST is empty" && exit 1
test -z "$COMMIT" && echo "COMMIT is empty" && exit 1
test -z "$AUTHOR" && echo "AUTHOR is empty" && exit 1

# Register this pull request commit and capture both the response and HTTP status
# so API failures can be reported with useful details.
response=$(curl --silent --show-error --max-time 30 --get \
  --data-urlencode "pullRequest=${PULL_REQUEST}" \
  --data-urlencode "commit=${COMMIT}" \
  --data-urlencode "author=${AUTHOR}" \
  --write-out '\n%{http_code}' \
  https://ava.apsim.info/api/pr/open)
curl_exit=$?
http_status="${response##*$'\n'}"
response_body="${response%$'\n'*}"

# Send status details to the GitHub Actions summary when one is available.
summary="${GITHUB_STEP_SUMMARY:-/dev/null}"

# Keep summary formatting in one helper so each API outcome reports the same fields.
write_summary() {
  {
    echo "## $1"
    echo ""
    echo "| Field | Value |"
    echo "|-------|-------|"
    echo "| Pull request | #${PULL_REQUEST} |"
    echo "| Commit | \`${COMMIT}\` |"
    echo "| Author | ${AUTHOR} |"
    echo "| HTTP status | ${http_status} |"
    echo "| Detail | $2 |"
  } >> "$summary"
}

# Treat a transport error or unexpected API response as a failed job. A duplicate
# record is expected on workflow reruns and is therefore reported as informational.
if [[ $curl_exit -ne 0 ]]; then
  echo "::error::Request to validation API failed (curl exit ${curl_exit})"
  http_status="none"
  write_summary "Validation record failed :x:" "Request failed (curl exit ${curl_exit})"
  exit 1
elif [[ "$http_status" == "200" ]]; then
  echo "Validation record opened."
  write_summary "Validation record opened :white_check_mark:" "New record created, ready to receive validation data"
elif [[ "$http_status" == "400" && "$response_body" == *"already exists"* ]]; then
  # A rerun for the same PR and commit is not a failure.
  echo "Validation record already exists for this pull request and commit."
  write_summary "Validation record already exists :information_source:" "Record already registered for this commit"
else
  echo "::error::Validation API returned HTTP ${http_status}: ${response_body}"
  write_summary "Validation record failed :x:" "${response_body}"
  exit 1
fi
