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
		options: options,
		fabric:  fabricService,
		compose: composeService,
	}
}

func (c *composeScalerService) Start(ctx context.Context) error {
	for {
		select {
		case <-ctx.Done():
			return nil
		case allInstances := <-c.fabric.Instances():
			project := c.options.Project // <<-- Important that we make a copy here
			project.Services = nil       // TODO: Only remove services that are managed by us

			for agent, instances := range allInstances {
				service, err := project.GetService(string(agent))
				if err != nil {
					// return err
					fmt.Fprintf(os.Stderr, "Unable to locate agent %s: %v\n", agent, err)
					continue
				}
				for _, instance := range instances {
					serviceCopy := service
					environment := make(types.MappingWithEquals)
					for k, v := range serviceCopy.Environment {
						environment[k] = v
					}

					agentCopy, instanceCopy := string(agent), string(instance)
					environment["X_PERPER_AGENT"] = &agentCopy
					environment["X_PERPER_INSTANCE"] = &instanceCopy

					serviceCopy.Environment = environment
					serviceCopy.Name = fmt.Sprintf("%s-%s", serviceCopy.Name, instance)
					project.Services = append(project.Services, serviceCopy)
				}
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
