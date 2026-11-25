#!/bin/bash

#using this script for now to upload to azure instead of docker image

#compress files needed for PR to 7z zrchive
cd /home/runner/work/ApsimX/

examples=ApsimX/Examples/
prototypes=ApsimX/Prototypes/
tests=ApsimX/Tests/
app=ApsimX/bin/Release/net10.0/

tar --zstd -cf upload.tar.zst $examples $prototypes $tests $app