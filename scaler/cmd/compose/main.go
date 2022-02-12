/*
Copyright 2022.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

package main

import (
	"context"
	"fmt"
	"os"
	"time"

	"github.com/docker/cli/cli/config"
	dockerclient "github.com/docker/docker/client"
	// 	"github.com/spf13/cobra"

	speccli "github.com/compose-spec/compose-go/cli"
	"github.com/docker/compose/v2/pkg/api"
	"github.com/docker/compose/v2/pkg/compose"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"

	"github.com/obecto/perper/scaler/pkg"
	scalercompose "github.com/obecto/perper/scaler/pkg/compose"
	"github.com/obecto/perper/scaler/pkg/fabric"
)

func run(ctx context.Context) error {
	addr := "localhost:40400" // TODO: accept flags

	workingDirectory := "../samples/python/container_sample/" // TODO: accept flags
	configPaths := []string{}                                 // TODO: accept flags
	options, err := speccli.NewProjectOptions(configPaths, speccli.WithWorkingDirectory(workingDirectory), speccli.WithDefaultConfigPath)
	if err != nil {
		return err
	}
	project, err := speccli.ProjectFromOptions(options)
	if err != nil {
		return err
	}

	timeoutDelay, err := time.ParseDuration("1s") // TODO: accept flags
	if err != nil {
		return err
	}

	fabricOptions := fabric.DefaultFabricOptions() // TODO: accept flags

	composeScalerOptions := scalercompose.ComposeScalerOptions{
		Up: api.UpOptions{
			Create: api.CreateOptions{
				RemoveOrphans:        true,
				Recreate:             api.RecreateDiverged,
				RecreateDependencies: api.RecreateNever, // TODO: accept flags
				Timeout:              &timeoutDelay,
			},
			Start: api.StartOptions{
				CascadeStop: false,
				Wait:        false,
			},
		},
	}

	os.Chdir(project.WorkingDir)

	connection, err := grpc.Dial(addr, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		return err
	}
	defer connection.Close()

	fabricService := fabric.NewFabricService(connection, fabricOptions)

	dockerClient, err := dockerclient.NewClientWithOpts(dockerclient.FromEnv, dockerclient.WithAPIVersionNegotiation())
	if err != nil {
		return err
	}

	composeService := compose.NewComposeService(dockerClient, config.LoadDefaultConfigFile(os.Stderr))

	composeScalerService := scalercompose.NewComposeScalerService(composeService, fabricService, composeScalerOptions)

	/*executionsChan := channels.NewRingChannel(1) //make(chan []fabricExecution)
	errChan := make(chan error)
	debounceDelay, err := time.ParseDuration("400ms") // TODO: accept flags
	if err != nil {
		return err
	}*/

	return scaler.Run(ctx, fabricService, composeScalerService)
}

func main() {
	err := run(context.Background())
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}
