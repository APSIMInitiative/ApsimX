#using this script for now to upload to azure instead of docker image

pwd

#compress files needed for PR to 7z zrchive
examples=/home/runner/work/ApsimX/ApsimX/Examples/
prototypes=/home/runner/work/ApsimX/ApsimX/Prototypes/
tests=/home/runner/work/ApsimX/ApsimX/Tests/
app=/home/runner/work/app
7z a upload.7z $examples $prototypes $tests $app

ls -lh