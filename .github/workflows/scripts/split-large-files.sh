#!/bin/bash

#using this script for now to upload to azure instead of docker image

echo "Pre-start azure pool for later"
dotnet ./bin/Release/net10.0/APSIM.Workflow.dll azure-jobs --pre-start-pool --pr-number "$pr_number" --commit-hash "$INCOMING_COMMIT_SHA" --env-string "$AZURE_ENV_CONTENTS"

#compress files needed for PR to 7z zrchive
echo "Package files for sending to azure"

cd /home/runner/work/ApsimX/

examples=ApsimX/Examples/
prototypes=ApsimX/Prototypes/
tests=ApsimX/Tests/
app=ApsimX/bin/Release/net10.0/

tar --zstd -cf upload.tar.zst $examples $prototypes $tests $app