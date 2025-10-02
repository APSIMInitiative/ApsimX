
# Check all the environment variables are set.
test -z "$PAYLOAD_FOLDER_PATH" && echo "PAYLOAD_FOLDER_PATH is empty" && exit 1 || echo "PAYLOAD_FOLDER_PATH is set"
test -z "$AZURE_ENV_CONTENTS" && echo "AZURE_ENV_CONTENTS is empty" && exit 1 || echo "AZURE_ENV_CONTENTS is set"
test -z "$INCOMING_COMMIT_SHA" && echo "INCOMING_COMMIT_SHA is empty" && exit 1 || echo "INCOMING_COMMIT_SHA is set"
test -z "$PR_AUTHOR" && echo "PR_AUTHOR is empty" && exit 1 || echo "PR_AUTHOR is set"
test -z "$DOCKER_METADATA_OUTPUT_VERSION" && echo "DOCKER_METADATA_OUTPUT_VERSION is empty" && exit 1 || echo "DOCKER_METADATA_OUTPUT_VERSION is set"

# Add .env to payload directory
echo "Adding .env file to $PAYLOAD_FOLDER_PATH"
echo "$AZURE_ENV_CONTENTS" > "$PAYLOAD_FOLDER_PATH/.env"
if test -f "$PAYLOAD_FOLDER_PATH/.env"; then
    echo ".env successfully added to $PAYLOAD_FOLDER_PATH"
fi

# Tell POStats2 to open and get ready to accept data
echo "Opening POStats2 to accept data..."
commitsha=$INCOMING_COMMIT_SHA
author=$PR_AUTHOR
if [[ "$author" == *"[bot]"* ]]; then
    author="${author%"[bot]"}"
fi

# This is used to make a PR specific pool to avoid multiple PRs issues on Azure.
azure_pool=${DOCKER_METADATA_OUTPUT_VERSION:3}-${commitsha:0:6}
echo "azure pool: $azure_pool"
echo "author variable: $author"
# substrings the variable so we get the number only after the 'pr-' part.
echo "PR Number: ${DOCKER_METADATA_OUTPUT_VERSION:3}"
# makes the variable available in subsequent steps.
jobcount=$(dotnet ./bin/Release/net8.0/APSIM.Workflow.dll --sim-count)
echo "jobcount: $jobcount"
if test -z "$jobcount"; then
    echo "The job count was found to be empty. Exiting."
    exit 1
fi
response=$(curl "https://postats2.apsim.info/api/open?pullrequestnumber=${DOCKER_METADATA_OUTPUT_VERSION:3}&commitid=${commitsha}&count=${jobcount}&author=${author}&pool=${azure_pool}")
echo "POStats2 open response: ${response}"
echo "Start creating payload..."
pr_number=${DOCKER_METADATA_OUTPUT_VERSION:3}
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll --payload-directory $PAYLOAD_FOLDER_PATH -g $author -t $DOCKER_METADATA_OUTPUT_VERSION --commit-sha $commitsha --pr-number $pr_number --azure-pool $azure_pool -v