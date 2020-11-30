package fabric

import (
	"fmt"

	"context"
	"io"
	"os"

	"github.com/spf13/cobra"

	"github.com/docker/docker/api/types"
	"github.com/docker/docker/client"
)

var logsCmd = &cobra.Command{
	Use:   "logs",
	Short: "Shows logs from Perper Fabric",
	Long:  `Finds a container with Fabric image type and prints its output`,
	Run: func(cmd *cobra.Command, args []string) {
		fmt.Println("logs called")
		getContainerLogs(nil, "", nil)
	},
}

func init() {
	FabricCmd.AddCommand(logsCmd)
}

func getContainerLogs(ctx context.Context, containerID string, cli *client.Client) {
	var err error
	if cli == nil || ctx == nil {
		ctx = context.Background()
		cli, err = client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
		if err != nil {
			panic(err)
		}
		containerID = findWorkingFabric(ctx, cli)
		if containerID == "" {
			panic("Could not find a fabric container")
		}
	}

	options := types.ContainerLogsOptions{
		ShowStdout: true,
		Follow:     true,
	}
	// Replace this ID with a container that really exists
	out, err := cli.ContainerLogs(ctx, containerID, options)
	if err != nil {
		panic(err)
	}

	io.Copy(os.Stdout, out)
}
