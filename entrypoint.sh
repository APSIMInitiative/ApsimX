#!/bin/bash

# Copy validation files to the bind mount directory
if test -d /validation_files; then
  echo "Copying validation files to /wd/"
else
  echo "Directory /validation_files does not exist."
  exit 1
fi

# Copies the validation files to the Docker bind mount
cp -a /validation_files/. /wd/

# Runs the Models command as normal
Models "$@"



