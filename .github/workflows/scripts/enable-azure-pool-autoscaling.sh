#!/bin/bash

# This script is used to enable autoscaling on an existing Azure pool.
# This is to ensure that the pool removes nodes when they are no longer needed to reduce costs.

# Check all the environment variables are set.
test -z "$AZURE_ENV_CONTENTS" && echo "AZURE_ENV_CONTENTS is empty" && exit 1 
test -z "$DOCKER_METADATA_OUTPUT_VERSION" && echo "DOCKER_METADATA_OUTPUT_VERSION is empty" && exit 1 
test -z "$INCOMING_COMMIT_SHA" && echo "INCOMING_COMMIT_SHA is empty" && exit 1

# Clean the PR number of any whitespace
pr_number=${DOCKER_METADATA_OUTPUT_VERSION:3}
cleaned_pr_number=$(echo "$pr_number" | tr -d '[:space:]')

# Use the cleaned PR number and the first 6 characters of the commit SHA to create a unique pool id.
POOL_ID="${cleaned_pr_number}-${INCOMING_COMMIT_SHA:0:6}"
echo "Combined pool id: ${POOL_ID}"

# Make AZURE_ENV_CONTENTS replace windows and linux line endings with a single space.
AZURE_ENV_CONTENTS=$(echo -e "$AZURE_ENV_CONTENTS" | tr -d '\r' | tr '\n' ' ')

# The env contents need to be url encoded before they can be sent via curl.
ENCODED_STRING=$(jq -rn --arg x "$AZURE_ENV_CONTENTS" '$x|@uri')

# Once the azure env contents are url encoded we can send them via curl
url="https://digitalag.csiro.au/workflo/enable-autoscale?poolId=${POOL_ID}&envString=${ENCODED_STRING}"
response=$(curl -f -s -w "%{http_code}" -X POST "${url}")
echo "Response from server: $response"
