#!/usr/bin/env bash
#
# This script originally ran inside docker on Jenkins build agents. 
# It generates an installer for the platform specified by the first argument, and
# uploads the installer to builds.apsim.info.
#
# Expects 1 argument (platform name - either macos or debian).

set -e

# Ensure that target platform name has been passed as an argument.
usage="Usage: $0 <debian|macos|windows>"
test $# -eq 1 || (echo $usage; exit 1)
test -z "$BUILDS_JWT" && echo "BUILDS_JWT is empty" && exit 1

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
echo "DIR: $DIR"

# Check the platform name - exit if invalid, otherwise get a valid
# runtime identifier to pass to MSBuild, as well as the name of the
# script which will build the installer for this platform.
#
# build_platform is the platform name expected by the APSIM.Builds API. It
# must be one of:
# - Linux
# - MacOS
# - Windows
platform=$1
if [[ $platform = debian ]]; then
    script=debian/buildDebianInstaller
    ext=deb
    build_platform=Linux
elif [[ $platform = macos ]]; then
    script=macos/buildMacInstaller
    ext=dmg
    build_platform=MacOS
elif [[ $platform = windows ]]; then
    script=windows/buildWindowsInstaller
    ext=exe
    build_platform=Windows
else
    echo $usage
    exit 1
fi

# Ensure that the PULL_ID environment variable is set.
test -z ${PULL_ID:+x} && ( echo "PULL_ID not set"; exit 1 )

# Get the version number for the new release.
echo Retrieving version info...
version=$(./.github/workflows/scripts/get-revision.sh)
revision=$version
year=$(TZ=Australia/Brisbane date +%-Y)
month=$(TZ=Australia/Brisbane date +%-m)
full_version=$year.$month.$version.0
ISSUE_NUMBER=$(echo $version | cut -d. -f 4)

# Build the installer.
echo Building installer...
outfile="$DIR"/apsim-$version.$ext
bash ./Setup/net8.0/$script $full_version "$outfile"

# Finally, upload the installer.
echo Uploading installer...
url="https://builds.apsim.info/api/nextgen/upload/installer?revision=$revision&platform=$build_platform"

function upload() {
    curl --fail -X POST -H "Authorization: bearer $BUILDS_JWT" -F "file=@$outfile" "$url"
    return $?
}

retry=0
maxRetries=3
interval=60 # in seconds
until [ ${retry} -ge ${maxRetries} ]
do
	upload && break
	retry=$((retry+1))
	echo "Retrying [${retry}/${maxRetries}] in ${interval}(s) "
	sleep ${interval}
    interval=$((interval * 2))
done
if [ ${retry} -ge ${maxRetries} ]; then
  echo "Failed to upload installer after ${maxRetries} attempts!"
  exit 1
fi