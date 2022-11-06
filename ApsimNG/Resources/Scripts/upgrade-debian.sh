#!/bin/sh
#
# This is the debian upgrade script. It will prompt the user for root 
# access, then uninstall an old version of apsim (if it exists), then
# install and launch the new apsim version.
#
# Requires 1 argument: path to the apsim installer (.deb file).

# Exit immediately if any command fails.
set -e

# Ensure that 1 argument was passed in, and that it's a file.
usage="Usage: $0 <installer-path>"
test $# -eq 1 || (echo $usage; exit 1)
test -f $1 || (echo $1 does not exist; exit 1)

# Prompt the user for root access.
prompt="APSIM Upgrade requires sudo access"
zenity --password --title $prompt --timeout 30 | sudo -v -S

# Check if apsim is already installed.
if dpkg-query -Wf'${db:Status-abbrev}' apsim 2>/dev/null | grep -q '^i'; then
  # apsim is already installed - so uninstall it.
  sudo dpkg -r apsim
fi

# Install the new version of apsim - if it fails, try again after an
# apt install -f.
sudo dpkg -i $1 || (sudo apt install -f && sudo dpkg -i $1)

# Now run the new apsim (and fork it, so this script can exit).
apsim &
