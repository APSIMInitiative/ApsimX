# Be aware that this script runs in the context of a GitHub Action and includes github workflow variables.
# See https://docs.github.com/en/actions/learn-github-actions/variables for more information
repository=$1
sha=$2
github_token=$3

# Test each input arg individually to make sure none are missing
if [ -z "$repository" ]; then
    echo "Repository is missing as an input argument."
    exit 1
fi

if [ -z "$sha" ]; then
    echo "sha is missing as an input argument."
    exit 1
fi

if [ -z "$github_token" ]; then
    echo "github_token is missing as an input argument."
    exit 1
fi

echo "URL: https://api.github.com/repos/${repository}/commits/${sha}/pulls"
response=$(curl -L \
            -H "Accept: application/vnd.github+json" \
            -H "Authorization: Bearer ${github_token}" \
            -H "X-GitHub-Api-Version: 2022-11-28" \
            https://api.github.com/repos/${repository}/commits/${sha}/pulls)
echo "pr_number=$(echo $response | jq -r '.[0].number')" >> "$GITHUB_OUTPUT"