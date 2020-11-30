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

	"github.com/docker/docker/client"
	"github.com/spf13/cobra"
)

// restartCmd represents the restart command
var restartCmd = &cobra.Command{
	Use:   "restart",
	Short: "Finds and restarts Perper Fabric Container",
	Run: func(cmd *cobra.Command, args []string) {
		fmt.Println("Restarting Fabric ...")
		restartFabricContainer()
	},
}

func init() {
	FabricCmd.AddCommand(restartCmd)
}

func restartFabricContainer() {
	ctx := context.Background()
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		panic(err)
	}
	containerID := findWorkingFabric(ctx, cli)
	if containerID == "" {
		fmt.Println("Could not find a fabric container")
		fmt.Println("Running new container")

		runFabricContainer()
	} else {
		err = cli.ContainerRestart(ctx, containerID, nil)
		if err != nil {
			panic(err)
		}
		fmt.Println("Fabric Restarted")
	}

}
