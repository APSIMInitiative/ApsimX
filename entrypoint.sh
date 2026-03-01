#!/bin/bash

# This bash script is used in the apsim docker image used for running validation tests.

# Checks if the /validation_files directory exists
if [ ! -d /validation_files ]; then
  echo "Directory /validation_files does not exist. Unable to copy validation files to required location."
  exit 1
fi

# Copies the validation files to the Docker bind mount
cp -a /validation_files/. /wd/

# Runs the Models command as normal
Models "$@"



