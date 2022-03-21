/*
Copyright 2022.

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

package main

import (
	"context"
	"fmt"
	"os"
	
	// 	"github.com/spf13/cobra"
	"k8s.io/client-go/dynamic"
	"k8s.io/client-go/rest"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"

	"github.com/obecto/perper/scaler/pkg"
	"github.com/obecto/perper/scaler/pkg/fabric"
	kubernetesscaler "github.com/obecto/perper/scaler/pkg/kubernetes"
)

func run(ctx context.Context) error {
	addr := "perper-fabric:40400" // TODO: accept flags

	namespace := "default" // TODO: accept flags

	fabricOptions := fabric.DefaultFabricOptions() // TODO: accept flags

	kubernetesScalerOptions := kubernetesscaler.KubernetesScalerOptions{
		Namespace: namespace,
	}

	connection, err := grpc.Dial(addr, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		return err
	}
	defer connection.Close()

	fabricService := fabric.NewFabricService(connection, fabricOptions)
	
	// creates the in-cluster config
	config, err := rest.InClusterConfig()
	if err != nil {
		panic(err.Error())
	}

	client, err := dynamic.NewForConfig(config)
	if err != nil {
		return err
	}

	kubernetesScalerService := kubernetesscaler.NewKubernetesScalerService(client, fabricService, kubernetesScalerOptions)

	return scaler.Run(ctx, fabricService, kubernetesScalerService)
}

func main() {
	err := run(context.Background())
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}
