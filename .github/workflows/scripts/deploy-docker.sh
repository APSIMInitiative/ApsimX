#!/usr/bin/env bash
#
# This script runs on Jenkins build agents, but /not/ inside a docker
# container. It builds a docker image and pushes the image to dockerhub.
#
# Expects 1 argument, which is the name of the image to be pushed.
# This corresponds to a directory in the APSIM.Docker repository.
#
# Also requires that the following environment variables are set:
#
# - DOCKER_PASSWORD: A personal access token for dockerhub, with access
#                    to the AI's dockerhub account.
# - DOCKER_USERNAME: The dockerhub username for the AI's dockerhub account.
# - MERGE_COMMIT:    Merge commit of the PR which was merged.

set -euo pipefail

# Ensure that the necessary inputs have been provioded.
test -z ${DOCKER_PASSWORD:+x} && ( echo "DOCKER_PASSWORD not set"; exit 1 )
test -z ${DOCKER_USERNAME:+x} && ( echo "DOCKER_USERNAME not set"; exit 1 )
test -z ${MERGE_COMMIT:+x} && ( echo "MERGE_COMMIT not set"; exit 1 )

# Get directory of script.
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

# Get version number.
revision=`$DIR/get-revision.sh`
year=$(TZ=Australia/Brisbane date +%-Y)
month=$(TZ=Australia/Brisbane date +%-m)
version=$year.$month.$revision.0
echo version=$version

build_image() {
    image=$1
    # Generate an image name.
    base_image_name=apsiminitiative/$image
    full_image=$base_image_name:$revision
    image_latest=$base_image_name:latest

    # Build image, then login and push to dockerhub.
    docker build --build-arg version=$version --build-arg commit=$MERGE_COMMIT -f ./Dockerfiles/release-dockerfile -t $full_image --target $image .
    docker tag $full_image $image_latest
    docker login -u "$DOCKER_USERNAME" -p"$DOCKER_PASSWORD"
    docker push $full_image
    docker push $image_latest
}

# Build and push various images
build_image apsimng
build_image apsimng-complete
build_image apsimng-gui