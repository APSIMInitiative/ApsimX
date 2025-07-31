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

# Get the PATH environment variable
# echo "Current PATH: $PATH"
# echo "Contents of /app: $(ls /app)"

# Runs the Models command as normal
Models "$@"



