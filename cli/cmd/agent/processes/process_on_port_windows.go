// +build windows

package processes

import (
	"io"
	"os/exec"
)

func GetProcessesOnPort() io.ReadCloser {

	cmd := exec.Command("netstat", "-ano")
}
