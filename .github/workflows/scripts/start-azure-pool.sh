# This script is used to start an Azure pool ahead of time before jobs are created.
# This is to ensure that the pool is ready to accept jobs when they are created and reduces spin up time.

# Check all the environment variables are set.
test -z "$AZURE_ENV_CONTENTS" && echo "AZURE_ENV_CONTENTS is empty" && exit 1 || echo "AZURE_ENV_CONTENTS is set"
# The env contents need to be url encoded before they can be sent via curl.
ENCODED_STRING=$(jq -rn --arg x "$AZURE_ENV_CONTENTS" '$x|@uri')
# Once the azure env contents are url encoded we can send them via curl
url="https://digitalag.csiro.au/workflo/create-pool?poolId=test-pool2&envString=$ENCODED_STRING&nodeNumber=30&isAutoscaled=false"
response=$(curl -f -X 'POST' "$url")

