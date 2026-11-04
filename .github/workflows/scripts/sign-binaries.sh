#!/usr/bin/env bash
#
# Code signs every unsigned exe/dll under a directory using DigiCert's smctl.
# Requires smctl to be installed and configured (digicert/code-signing-software-trust-action).
#
# Usage: sign-binaries.sh <directory> <keypair-alias>
set -e

usage="Usage: $0 <directory> <keypair-alias>"
test $# -eq 2 || { echo "$usage"; exit 1; }
dir=$1
alias=$2

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

count=0
while IFS= read -r file; do
    echo "Signing $file"
    smctl sign --keypair-alias "$alias" --input "$file" --simple
    count=$((count+1))
done < <(bash "$DIR/list-unsigned-binaries.sh" "$dir")
echo "Signed $count file(s)."

# Fail if anything is still unsigned.
remaining=$(bash "$DIR/list-unsigned-binaries.sh" "$dir")
if [[ -n "$remaining" ]]; then
    echo "The following files are still unsigned:"
    echo "$remaining"
    exit 1
fi
