# Check payload directory exists and create it if not
if test -d "${PAYLOAD_FOLDER_PATH}"; then
    echo "Directory exists."
else
    echo "Directory does not exist. Making directory..."
    mkdir -p "${PAYLOAD_FOLDER_PATH}"
fi

# Add .env to payload directory
echo "Adding .env file to ${PAYLOAD_FOLDER_PATH}"
echo "${{ secrets.AZURE_ENV_CONTENTS }}" > "${PAYLOAD_FOLDER_PATH}/.env"
if test -f "${PAYLOAD_FOLDER_PATH}/.env"; then
    echo ".env successfully added to ${PAYLOAD_FOLDER_PATH}"
fi

# Tell POStats2 to open and get ready to accept data
echo "Opening POStats2 to accept data..."
echo "Commit SHA: ${{ github.event.pull_request.head.sha }}"
commitsha=${{ github.event.pull_request.head.sha }}
author=${{ github.actor }}
if [[ "$author" == *"[bot]"* ]]; then
    author="${author%"[bot]"}"
fi

# This is used to make a PR specific pool to avoid multiple PRs issues on Azure.
azure_pool="${DOCKER_METADATA_OUTPUT_VERSION:3}-${commitsha:0:6}"
echo "azure pool: ${azure_pool}"
echo "author variable: ${author}"
# substrings the variable so we get the number only after the 'pr-' part.
echo "PR Number: ${DOCKER_METADATA_OUTPUT_VERSION:3}"
# makes the variable available in subsequent steps.
jobcount=$(dotnet ./bin/Release/net8.0/APSIM.Workflow.dll --sim-count)
echo "jobcount: ${jobcount}"
if test -z "$jobcount"; then
    echo "The job count was found to be empty. Exiting."
    exit 1
fi
response=$(curl "https://postats2.apsim.info/api/open?pullrequestnumber=${DOCKER_METADATA_OUTPUT_VERSION:3}&commitid=${commitsha}&count=${jobcount}&author=${author}&pool=${azure_pool}")
echo "POStats2 open response: ${response}"
echo "Start creating payload..."
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll --payload-directory "${PAYLOAD_FOLDER_PATH}" -g "$author" -t $DOCKER_METADATA_OUTPUT_VERSION --commit-sha "$commitsha" --pr-number ${DOCKER_METADATA_OUTPUT_VERSION:3} --azure-pool ${azure_pool} -v