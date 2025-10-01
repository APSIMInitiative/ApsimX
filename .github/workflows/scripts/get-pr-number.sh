
example=$1

repository=$2
sha=$3
github_token=$4

# test if example arg is missing
if [ -z "$example" ]; then
    echo "Example argument is missing."
    exit 1
fi

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