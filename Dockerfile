FROM mijitt0m/ocelot-build:0.0.1

WORKDIR /src

COPY ./. .

RUN chmod u+x build.sh

RUN make build