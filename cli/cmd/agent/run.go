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
	"os"
	"os/exec"
	"os/signal"
	"syscall"

	"github.com/spf13/cobra"
	"github.com/spf13/viper"
)

var keepLogs bool

var g *graph

func newGraph() *graph {
	g = &graph{}
	g.edges = make(map[string][]string)
	return g
}

// runCmd represents the run command
var runCmd = &cobra.Command{
	Use:   "run",
	Short: "Runs Azure Function",
	Long:  `Runs Azure Function. Should be executed from functions folder`,
	Run: func(cmd *cobra.Command, args []string) {
		g = newGraph()
		g.generateGraph(".")
		g.topoSortGraph()

		runMultipleAgents()
	},
}

func init() {
	AgentCmd.AddCommand(runCmd)
	runCmd.Flags().BoolVarP(&keepLogs, "keeplogs", "k", false, "Keep logs after execution end, otherwise the CLI deletes it")
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

func runMultipleAgents() {

	for i, v := range g.sequence {
		port := 7071 + i
		runSingleAgent("../"+v, port)
	}
	if !keepLogs {
		sigs := make(chan os.Signal, 1)
		signal.Notify(sigs, syscall.SIGINT)
		go clearLogs(sigs)
	}

	agentLog(logFileName, os.Stdout)
}

func clearLogs(sigs chan os.Signal) {
	sig := <-sigs
	fmt.Println()
	fmt.Println(sig)
	for _, v := range g.sequence {
		err := os.Remove("../" + v + "/" + logFileName)
		if err != nil {
			panic(err)
		}
	}

	fmt.Println("Logs cleared")
	os.Exit(2)
}

func runSingleAgent(dirPath string, port int) {

	cmd := exec.Command("func", "host", "start", "--script-root", dirPath)

	outfile, err := os.Create(dirPath + "/" + logFileName)
	if err != nil {
		panic(err)
	}
	cmd.Stdout = outfile
	cmd.Stderr = outfile

	if err := cmd.Start(); err != nil {
		panic(err)
	}
}
