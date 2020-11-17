/*
Copyright © 2020 NAME HERE <EMAIL ADDRESS>

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
package fabric

import (
	"context"
	"fmt"

	"github.com/spf13/cobra"

	"github.com/docker/docker/api/types"
	"github.com/docker/docker/client"
)

// stopCmd represents the stop command
var stopCmd = &cobra.Command{
	Use:   "stop",
	Short: "Kills and removes a container from the docker host.",
	Long: `Finds a container with Fabric image type, kills and removes it.
	Returns panic If there is not present Fabric container`,

	Run: func(cmd *cobra.Command, args []string) {
		stopFabricContainer()
	},
}

func init() {
	FabricCmd.AddCommand(stopCmd)
}

func stopFabricContainer() {
	ctx := context.Background()
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		panic(err)
	}
	containerID := findWorkingFabric(ctx, cli)
	if containerID == "" {
		panic("Could not find a fabric container")
	}

	err = cli.ContainerStop(ctx, containerID[:12], nil)
	if err != nil {
		panic(err)
	}

	err = cli.ContainerRemove(ctx, containerID[:12], types.ContainerRemoveOptions{})
	if err != nil {
		panic(err)
	}
	fmt.Println("Fabric Stopped!")
}
