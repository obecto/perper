package agent

import (
	"github.com/spf13/cobra"
)

var AgentCmd = &cobra.Command{
	Use:   "agent",
	Short: "Interact with perper agents",
}

func init() {}
