package agent

import (
	"fmt"
	"os"
	"os/exec"
	"os/signal"
	"path"
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

	for _, v := range agentNames {
		filePath := path.Join(agentsParentDir, v, configFileName)
		if _, err := os.Stat(filePath); os.IsNotExist(err) {
			gitCloneWithExec(v, g.agentToAddress[v], agentsParentDir)
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

func gitCloneWithExec(agent, address, agentsParentDir string) {
	cmd := exec.Command("git", "clone", address)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stdout
	if err := cmd.Run(); err != nil {
		panic(err)
	}
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
	}
	return path.Base(dirPath)
}

func getParentDirName() string {
	agentsParentDir := parentDir
	if pathFlag != defaultDir {
		agentsParentDir = path.Join(pathFlag, parentDir)
	}
	return agentsParentDir
}

func checkForSrcFolder(dirPath string) string {
	if _, err := os.Stat(path.Join(dirPath, "src")); err == nil {
		dirPath = path.Join(dirPath, "src")
	}
	return dirPath
}
