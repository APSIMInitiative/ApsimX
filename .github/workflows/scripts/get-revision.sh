#!/bin/sh

url='https://builds.apsim.info/api/nextgen/nextversion'
VERSION=$(curl -ks "$url" | sed -e 's/<[^>]*>//g' | cut -d'<' -f1)
echo $VERSION