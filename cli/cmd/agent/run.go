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
	"path"
	"strings"
	"syscall"

	"github.com/go-git/go-git/v5"
	"github.com/go-git/go-git/v5/plumbing/transport"
	"github.com/go-git/go-git/v5/plumbing/transport/http"
	"github.com/spf13/cobra"
	"github.com/spf13/viper"
	"golang.org/x/crypto/ssh/terminal"
)

//flags
var keepLogs bool
var pathFlag string

// runCmd represents the run command
var runCmd = &cobra.Command{
	Use:   "run",
	Short: "Runs Azure Function",
	Long:  `Runs Azure Function. Should be executed from functions folder`,
	Run: func(cmd *cobra.Command, args []string) {
		var agentsParentDir string = parentDir
		var g = newGraph()
		if pathFlag != defaultDir {
			agentsParentDir = path.Join(pathFlag, parentDir)
		}
		generateGraph(pathFlag, agentsParentDir, g)
		//Probably will be removed
		//Due to how Perper 0.6 works, it is not necessary to start agents in any specific order.
		g.topoSortGraph()
		fmt.Println(g.sequence)

		runMultipleAgents(agentsParentDir, g.sequence)
	},
}

func init() {
	AgentCmd.AddCommand(runCmd)
	runCmd.Flags().BoolVarP(&keepLogs, "keeplogs", "k", false, "Keep logs after execution end, otherwise the CLI deletes it")
	runCmd.Flags().StringVarP(&pathFlag, "path", "p", defaultDir, "Path to the first agent, otherwise serches the current dir for .perper.toml")
}

func readConfig(dirPath string) (map[string]string, string) {
	agentName := getAgentNameFromPath(path.Clean(dirPath))

	localConfig := viper.New()
	localConfig.SetConfigFile(path.Join(dirPath, configFileName))
	err := localConfig.ReadInConfig()
	if err != nil {
		panic(err)
	}
	agents := localConfig.GetStringMapString("dependencies")
	root := localConfig.GetString("name")
	if root != agentName {
		fmt.Println(agentName + " != " + root)
		panic("Expect agent name to be '" + agentName + "' in '" + path.Join(dirPath, configFileName) + "'")
	}

	return agents, root
}

func generateGraph(dirPath, agentsParentDir string, g *graph) {
	agents, root := readConfig(dirPath)
	agentNames := make([]string, 0, len(agents))
	for name, address := range agents {
		g.agentToAddress[name] = address
		agentNames = append(agentNames, name)
	}

	if len(agentNames) == 0 {
		g.add(root, root)
	}
	for _, v := range agentNames {
		g.add(v, root)
	}
	username := ""
	password := ""
	for _, v := range agentNames {
		filePath := path.Join(agentsParentDir, v, configFileName)
		if _, err := os.Stat(filePath); os.IsNotExist(err) {
			fmt.Println(filePath)
			username, password = cloneGitRepository(v, g.agentToAddress[v], agentsParentDir, username, password)
		}
		generateGraph(path.Join(agentsParentDir, v), agentsParentDir, g)
	}
}

func runMultipleAgents(agentsParentDir string, sequence []string) {
	for i, agent := range sequence {

		port := 7071 + i
		dirPath := checkForSrcFolder(path.Join(agentsParentDir, agent))
		runSingleAgent(dirPath, port)
	}
	if !keepLogs {
		sigs := make(chan os.Signal, 1)
		signal.Notify(sigs, syscall.SIGINT)
		go clearLogs(sigs, agentsParentDir, sequence)
	}

	dirPath := checkForSrcFolder(pathFlag)
	agentLog(path.Join(dirPath, logFileName), os.Stdout)
}

func clearLogs(sigs chan os.Signal, agentsParentDir string, sequence []string) {
	_ = <-sigs
	fmt.Println()
	fmt.Println("Interrupted")
	for _, v := range sequence {
		dirPath := checkForSrcFolder(path.Join(agentsParentDir, v))
		err := os.Remove(path.Join(dirPath, logFileName))
		if err != nil {
			panic(err)
		}
	}

	fmt.Println("Logs cleared")
	os.Exit(2)
}

func cloneGitRepository(agent, address, agentsParentDir, username, password string) (string, string) {
	var err error
	fmt.Printf("\nCloning %s ...\n", address)
	_, err = gitCloneCall(agent, address, agentsParentDir, username, password)
	if err != nil {
		if err == transport.ErrAuthenticationRequired {
			if username == "" || password == "" {
				username, password, err = credentials()
				if err != nil {
					panic(err)
				}
			}
			_, err = gitCloneCall(agent, address, agentsParentDir, username, password)
			if err != nil {
				panic(err)
			}
		} else {
			panic(err)
		}
	}
	if err != nil {
		panic(err)
	}
	return username, password
}

func gitCloneCall(agent, address, agentsParentDir, username, password string) (*git.Repository, error) {
	repo, err := git.PlainClone(path.Join(agentsParentDir, agent), false, &git.CloneOptions{
		URL:      address,
		Progress: os.Stdout,
		Auth:     &http.BasicAuth{Username: username, Password: password},
	})
	return repo, err
}

func runSingleAgent(dirPath string, port int) {

	cmd := exec.Command("func", "host", "start", "--script-root", dirPath, "--port", fmt.Sprint(port))

	outfile, err := os.Create(path.Join(dirPath, logFileName))
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

func checkForSrcFolder(dirPath string) string {
	if _, err := os.Stat(path.Join(dirPath, "src")); err == nil {
		dirPath = path.Join(dirPath, "src")
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
