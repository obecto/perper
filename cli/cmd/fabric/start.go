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
	"fmt"
	"strings"

	"github.com/spf13/cobra"
	"github.com/spf13/viper"

	"context"

	"github.com/docker/docker/api/types"
	"github.com/docker/docker/api/types/container"
	"github.com/docker/docker/client"
	"github.com/docker/go-connections/nat"
)

const imageName = "obecto/perper-fabric"

var showLogs bool
var ports []string

// startCmd represents the start command
var startCmd = &cobra.Command{
	Use:   "start",
	Short: "Creates new Fabric container",
	Long:  `Creates new Fabric container if there is a running Fabric it returns its id`,
	Run: func(cmd *cobra.Command, args []string) {
		fmt.Println("Starting fabric container ...")

		runFabricContainer()
	},
}

func init() {
	FabricCmd.AddCommand(startCmd)

	startCmd.Flags().BoolVarP(&showLogs, "logs", "l", false, "Keep logs open")
	startCmd.Flags().StringArrayVarP(&ports, "port", "p", nil, "Bind a container’s ports to a specific port ")
	viper.BindPFlag("port", startCmd.Flags().Lookup("port"))

}

func runFabricContainer() {
	ctx := context.Background()
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		panic(err)
	}

	fabricID := findWorkingFabric(ctx, cli)
	if fabricID != "" {
		fmt.Printf("There is a running fabric with ID: %s \n", fabricID)
		return
	}

	reader, err := cli.ImagePull(ctx, imageName, types.ImagePullOptions{})
	if err != nil || reader == nil {
		panic(err)
	}

	if ports != nil || len(ports) == 0 {
		ports = []string{"10800:10800", "40400:40400"}
	}
	portSet, portMap := getExposedPorts(ports)

	containerConfig := &container.Config{
		Image:        imageName,
		ExposedPorts: portSet,
	}

	hostConfig := &container.HostConfig{
		PortBindings: portMap,
	}

	resp, err := cli.ContainerCreate(ctx, containerConfig, hostConfig, nil, "")
	if err != nil {
		panic(err)
	}

	if err := cli.ContainerStart(ctx, resp.ID, types.ContainerStartOptions{}); err != nil {
		panic(err)
	}

	if showLogs {
		getContainerLogs(ctx, resp.ID, cli)
	} else {
		fmt.Println("Fabric started!")
	}
}

const errorParsingPort = "Error parsing port binding. It should be port:port example 80:3001"

func getExposedPorts(rowData []string) (nat.PortSet, nat.PortMap) {
	var portSet nat.PortSet = make(nat.PortSet)
	var portMap nat.PortMap = make(nat.PortMap)

	for _, port := range rowData {
		couple := strings.Split(port, ":")
		if len(couple) != 2 {
			panic(errorParsingPort)
		}

		p1, err1 := nat.NewPort("/tcp", couple[0])
		_, err2 := nat.NewPort("/tcp", couple[1])

		if err1 != nil && err2 != nil {
			panic(errorParsingPort)
		}
		portSet[p1] = struct{}{}
		portMap[p1] = []nat.PortBinding{
			{
				HostIP:   "0.0.0.0",
				HostPort: couple[1],
			},
		}
	}
	return portSet, portMap
}
