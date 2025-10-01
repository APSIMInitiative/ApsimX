
repository=$1
sha=$2
token=$3
test -z "$repository" && echo "REPOSITORY is not set" && exit 1
test -z "$sha" && echo "SHA is not set" && exit 1
test -z "$token" && echo "GITHUB_TOKEN is not set" && exit 1

echo "URL: https://api.github.com/repos/${repository}/commits/${sha}/pulls"
response=$(curl -L -s -H "Accept: application/vnd.github+json" \
            -H "Authorization: Bearer ${token}" \
            -H "X-GitHub-Api-Version: 2022-11-28" \
            https://api.github.com/repos/${repository}/commits/${sha}/pulls)
echo "pr_number=$(echo $response | jq -r '.[0].number')" >> "$GITHUB_OUTPUT"