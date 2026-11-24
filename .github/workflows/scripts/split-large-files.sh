#using this script for now to upload to azure instead of docker image

pwd

#compress files needed for PR to 7z zrchive
cd /home/runner/work/ApsimX/

examples=/home/runner/work/ApsimX/ApsimX/Examples/
prototypes=/home/runner/work/ApsimX/ApsimX/Prototypes/
tests=/home/runner/work/ApsimX/ApsimX/Tests/
app=/home/runner/work/ApsimX/ApsimX/bin/Release/net10.0/

tar --zstd -cf upload.tar.zst $examples $prototypes $tests $app

ls -lh