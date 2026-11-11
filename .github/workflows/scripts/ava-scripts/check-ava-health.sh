#!/bin/bash

# Check whether the APSIM validation API (A.V.A) health endpoint is reachable.
# Keep the response body and HTTP status so either can be included in the job summary.
response=$(curl --silent --show-error --max-time 30 --write-out '\n%{http_code}' https://ava.apsim.info/api/health)
curl_exit=$?
http_status="${response##*$'\n'}"
response_body="${response%$'\n'*}"

# Write health-check details to the GitHub Actions summary when available.
summary="${GITHUB_STEP_SUMMARY:-/dev/null}"

# Report a healthy response; otherwise report the failure and stop dependent jobs.
if [[ $curl_exit -eq 0 && "$http_status" == "200" ]]; then
  echo "A.V.A is up: ${response_body}"
  {
    echo "## A.V.A is up :white_check_mark:"
    echo ""
    echo "| Field | Value |"
    echo "|-------|-------|"
    echo "| Endpoint | https://ava.apsim.info/api/health |"
    echo "| HTTP status | ${http_status} |"
    echo "| Response | \`${response_body}\` |"
  } >> "$summary"
else
  echo "::error::A.V.A is down (curl exit ${curl_exit}, HTTP ${http_status}): ${response_body}"
  {
    echo "## A.V.A is down :x:"
    echo ""
    echo "| Field | Value |"
    echo "|-------|-------|"
    echo "| Endpoint | https://ava.apsim.info/api/health |"
    echo "| curl exit code | ${curl_exit} |"
    echo "| HTTP status | ${http_status} |"
    echo "| Response | \`${response_body}\` |"
    echo ""
    echo "Validation record was not opened because A.V.A is unavailable."
  } >> "$summary"
  exit 1
fi
