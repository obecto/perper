package fabric

import (
	"context"
	"fmt"

	"github.com/docker/docker/client"
	"github.com/spf13/cobra"
)

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
