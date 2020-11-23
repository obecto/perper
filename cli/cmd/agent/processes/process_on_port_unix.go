// +build !windows

package processes

import (
	"bufio"
	"io"
	"log"
	"os/exec"
	"strconv"
	"strings"
)

//
func FindProcessesOnPort(port int) (bool, int) {

	stdout := GetProcessesOnPort()

	scanner := bufio.NewScanner(stdout)
	for scanner.Scan() {
		if strings.Contains(scanner.Text(), strconv.Itoa(port)) {
			index := strings.Index(scanner.Text(), "pid=")
			pid := GetPidFromLine(scanner.Text(), index+4)

			return false, pid
		}
	}
	return true, 0
}

//
func GetProcessesOnPort() io.ReadCloser {
	cmd := exec.Command("ss", "-tulpn")
	stdout, err := cmd.StdoutPipe()
	if err != nil {
		log.Fatal(err)
	}
	if err := cmd.Start(); err != nil {
		panic(err)
	}
	return stdout
}

//
func GetPidFromLine(line string, index int) int {
	var save = 0
	for line[index] != ',' {
		curr, err := strconv.Atoi(string(line[index]))
		if err != nil {
			panic(err)
		}
		save = save*10 + curr
		index++
	}

	return save
}
