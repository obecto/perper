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
package agent

import (
	"fmt"

	"github.com/spf13/cobra"
	"github.com/spf13/viper"
)

// multipleCmd represents the multiple command
var startMultipleCmd = &cobra.Command{
	Use:   "start-multiple",
	Short: "A brief description of your command",
	Long:  `A longer description ...`,
	Run: func(cmd *cobra.Command, args []string) {
		fmt.Println("multiple called")
		g = newGraph()
		g.generateGraph(".")

		g.topoSortGraph()

		fmt.Println(g.sequence)

		startMultipleAgents()
	},
}

func init() {
	AgentCmd.AddCommand(startMultipleCmd)
}

func readConfig(dirPath string) ([]string, string) {
	viper.SetConfigFile(dirPath + "/" + configFileName)
	err := viper.ReadInConfig()
	if err != nil {
		panic(err)
	}
	agents := viper.GetStringSlice("dependencies.agents")
	name := viper.GetString("name")

	return agents, name
}

var g *Graph

func newGraph() *Graph {
	g = &Graph{}
	g.edges = make(map[string][]string)
	return g
}

func startMultipleAgents() {
	for i, v := range g.sequence {
		port := 7071 + i
		startSingleAgent("../"+v, port)
	}
}
