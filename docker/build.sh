# This script builds the Ocelot Docker file
# echo $DOCKER_PASS | docker login -u $DOCKER_USER --password-stdin

# {DotNetSdkVer}.{OcelotVer} -> {.NET9}.{24.0} -> 9.24.0
#version=9.24.0
tag=sdk9-alpine-lin.net8-9

docker build --platform linux/amd64 -t ocelot2/circleci-build -f Dockerfile.base .
docker tag ocelot2/circleci-build ocelot2/circleci-build:$tag
docker push ocelot2/circleci-build:$tag
# docker tag ocelot2/circleci-build ocelot2/circleci-build:$version
# docker push ocelot2/circleci-build:$version
