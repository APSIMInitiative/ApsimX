#!/bin/bash

#using this script for now to upload to azure instead of docker image

set -euo pipefail

test -z "${PULL_REQUEST:-}" && echo "PULL_REQUEST is empty" && exit 1
test -z "${INCOMING_COMMIT_SHA:-}" && echo "INCOMING_COMMIT_SHA is empty" && exit 1
test -z "${AZURE_ENV_CONTENTS:-}" && echo "AZURE_ENV_CONTENTS is empty" && exit 1

echo "Pre-start azure pool for later"
dotnet ./bin/Release/net10.0/APSIM.Workflow.dll azure-jobs --pre-start-pool --pr-number "$PULL_REQUEST" --commit-hash "$INCOMING_COMMIT_SHA" --env-string "$AZURE_ENV_CONTENTS"

#fetch 7z
echo "fetch 7z"
cd /home/runner/work/ApsimX/
mkdir sevenzip
cd sevenzip

sevenzip_filename=7z2601-linux-x64.tar.xz
curl -LO https://github.com/ip7z/7zip/releases/download/26.01/$sevenzip_filename
tar -xf $sevenzip_filename
rm $sevenzip_filename

ls -lh

#compress files needed for PR to 7z zrchive
echo "Package files for sending to azure"
cd /home/runner/work/ApsimX/

sevenzip=sevenzip/
examples=ApsimX/Examples/
prototypes=ApsimX/Prototypes/
tests=ApsimX/Tests/
bin=ApsimX/bin/Release/net10.0/

tar --zstd -cf upload.tar.zst $sevenzip $examples $prototypes $tests $bin

ls -lh