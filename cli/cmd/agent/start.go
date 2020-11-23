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
	"log"
	"os"
	"os/exec"
	"perper/cmd/agent/processes"

	"github.com/spf13/cobra"
)

// startCmd represents the start command
var startCmd = &cobra.Command{
	Use:   "start",
	Short: "Starts Azure Function",
	Long:  `Starts Azure Function. Should be executed from functions folder`,
	Run: func(cmd *cobra.Command, args []string) {
		startSingleAgent("./", 7071)
	},
}

func init() {
	AgentCmd.AddCommand(startCmd)
}

func startSingleAgent(dirPath string, port int) {
	if ok, pid := processes.FindProcessesOnPort(port); ok == false {
		fmt.Printf("Port [%d] already taken, from [PID=%d]\n", port, pid)
		fmt.Println("Killing the process")
		proc, err := os.FindProcess(pid)
		if err != nil {
			log.Println(err)
		}

		err = proc.Kill()
		if err != nil {
			log.Println(err)
		}
	}
	cmd := exec.Command("func", "host", "start", "--script-root", dirPath)

	outfile, err := os.Create(dirPath + logFileName)
	if err != nil {
		panic(err)
	}
	// defer outfile.Close()
	cmd.Stdout = outfile
	cmd.Stderr = outfile

	if err := cmd.Start(); err != nil {
		panic(err)
	}
	fmt.Printf("Started process with PID: %d \n", cmd.Process.Pid)
}
