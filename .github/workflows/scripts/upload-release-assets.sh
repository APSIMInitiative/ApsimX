#!/usr/bin/env bash
#
# This script deploys an ApsimX release, causing this version to start
# appearing in the upgrade lists.
#
# This script requires that the following variables are set:
# - DOCKER_METADATA_OUTPUT_VERSION         variable containing PR number with "pr-" prefix
# - NETLIFY_BUILD_HOOK      Token to trigger a netlify build
# - BUILDS_JWT              JWT token to authorise calls to the APSIM Builds API
# this script.

set -euo pipefail

test -z ${DOCKER_METADATA_OUTPUT_VERSION:+x} && ( echo "DOCKER_METADATA_OUTPUT_VERSION not set"; exit 1 )
test -z ${BUILDS_JWT:+x} && ( echo "BUILDS_JWT not set"; exit 1 )

PULL_ID=${DOCKER_METADATA_OUTPUT_VERSION:3}
test -z ${PULL_ID:+x} && ( echo "PULL_ID not set"; exit 1 )

echo Adding build to DB...
response_file=$(mktemp)
http_code=$(curl -sS -o "$response_file" -w '%{http_code}' -X POST -H "Authorization: bearer $BUILDS_JWT" "https://builds.apsim.info/api/nextgen/add?pullRequestNumber=$PULL_ID") || http_code=000
if [ "$http_code" -lt 200 ] || [ "$http_code" -ge 300 ]; then
    echo "Failed to add build to DB. HTTP status: $http_code"
    cat "$response_file"
    echo
    exit 1
fi
rm -f "$response_file"

echo new build uploaded
echo finished
