FROM gradle:7.2-jdk11 AS build-env
# Build
WORKDIR /fabric
USER root
COPY fabric/*gradle* /fabric/
RUN gradle tasks --no-daemon
COPY proto /proto
COPY fabric /fabric
RUN gradle assemble --no-daemon

FROM openjdk:11-buster
# Install Docker CLI:
RUN apt-get update && apt-get -qq install apt-transport-https ca-certificates curl gnupg lsb-release
RUN curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
RUN echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
RUN apt-get update && apt-get -qq install docker-ce docker-ce-cli containerd.io
RUN curl -L "https://github.com/docker/compose/releases/download/1.29.2/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose && chmod +x /usr/local/bin/docker-compose

# Unpack app itself
WORKDIR /app
COPY --from=build-env /fabric/build/distributions/*.tar perper-fabric.tar
RUN tar -xvf *.tar && rm *.tar && mv perper-fabric-*/ perper-fabric/
COPY fabric/config/*.xml perper-fabric/config/
ENTRYPOINT ["perper-fabric/bin/perper-fabric"]
