FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build-env
WORKDIR /app
COPY src src
WORKDIR /app/src/Perper.Fabric
RUN dotnet publish -c Release -o /app/out

FROM openjdk:8-slim-buster
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl unzip \ 
    && rm -rf /var/lib/apt/lists/*

# Install .NET Core
RUN dotnet_version=3.1.0 \
    && curl -SL --output dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Runtime/$dotnet_version/dotnet-runtime-$dotnet_version-linux-x64.tar.gz \
    && dotnet_sha512='99949807c00871d66e8ce7c25c14998e78a0ea60ba8cc42244643ed2e13aa360285df1c8d27729df3efb319f4af9163ea5626c1478a9dd4bed9d2a58e01d6343' \
    && echo "$dotnet_sha512 dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -ozxf dotnet.tar.gz -C /usr/share/dotnet \
    && rm dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

# Install Apache Ignite  
RUN ignite_version=2.8.1 \
    && curl -SL --output ignite.zip https://archive.apache.org/dist/ignite/$ignite_version/apache-ignite-$ignite_version-bin.zip \
    && ignite_sha512='668377f8160633501506d7480784357a3831a8666335dd6c76d041198e76eb602a9c962071b4f92453d3a5662b4da6af8a498740a7a97f8d79ca9e465fcc8905' \
    && echo "$ignite_sha512 ignite.zip" | sha512sum -c - \
    && mkdir -p /usr/share \
    && unzip ignite.zip -d /usr/share \
    && mv /usr/share/apache-ignite-$ignite_version-bin /usr/share/apache-ignite \
    && rm ignite.zip

WORKDIR /app
COPY --from=build-env /app/out .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT 1
ENTRYPOINT ["dotnet", "Perper.Fabric.dll"]