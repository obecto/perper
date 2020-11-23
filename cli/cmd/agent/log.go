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
	"io"
	"os"
	"time"

	"github.com/spf13/cobra"
)

// logCmd represents the log command
var logCmd = &cobra.Command{
	Use:   "log",
	Short: "Gets logs of an agent",

	Run: func(cmd *cobra.Command, args []string) {
		tail(logFileName, os.Stdout)
	},
}

func init() {
	AgentCmd.AddCommand(logCmd)
}

func tail(filename string, out io.Writer) {
	f, err := os.Open(filename)
	if err != nil {
		panic(err)
	}
	defer f.Close()
	r := bufio.NewReader(f)
	info, err := f.Stat()
	if err != nil {
		panic(err)
	}
	oldSize := info.Size()
	for {
		for line, prefix, err := r.ReadLine(); err != io.EOF; line, prefix, err = r.ReadLine() {
			if prefix {
				fmt.Fprint(out, string(line))
			} else {
				fmt.Fprintln(out, string(line))
			}
		}
		pos, err := f.Seek(0, io.SeekCurrent)
		if err != nil {
			panic(err)
		}
		for {
			time.Sleep(time.Second)
			newinfo, err := f.Stat()
			if err != nil {
				panic(err)
			}
			newSize := newinfo.Size()
			if newSize != oldSize {
				if newSize < oldSize {
					f.Seek(0, 0)
				} else {
					f.Seek(pos, io.SeekStart)
				}
				r = bufio.NewReader(f)
				oldSize = newSize
				break
			}
		}
	}
}
