
payload_folder_path=$1
azure_env_contents=$2
incoming_commit_sha=$3
pr_author=$4
docker_metadata_output_version=$5

# Test if any input args are missing
if [ -z "$payload_folder_path" ] || [ -z "$azure_env_contents" ] || [ -z "$incoming_commit_sha" ] || [ -z "$pr_author" ] || [ -z "$docker_metadata_output_version" ]; then
    echo "One or more input arguments are missing."
    exit 1
fi

# Add .env to payload directory
echo "Adding .env file to ${payload_folder_path}"
echo "${azure_env_contents}" > "${payload_folder_path}/.env"
if test -f "${payload_folder_path}/.env"; then
    echo ".env successfully added to ${payload_folder_path}"
fi

# Tell POStats2 to open and get ready to accept data
echo "Opening POStats2 to accept data..."
commitsha=${incoming_commit_sha}
author=${pr_author}
if [[ "$author" == *"[bot]"* ]]; then
    author="${author%"[bot]"}"
fi

# This is used to make a PR specific pool to avoid multiple PRs issues on Azure.
azure_pool="${docker_metadata_output_version:3}-${commitsha:0:6}"
echo "azure pool: ${azure_pool}"
echo "author variable: ${author}"
# substrings the variable so we get the number only after the 'pr-' part.
echo "PR Number: ${docker_metadata_output_version:3}"
# makes the variable available in subsequent steps.
jobcount=$(dotnet ./bin/Release/net8.0/APSIM.Workflow.dll --sim-count)
echo "jobcount: ${jobcount}"
if test -z "$jobcount"; then
    echo "The job count was found to be empty. Exiting."
    exit 1
fi
response=$(curl "https://postats2.apsim.info/api/open?pullrequestnumber=${docker_metadata_output_version:3}&commitid=${commitsha}&count=${jobcount}&author=${author}&pool=${azure_pool}")
echo "POStats2 open response: ${response}"
echo "Start creating payload..."
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll --payload-directory "${payload_folder_path}" -g "$author" -t $docker_metadata_output_version --commit-sha "$commitsha" --pr-number ${docker_metadata_output_version:3} --azure-pool ${azure_pool} -v