package cmd

import (
	"fmt"
	"os"
	"perper/cmd/agent"
	"perper/cmd/fabric"

	"github.com/spf13/cobra"
)

var cfgFile string

// rootCmd represents the base command when called without any subcommands
var rootCmd = &cobra.Command{
	Use:   "perper",
	Short: "CLI interaction with perper",
	// Long:  `A longer description ...`,
}

// Execute adds all child commands to the root command and sets flags appropriately.
// This is called by main.main(). It only needs to happen once to the rootCmd.
func Execute() {
	if err := rootCmd.Execute(); err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
}

func init() {

	rootCmd.AddCommand(fabric.FabricCmd)
	rootCmd.AddCommand(agent.AgentCmd)
}
