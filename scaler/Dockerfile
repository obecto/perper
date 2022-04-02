FROM golang:1.17-buster AS build
RUN apt-get update && \
    apt-get install --no-install-recommends --assume-yes protobuf-compiler libprotobuf-dev

WORKDIR /app
COPY go.mod ./
COPY go.sum ./
RUN go mod download

COPY . ./

WORKDIR /app/scaler
RUN make build-kubernetes

FROM gcr.io/distroless/base-debian10
WORKDIR /
COPY --from=build /app/scaler/bin/perper-scaler-kubernetes /perper-scaler-kubernetes
ENTRYPOINT ["/perper-scaler-kubernetes"]
