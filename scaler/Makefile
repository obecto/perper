.PHONY: all
all: fmt vet build

PROJECT_DIR := $(shell dirname $(abspath $(lastword $(MAKEFILE_LIST))))

BIN_DIR := $(PROJECT_DIR)/bin
ifeq (,$(shell go env GOBIN))
GOBIN=$(shell go env GOPATH)/bin
else
GOBIN=$(shell go env GOBIN)
endif

export PATH := $(GOBIN):$(PATH)

##@ General

# The help target prints out all targets with their descriptions organized
# beneath their categories. The categories are represented by '##@' and the
# target descriptions by '##'. The awk commands is responsible for reading the
# entire set of makefiles included in this invocation, looking for lines of the
# file as xyz: ## something, and then pretty-format the target and help. Then,
# if there's a line with ##@ something, that gets pretty-printed as a category.
# More info on the usage of ANSI control characters for terminal formatting:
# https://en.wikipedia.org/wiki/ANSI_escape_code#SGR_parameters
# More info on the awk command:
# http://linuxcommand.org/lc3_adv_awk.php

.PHONY: help
help: ## Display this help.
	@awk 'BEGIN {FS = ":.*##"; printf "\nUsage:\n  make \033[36m<target>\033[0m\n"} /^[a-zA-Z_0-9-]+:.*?##/ { printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } ' $(MAKEFILE_LIST)

##@ Development

.PHONY: fmt
fmt: ## Run go fmt against code.
	go fmt ./...

.PHONY: vet
vet: ## Run go vet against code.
	go vet ./...

.PHONY: build
build: build-compose ## Build everything.

.PHONY: clean
clean: clean-proto clean-compose ## Clean everything.

##@ Compose

.PHONY: build-compose
build-compose: generate-proto ## Build compose scaler binary.
	go build -o bin/perper-scaler-compose cmd/compose/main.go

.PHONY: run-compose
run-compose: generate-proto  ## Run compose scaler.
	go run cmd/compose/main.go

.PHONY: clean-compose
clean-compose: ## Clean compose scaler binary.
	rm bin/perper-scaler-compose

##@ Dependencies

.PHONY: generate-proto
generate-proto: pb/fabric.pb.go pb/fabric_grpc.pb.go  ## Generate proto files.

.PHONY: clean-proto
clean-proto:  ## Clean proto files.
	rm pb/fabric.pb.go pb/fabric_grpc.pb.go

pb/fabric.pb.go pb/fabric_grpc.pb.go &: ../proto/fabric.proto | protoc-gen-go protoc-gen-go-grpc
	protoc --go_out=./pb --go_opt=paths=source_relative --go-grpc_out=./pb --go-grpc_opt=paths=source_relative --proto_path=../proto/ fabric.proto


.PHONY: protoc-gen-go
protoc-gen-go: ## Download protoc-gen-go locally if necessary.
ifeq (,$(shell PATH=$(PATH) which protoc-gen-go 2>/dev/null))
	go install google.golang.org/protobuf/cmd/protoc-gen-go@v1.26
endif

.PHONY: protoc-gen-go-grpc
protoc-gen-go-grpc: ## Download protoc-gen-go locally if necessary.
ifeq (,$(shell PATH=$(PATH) which protoc-gen-go-grpc 2>/dev/null))
	go install google.golang.org/grpc/cmd/protoc-gen-go-grpc@v1.1
endif

