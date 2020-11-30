package fabric

import (
	"context"

	"github.com/docker/docker/api/types"
	"github.com/docker/docker/client"
	"github.com/spf13/cobra"
)

var FabricCmd = &cobra.Command{
	Use:   "fabric",
	Short: "Interaction with Perper Fabric",
}

func init() {}

func findWorkingFabric(ctx context.Context, cli *client.Client) string {
	containers, err := cli.ContainerList(ctx, types.ContainerListOptions{})
	if err != nil {
		panic(err)
	}
	for _, elem := range containers {
		if elem.Image == imageName {
			return elem.ID
		}
	}
	return ""
}
