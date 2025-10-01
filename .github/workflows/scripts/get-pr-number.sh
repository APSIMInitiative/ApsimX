# Be aware that this script runs in the context of a GitHub Action and includes github workflow variables.
# See https://docs.github.com/en/actions/learn-github-actions/variables for more information
repository=$1
sha=$2
github_token=$3

# Test if any input args are missing
if [ -z "$repository" ] || [ -z "$sha" ] || [ -z "$github_token" ]; then
    echo "One or more input arguments are missing."
    exit 1
fi

echo "URL: https://api.github.com/repos/${repository}/commits/${sha}/pulls"
response=$(curl -L \
            -H "Accept: application/vnd.github+json" \
            -H "Authorization: Bearer ${github_token}" \
            -H "X-GitHub-Api-Version: 2022-11-28" \
            https://api.github.com/repos/${repository}/commits/${sha}/pulls)
echo "pr_number=$(echo $response | jq -r '.[0].number')" >> "$GITHUB_OUTPUT"