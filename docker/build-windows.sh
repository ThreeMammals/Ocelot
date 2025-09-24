version=9.24.win
tag=sdk9-nano2022-win.net8-9

docker build --no-cache --platform windows/amd64 -t ocelot2/circleci-build -f Dockerfile.windows .

docker tag ocelot2/circleci-build ocelot2/circleci-build:$tag
docker push ocelot2/circleci-build:$tag

docker tag ocelot2/circleci-build ocelot2/circleci-build:$version
docker push ocelot2/circleci-build:$version
