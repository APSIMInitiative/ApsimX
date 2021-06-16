#!/bin/sh
#
# This is the macOS upgrade script. It will install a new version of
# apsim (pointed to by the first argument), then uninstall an old
# version of apsim (pointed to by the second argument), then launch the
# new version of apsim.
#
# Requires 2 arguments:
#   1. Path to the apsim installer (a .dmg file).
#   2. Path to the old apsim installation, which is to be removed.

# Exit immediately if any command fails.
set -e

# Ensure that two arguments are provided.
usage="Usage: $0 <installer-path> <old-install-path>"
test $# -eq 2 || (echo $usage; exit 1)
test -f $1 || (echo "Installer $1 does not exist"; exit 1)
test -d $2 || (echo "Old installation path $2 does not exist"; exit 1)

# todo: should we wait for apsim to exit?
# lsof -p $pid +r 1 &>/dev/null

# Mount the .dmg file.
APSIMDMG=$(hdiutil attach $1)

# Parse output of hdiutil to determine the app path.
DMGDevice=$(echo $APSIMDMG | cut -f1 -d' ')
DMGPath=$(echo $APSIMDMG | cut -f2 -d' ')
appPath=$(echo $DMGPath)/`ls $DMGPath`

# Uninstall the old apsim install by deleting the directory.
rm -rf $2

# Install the new apsim into /Applications.
cp -R $appPath /Applications

# Unmount the .dmg file.
hdiutil detach -quiet $DMGDevice
