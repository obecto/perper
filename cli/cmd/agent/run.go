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
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"os/signal"
	"strings"
	"syscall"

	"github.com/go-git/go-git/v5"
	"github.com/go-git/go-git/v5/plumbing/transport/http"
	"github.com/spf13/cobra"
	"github.com/spf13/viper"
	"golang.org/x/crypto/ssh/terminal"
)

//flags
var keepLogs bool
var path string

//global variables
var g *graph
var agentsParentDir string = parentDir
var agentToAddress map[string]string
var username, password string

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
		agentToAddress = make(map[string]string)
		g = newGraph()
		if path != defaultDir {
			agentsParentDir = addSlash(path) + parentDir
		}
		generateGraph(path)
		g.topoSortGraph()

		fmt.Printf("Initialization order: ")
		fmt.Println(g.sequence)
		runMultipleAgents()
	},
}

func init() {
	AgentCmd.AddCommand(runCmd)
	runCmd.Flags().BoolVarP(&keepLogs, "keeplogs", "k", false, "Keep logs after execution end, otherwise the CLI deletes it")
	runCmd.Flags().StringVarP(&path, "path", "p", defaultDir, "Path to the first agent, otherwise serches the current dir for .perper.toml")
}

func readConfig(dirPath string) ([]string, string) {
	agentName := getAgentNameFromPath(trimSlash(dirPath))

	viper.SetConfigFile(addSlash(dirPath) + configFileName)
	err := viper.ReadInConfig()
	if err != nil {
		panic(err)
	}
	agents := viper.GetStringMapString("dependencies")
	root := viper.GetString("name")
	if root != agentName {
		fmt.Println(agentName + " != " + root)
		panic("Agent name differend in " + addSlash(dirPath) + configFileName)
	}

	agentNames := make([]string, 0, len(agents))
	for name, address := range agents {
		agentToAddress[name] = address
		agentNames = append(agentNames, name)
	}
	return agentNames, root
}

func generateGraph(dirPath string) {
	agents, root := readConfig(dirPath)
	if len(agents) == 0 {
		g.add(root, root)
	}
	for _, v := range agents {
		g.add(v, root)
	}
	for _, v := range agents {
		if _, err := os.Stat(addSlash(agentsParentDir+v) + configFileName); os.IsNotExist(err) {
			fmt.Println(addSlash(agentsParentDir+v) + configFileName)
			cloneGitRepository(v)
		}
		generateGraph(agentsParentDir + v)
	}
}

func runMultipleAgents() {
	for i, agent := range g.sequence {

		port := 7071 + i
		dirPath := agentsParentDir + agent
		if _, err := os.Stat(addSlash(dirPath) + "src"); err == nil {
			dirPath = addSlash(dirPath) + "src"
		}
		runSingleAgent(dirPath, port)
	}
	if !keepLogs {
		sigs := make(chan os.Signal, 1)
		signal.Notify(sigs, syscall.SIGINT)
		go clearLogs(sigs)
	}

	agentLog(addSlash(path)+logFileName, os.Stdout)
}

func clearLogs(sigs chan os.Signal) {
	sig := <-sigs
	fmt.Println()
	fmt.Println(sig)
	for _, v := range g.sequence {
		err := os.Remove(addSlash(agentsParentDir+v) + logFileName)
		if err != nil {
			panic(err)
		}
	}

	fmt.Println("Logs cleared")
	os.Exit(2)
}

func cloneGitRepository(agent string) {
	var err error
	if username == "" || password == "" {
		username, password, err = credentials()
		if err != nil {
			panic(err)
		}
	}

	fmt.Printf("Cloning %s ...\n", agentToAddress[agent])

	_, err = git.PlainClone(agentsParentDir+agent, false, &git.CloneOptions{
		URL:      agentToAddress[agent],
		Progress: os.Stdout,
		Auth:     &http.BasicAuth{Username: username, Password: password},
	})
	if err != nil {
		panic(err)
	}
}

func runSingleAgent(dirPath string, port int) {

	cmd := exec.Command("func", "host", "start", "--script-root", dirPath, "--port", fmt.Sprint(port))

	outfile, err := os.Create(addSlash(dirPath) + logFileName)
	if err != nil {
		panic(err)
	}
	cmd.Stdout = outfile
	cmd.Stderr = outfile

	if err := cmd.Start(); err != nil {
		panic(err)
	}
}

func getAgentNameFromPath(dirPath string) string {
	if dirPath == defaultDir {
		dir, err := os.Getwd()
		if err != nil {
			panic(err)
		}
		dirPath = dir
		index := strings.LastIndex(dir, "/")
		return dirPath[index+1:]
	}
	index := strings.LastIndex(dirPath, "/")
	if index == -1 {
		return dirPath
	}
	return dirPath[index+1:]
}

func addSlash(dirPath string) string {
	if dirPath[len(dirPath)-1] == '/' {
		return dirPath
	}

	return dirPath + "/"
}

func trimSlash(dirPath string) string {
	if dirPath[len(dirPath)-1] == '/' {
		return dirPath[:len(dirPath)-1]
	}

	return dirPath
}

func credentials() (string, string, error) {

	fmt.Println("Git credentials are needed.")
	reader := bufio.NewReader(os.Stdin)

	fmt.Print("Username: ")
	username, err := reader.ReadString('\n')
	if err != nil {
		return "", "", err
	}

	fmt.Print("Password: ")
	bytePassword, err := terminal.ReadPassword(int(syscall.Stdin))
	if err != nil {
		return "", "", err
	}
	fmt.Println()
	password := string(bytePassword)
	return strings.TrimSpace(username), strings.TrimSpace(password), nil
}
