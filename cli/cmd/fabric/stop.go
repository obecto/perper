package fabric

import (
	"context"
	"fmt"

	"github.com/spf13/cobra"

	"github.com/docker/docker/api/types"
	"github.com/docker/docker/client"
)

var stopCmd = &cobra.Command{
	Use:   "stop",
	Short: "Kills and removes a container from the docker host.",
	Long: `Finds a container with Fabric image type, kills and removes it.
	Errors if there is no Fabric container present`,

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
		fmt.Println("Could not find a fabric container")
		return
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
