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
	"github.com/compose-spec/compose-go/types"
	"github.com/docker/compose/v2/pkg/api"
	"github.com/docker/compose/v2/pkg/compose"
	"github.com/obecto/perper/scaler/common/proto"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	channels "gopkg.in/eapache/channels.v1"
)

type fabricExecution struct {
	agent    string
	instance string
}

/*
func listenFabric(ctx context.Context, client proto.FabricClient, executionsChan chan<- []fabricExecution, errChan chan<- error) {
	scaler_agent := "Registry" // TODO: accept flags
	//scaler_instance_prefix := "Registry-"
	scaler_local_to_data := false
	for true {

		ctxTimeout, cancel := context.WithTimeout(ctx, time.Second)
		defer cancel()

		allExecutions, err := client.AllExecutions(ctxTimeout, &proto.ExecutionsRequest{
			Agent:       scaler_agent,
			LocalToData: scaler_local_to_data,
		})
		if err != nil {
			errChan <- err
			return
		}

		result := make([]fabricExecution, 0, len(allExecutions.Executions))

		for _, execution := range allExecutions.Executions {
			result = append(result, fabricExecution{
				agent:    execution.Instance, // Hmmm; scaler_instance_prefix
				instance: execution.Execution,
			})
		}

		executionsChan <- result
	}
}*/

func debounce(delay time.Duration, input <-chan interface{}) <-chan interface{} {
	// based on https://gist.github.com/gigablah/80d7160f3577edc153c9
	output := make(chan interface{})

	go func() {
		var timer <-chan time.Time
		var last interface{}
		var ok bool

		for {
			select {
			case last, ok = <-input:
				if !ok {
					close(output)
					return
				}
				if timer == nil {
					timer = time.After(delay)
				}
			case <-timer:
				output <- last
				timer = nil
			}
		}
	}()

	return output
}

func listenFabric(ctx context.Context, client proto.FabricClient, executionsChan chan<- interface{}, errChan chan<- error) {
	scaler_agent := "Registry" // TODO: accept flags
	//scaler_instance_prefix := "Registry-"
	scaler_local_to_data := false

	ctxCancel, cancel := context.WithCancel(ctx)
	defer cancel()
	executions, err := client.Executions(ctxCancel, &proto.ExecutionsRequest{
		Agent:       scaler_agent,
		LocalToData: scaler_local_to_data,
	})
	if err != nil {
		errChan <- err
		return
	}

	aliveExecutions := map[fabricExecution]struct{}{}
	outputReady := false

	for true {
		execution, err := executions.Recv()
		fmt.Fprintf(os.Stdout, "%v\n", execution)

		if err != nil {
			errChan <- err
			return
		}
		if execution.StartOfStream {
			outputReady = true
		} else {
			key := fabricExecution{
				agent:    execution.Instance, // Hmmm; scaler_instance_prefix
				instance: execution.Execution,
			}

			if execution.Cancelled {
				delete(aliveExecutions, key)
			} else {
				aliveExecutions[key] = struct{}{}
			}
		}

		if outputReady {
			result := make([]fabricExecution, 0, len(aliveExecutions))

			for execution, _ := range aliveExecutions {
				result = append(result, execution)
			}

			// 			sort(result)

			executionsChan <- result
		}
	}
}

func syncCompose(ctx context.Context, project *types.Project, composeService api.Service, upOptions api.UpOptions, executionsChan <-chan interface{}, errChan chan<- error) {
	for {
		select {
		case executions_ := <-executionsChan:
			executions := executions_.([]fabricExecution)
			projectCopy := *project
			projectCopy.Services = nil // TODO: remove only services that are managed by us

			for _, execution := range executions {
				service, err := project.GetService(execution.agent)
				if err != nil {
					//errChan <- err
					fmt.Fprintf(os.Stderr, "Unable to locate agent %s: %v\n", execution.agent, err)
					continue
				}

				environmentCopy := make(types.MappingWithEquals)
				for k, v := range service.Environment {
					environmentCopy[k] = v
				}
				executionCopy := execution
				environmentCopy["X_PERPER_INSTANCE"] = &executionCopy.instance
				environmentCopy["X_PERPER_AGENT"] = &executionCopy.agent
				service.Environment = environmentCopy
				service.Name = fmt.Sprintf("%s-%s", service.Name, execution.instance)
				projectCopy.Services = append(projectCopy.Services, service)
			}

			json, err := composeService.Convert(ctx, &projectCopy, api.ConvertOptions{
				Format: "json",
			})
			if err != nil {
				errChan <- err
				return
			}
			fmt.Fprint(os.Stdout, string(json))

			err = composeService.Up(ctx, &projectCopy, upOptions)
			if err != nil {
				errChan <- err
				return
			}
		case <-ctx.Done():
			return
		}
	}
}

func run() error {
	addr := "localhost:40400" // TODO: accept flags

	connection, err := grpc.Dial(addr, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		return err
	}
	defer connection.Close()
	fabricClient := proto.NewFabricClient(connection)

	ctx := context.Background()
	dockerClient, err := dockerclient.NewClientWithOpts(dockerclient.FromEnv, dockerclient.WithAPIVersionNegotiation())
	if err != nil {
		return err
	}

	// lazyInit := api.NewServiceProxy(), lazyInit.WithService(..)
	composeService := compose.NewComposeService(dockerClient, config.LoadDefaultConfigFile(os.Stderr))

	workingDirectory := "../samples/python/container_sample/" // TODO: accept flags
	configPaths := []string{}                                    // TODO: accept flags
	options, err := speccli.NewProjectOptions(configPaths, speccli.WithWorkingDirectory(workingDirectory), speccli.WithDefaultConfigPath)
	if err != nil {
		return err
	}
	project, err := speccli.ProjectFromOptions(options)
	if err != nil {
		return err
	}
	os.Chdir(project.WorkingDir)

	executionsChan := channels.NewRingChannel(1) //make(chan []fabricExecution)
	errChan := make(chan error)
	debounceDelay, err := time.ParseDuration("400ms") // TODO: accept flags
	if err != nil {
		return err
	}

	timeoutDelay, err := time.ParseDuration("1s") // TODO: accept flags
	if err != nil {
		return err
	}

	upOptions := api.UpOptions{
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
	}

	go listenFabric(ctx, fabricClient, executionsChan.In(), errChan)
	go syncCompose(ctx, project, composeService, upOptions, debounce(debounceDelay, executionsChan.Out()), errChan)

	return <-errChan
}

func main() {
	err := run()
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}
