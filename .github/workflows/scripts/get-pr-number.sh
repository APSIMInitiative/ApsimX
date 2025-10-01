# Be aware that this script runs in the context of a GitHub Action and includes github workflow variables.
# See https://docs.github.com/en/actions/learn-github-actions/variables for more information

echo "URL: https://api.github.com/repos/${{ github.repository }}/commits/${{ github.sha }}/pulls"
response=$(curl -L \
            -H "Accept: application/vnd.github+json" \
            -H "Authorization: Bearer ${{ secrets.GITHUB_TOKEN }}" \
            -H "X-GitHub-Api-Version: 2022-11-28" \
            https://api.github.com/repos/${{ github.repository }}/commits/${{ github.sha }}/pulls)
echo "pr_number=$(echo $response | jq -r '.[0].number')" >> "$GITHUB_OUTPUT"