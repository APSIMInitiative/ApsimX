#!/usr/bin/env bash
#
# This script deploys an ApsimX release, causing this version to start
# appearing in the upgrade lists.
#
# This script requires that the following variables are set:
# - PULL_ID         ID of the pull request which triggered this release
# - NETLIFY_BUILD_HOOK      Token to trigger a netlify build
# this script.

set -euo pipefail

test -z ${DOCKER_METADATA_OUTPUT_VERSION:+x} && ( echo "DOCKER_METADATA_OUTPUT_VERSION not set"; exit 1 )
test -z ${NETLIFY_BUILD_HOOK:+x} && ( echo "NETLIFY_BUILD_HOOK not set"; exit 1 )
test -z ${BUILDS_JWT:+x} && ( echo "BUILDS_JWT not set"; exit 1 )

PULL_ID=${DOCKER_METADATA_OUTPUT_VERSION:3}
test -z ${PULL_ID:+x} && ( echo "PULL_ID not set"; exit 1 )

echo Adding build to DB...
curl -fsX POST -H "Authorization: bearer $BUILDS_JWT" "https://builds.apsim.info/api/nextgen/add?pullRequestNumber=$PULL_ID"

echo Updating registration website cache
curl -fs "https://registration.apsim.info/api/updateproducts"

echo Updating netlify build...
curl -fsX POST -d {} https://api.netlify.com/build_hooks/$NETLIFY_BUILD_HOOK