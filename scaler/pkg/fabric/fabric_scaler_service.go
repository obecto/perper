package fabric

import (
	"context"
	"fmt"
	"os"
	"time"

	"github.com/obecto/perper/scaler/pb"

	"google.golang.org/grpc"
	// 	channels "gopkg.in/eapache/channels.v1"
)

type FabricService interface {
	Instances() <-chan FabricInstances

	Start(context.Context) error
}

type Agent string
type Instance string
type FabricInstances map[Agent][]Instance

type FabricOptions struct {
	Agent        string
	LocalToData  bool
	DebounceTime time.Duration

	//InstancePrefix string
}

func DefaultFabricOptions() FabricOptions {
	return FabricOptions{
		Agent: "Registry",
	}
}

type fabricService struct {
	options           FabricOptions
	client            proto.FabricClient
	executionsChannel chan FabricInstances
}

func NewFabricService(connection grpc.ClientConnInterface, options FabricOptions) FabricService {
	fabricClient := proto.NewFabricClient(connection)
	return &fabricService{
		options:           options,
		client:            fabricClient,
		executionsChannel: make(chan FabricInstances),
	}
}

func (s *fabricService) Instances() <-chan FabricInstances {
	return s.executionsChannel
}

func debouncedRingBuffer(delay time.Duration, input <-chan FabricInstances, output chan<- FabricInstances) {
	// based on https://gist.github.com/gigablah/80d7160f3577edc153c9
	// and https://github.com/eapache/channels/blob/47238d5aae8c0fefd518ef2bee46290909cf8263/ring_channel.go#L74

	var debouncedElem FabricInstances
	var timerOrNil <-chan time.Time
	var outputElem FabricInstances
	outputOrNil := output

	for {
		select {
		case elem, open := <-input:
			if open {
				debouncedElem = elem
				if timerOrNil == nil {
					timerOrNil = time.After(delay)
				}
			} else {
				input = nil
			}
		case <-timerOrNil:
			timerOrNil = nil
			outputElem = debouncedElem
			outputOrNil = output
		case outputOrNil <- outputElem:
			outputOrNil = nil
		}
	}
}

func (s *fabricService) Start(ctx context.Context) error {
	ctx, cancel := context.WithCancel(ctx)
	defer cancel()

	internalInstancesChannel := make(chan FabricInstances)
	defer close(internalInstancesChannel)

	go debouncedRingBuffer(s.options.DebounceTime, internalInstancesChannel, s.executionsChannel)

	executions, err := s.client.Executions(ctx, &proto.ExecutionsRequest{
		Agent:       s.options.Agent,
		LocalToData: s.options.LocalToData,
	})
	if err != nil {
		return err
	}

	aliveExecutions := make(map[Agent]map[Instance]struct{})
	outputReady := false

	for {
		execution, err := executions.Recv()
		fmt.Fprintf(os.Stdout, "%v\n", execution)

		if err != nil {
			return err
		}
		if execution.StartOfStream {
			outputReady = true
		}
		if execution.Instance != "" {
			agent := Agent(execution.Instance)
			instance := Instance(execution.Execution)

			if execution.Cancelled {
				delete(aliveExecutions[agent], instance)
			} else {
				if aliveExecutions[agent] == nil {
					aliveExecutions[agent] = make(map[Instance]struct{})
				}
				aliveExecutions[agent][instance] = struct{}{}
			}
		}

		if outputReady {
			result := make(FabricInstances)

			for agent, instances := range aliveExecutions {
				for instance, _ := range instances {
					result[agent] = append(result[agent], instance)
				}
			}

			internalInstancesChannel <- result
		}
	}
	//return nil
}
