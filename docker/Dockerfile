FROM gradle:6.4.1-jdk8 AS build-env
WORKDIR /fabric
COPY fabric /fabric
COPY proto /proto
RUN gradle jar

FROM apacheignite/ignite:2.8.1
WORKDIR /app
COPY --from=build-env /fabric/build/libs .
CMD java -jar *.jar