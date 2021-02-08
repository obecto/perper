package agent

import (
	"fmt"
	"os"
	"os/exec"
	"os/signal"
	"path/filepath"
	"syscall"

	"github.com/spf13/cobra"
	"github.com/spf13/viper"
)

//flags
var keepLogs bool
var pathFlag string

var runCmd = &cobra.Command{
	Use:   "run",
	Short: "Runs Azure Function",
	Long:  `Runs Azure Function. Should be executed from functions folder`,
	Run: func(cmd *cobra.Command, args []string) {
		var g = newGraph()
		var agentsParentDir = getParentDirName()

		generateGraph(pathFlag, agentsParentDir, g)
		//Probably will be removed
		//Due to how Perper 0.6 works, it is not necessary to start agents in any specific order.
		g.topoSortGraph()

		os.Setenv(rootAgentName, getAgentNameFromPath(pathFlag))

		runMultipleAgents(agentsParentDir, g.sequence)
		os.Unsetenv(rootAgentName)
	},
}

func init() {
	AgentCmd.AddCommand(runCmd)
	runCmd.Flags().BoolVarP(&keepLogs, "keeplogs", "k", false, "Keep logs after execution end, otherwise the CLI deletes it")
	runCmd.Flags().StringVarP(&pathFlag, "path", "p", defaultDir, "Path to the first agent, otherwise serches the current dir for .perper.toml")
}

func readConfig(dirPath string) (map[string]string, string) {
	agentName := getAgentNameFromPath(filepath.Clean(dirPath))

	localConfig := viper.New()
	localConfig.SetConfigFile(filepath.Join(dirPath, configFileName))
	err := localConfig.ReadInConfig()
	if err != nil {
		fmt.Println("Error while parsing: " + filepath.Join(dirPath, configFileName))
		panic(err)
	}
	agents := localConfig.GetStringMapString("dependencies")
	root := localConfig.GetString("name")
	if root != agentName {
		fmt.Println(agentName + " != " + root)
		panic("Expect agent name to be '" + agentName + "' in '" + filepath.Join(dirPath, configFileName) + "'")
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

	for _, v := range agentNames {
		filePath := filepath.Join(agentsParentDir, v, configFileName)
		if _, err := os.Stat(filePath); os.IsNotExist(err) {
			gitCloneWithExec(v, g.agentToAddress[v], agentsParentDir)
		}
		generateGraph(filepath.Join(agentsParentDir, v), agentsParentDir, g)
	}
}

func runMultipleAgents(agentsParentDir string, sequence []string) {
	for i, agent := range sequence {

		port := 7071 + i
		dirPath := checkForSrcFolder(filepath.Join(agentsParentDir, agent))
		runSingleAgent(dirPath, agent, port)
	}
	if !keepLogs {
		sigs := make(chan os.Signal, 1)
		signal.Notify(sigs, syscall.SIGINT)
		go clearLogs(sigs, agentsParentDir, sequence)
	}

	dirPath := checkForSrcFolder(pathFlag)
	agentLog(filepath.Join(dirPath, logFileName), os.Stdout)
}

func clearLogs(sigs chan os.Signal, agentsParentDir string, sequence []string) {
	_ = <-sigs
	fmt.Println()
	fmt.Println("Interrupted")
	for _, v := range sequence {
		dirPath := checkForSrcFolder(filepath.Join(agentsParentDir, v))
		err := os.Remove(filepath.Join(dirPath, logFileName))
		if err != nil {
			panic(err)
		}
	}

	fmt.Println("Logs cleared")
	os.Exit(2)
}

func gitCloneWithExec(agent, address, agentsParentDir string) {
	cmd := exec.Command("git", "clone", address)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stdout
	if err := cmd.Run(); err != nil {
		panic(err)
	}
}

func runSingleAgent(dirPath, agent string, port int) {

	cmd := exec.Command("func", "host", "start", "--script-root", dirPath, "--port", fmt.Sprint(port))

	outfile, err := os.Create(filepath.Join(dirPath, logFileName))
	if err != nil {
		panic(err)
	}
	cmd.Stdout = outfile
	cmd.Stderr = outfile

	os.Setenv(currentAgentName, agent)

	if err := cmd.Start(); err != nil {
		panic(err)
	}
	os.Unsetenv(currentAgentName)
}

func getAgentNameFromPath(dirPath string) string {
	if dirPath == defaultDir {
		dir, err := os.Getwd()
		if err != nil {
			panic(err)
		}
		dirPath = dir
	}
	return filepath.Base(dirPath)
}

func getParentDirName() string {
	agentsParentDir := parentDir
	if pathFlag != defaultDir {
		agentsParentDir = filepath.Join(pathFlag, parentDir)
	}
	return agentsParentDir
}

func checkForSrcFolder(dirPath string) string {
	if _, err := os.Stat(filepath.Join(dirPath, "src")); err == nil {
		dirPath = filepath.Join(dirPath, "src")
	}
	return dirPath
}
