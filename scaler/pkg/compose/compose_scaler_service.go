package compose

import (
	"context"
	"fmt"
	"os"

	"github.com/compose-spec/compose-go/types"
	"github.com/docker/compose/v2/pkg/api"
	"github.com/obecto/perper/scaler/pkg/fabric"
)

type ComposeScalerService interface {
	Start(context.Context) error
}

type ComposeScalerOptions struct {
	Project        types.Project
	ComposeService api.Service
	Up             api.UpOptions
}

type composeScalerService struct {
	options ComposeScalerOptions
	fabric  fabric.FabricService
	compose api.Service
}

func NewComposeScalerService(composeService api.Service, fabricService fabric.FabricService, options ComposeScalerOptions) ComposeScalerService {
	return &composeScalerService{
		fabric:  fabricService,
		compose: composeService,
		options: options,
	}
}

func (c *composeScalerService) Start(ctx context.Context) error {
	for {
		select {
		case <-ctx.Done():
			return nil
		case executions := <-c.fabric.Executions():
			project := c.options.Project // <<-- Important that we make a copy here
			project.Services = nil       // TODO: Only remove services that are managed by us

			for _, execution := range executions {
				service, err := project.GetService(execution.Agent)
				if err != nil {
					// return err
					fmt.Fprintf(os.Stderr, "Unable to locate agent %s: %v\n", execution.Agent, err)
					continue
				}

				environment := make(types.MappingWithEquals)
				for k, v := range service.Environment {
					environment[k] = v
				}
				executionCopy := execution
				environment["X_PERPER_AGENT"] = &executionCopy.Agent
				environment["X_PERPER_INSTANCE"] = &executionCopy.Instance
				service.Environment = environment
				service.Name = fmt.Sprintf("%s-%s", service.Name, execution.Instance)
				project.Services = append(project.Services, service)
			}

			{
				json, err := c.compose.Convert(ctx, &project, api.ConvertOptions{
					Format: "json",
				})
				if err != nil {
					return err
				}
				fmt.Fprint(os.Stdout, string(json))
			}

			err := c.compose.Up(ctx, &project, c.options.Up)
			if err != nil {
				return err
			}
		}
	}
}
