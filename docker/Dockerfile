FROM gradle:6.4.1-jdk8 AS build-env
WORKDIR /fabric
COPY fabric /fabric
COPY proto /proto
USER root
RUN gradle assemble

FROM apacheignite/ignite:2.8.1
WORKDIR /app
COPY --from=build-env /fabric/build/distributions/*.tar perper-fabric.tar
RUN tar -xvf *.tar && rm *.tar && mv perper-fabric-*/ perper-fabric/
ENTRYPOINT ["perper-fabric/bin/perper-fabric"]
