
test -z "$REPOSITORY" && echo "REPOSITORY is not set" && exit 1 || echo "REPOSITORY is set"
test -z "$SHA" && echo "SHA is not set" && exit 1 || echo "SHA is set"
test -z "$GITHUB_TOKEN" && echo "GITHUB_TOKEN is not set" && exit 1 || echo "GITHUB_TOKEN is set"

echo "URL: https://api.github.com/repos/${REPOSITORY}/commits/${SHA}/pulls"
response=$(curl -L -s -H "Accept: application/vnd.github+json" \
            -H "Authorization: Bearer ${TOKEN}" \
            -H "X-GitHub-Api-Version: 2022-11-28" \
            https://api.github.com/repos/${REPOSITORY}/commits/${SHA}/pulls)
echo "pr_number=$(echo $response | jq -r '.[0].number')" >> "$GITHUB_OUTPUT"