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
curl -fsX POST -H "Authorization: bearer $BUILDS_JWT" "https://builds.apsim.info/api/nextgen/add?pullRequestNumber=$PULL_ID"

echo new build uploaded
echo finished
