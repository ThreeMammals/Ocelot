# this script build the ocelot docker file
version=0.0.10
docker build --platform linux/amd64 -t ggnaegi/ocelot-build -f Dockerfile.base .
echo $DOCKER_PASS | docker login -u $DOCKER_USER --password-stdin
docker tag ggnaegi/ocelot-build ggnaegi/ocelot-build:$version
docker push ggnaegi/ocelot-build:latest
docker push ggnaegi/ocelot-build:$version