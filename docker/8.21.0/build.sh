# This script builds the Ocelot Docker file

# {DotNetSdkVer}.{OcelotVer} -> {.NET8}.{21.0} -> 8.21.0
version=8.21.0
docker build --platform linux/amd64 -t ocelot2/circleci-build -f Dockerfile.base .

echo $DOCKER_PASS | docker login -u $DOCKER_USER --password-stdin

docker tag ocelot2/circleci-build ocelot2/circleci-build:$version
docker push ocelot2/circleci-build:latest
docker push ocelot2/circleci-build:$version
