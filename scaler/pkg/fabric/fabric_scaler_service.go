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
	//GetExecutions() FabricExecutions
	Executions() <-chan FabricExecutions // TODO

	Start(context.Context) error
}

type FabricExecutions []FabricExecution

type FabricExecution struct {
	Agent    string
	Instance string
}

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
	executionsChannel chan FabricExecutions

	//executions FabricExecutions// TODO
}

func NewFabricService(connection grpc.ClientConnInterface, options FabricOptions) FabricService {
	fabricClient := proto.NewFabricClient(connection)
	return &fabricService{
		client:            fabricClient,
		executionsChannel: make(chan FabricExecutions),
		options:           options,
	}
}

func (s *fabricService) Executions() <-chan FabricExecutions {
	return s.executionsChannel
}

/*func ringBuffer(delay time.Duration, input <-chan FabricExecutions, output chan<- FabricExecutions) {
	// based on https://github.com/eapache/channels/blob/47238d5aae8c0fefd518ef2bee46290909cf8263/ring_channel.go#L74
	var next FabricExecutions
	outputOrNil := output

	for input != nil || outputOrNil != nil {
		select {
		case elem, open := <-input:
			if open {
				next = elem
				outputOrNil = output
			} else {
				input = nil
			}
		case outputOrNil <- next:
			outputOrNil = nil
	}

	close(output)
}

func debounce(delay time.Duration, input <-chan FabricExecutions, output chan<- FabricExecutions) {
	// based on https://gist.github.com/gigablah/80d7160f3577edc153c9
	var timer <-chan time.Time
	var last FabricExecutions
	var ok bool

	for {
		select {
		case last, ok = <-input:
			if !ok {
				close(output)
				return
			}
			if timer == nil {
				timer = time.After(delay)
			}
		case <-timer:
			output <- last
			timer = nil
		}
	}
}*/

func debouncedRingBuffer(delay time.Duration, input <-chan FabricExecutions, output chan<- FabricExecutions) {
	// based on https://gist.github.com/gigablah/80d7160f3577edc153c9
	// and https://github.com/eapache/channels/blob/47238d5aae8c0fefd518ef2bee46290909cf8263/ring_channel.go#L74

	var debouncedElem FabricExecutions
	var timerOrNil <-chan time.Time
	var outputElem FabricExecutions
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

/*func (s fabricService*) Start(ctx context.Context) error {
	internalExecutionsChannel := make(chan FabricExecutions)
	defer close(internalExecutionsChannel)

	go debouncedRingBuffer(s.options.Delay, internalExecutionsChannel, s.executionsChannel)

	for {
		ctxTimeout, cancel := context.WithTimeout(ctx, time.Second)
		defer cancel()

		allExecutions, err := client.AllExecutions(ctxTimeout, &proto.ExecutionsRequest{
			Agent:       s.options.Agent,
			LocalToData: s.options.LocalToData,
		})
		if err != nil {
			return err
		}

		result := make([]fabricExecution, 0, len(allExecutions.Executions))

		for _, execution := range allExecutions.Executions {
			result = append(result, FabricExecution{
				Agent:    execution.Instance,
				Instance: execution.Execution,
			})
		}

		internalExecutionsChannel <- result
	}
}*/

func (s *fabricService) Start(ctx context.Context) error {
	ctx, cancel := context.WithCancel(ctx)
	defer cancel()

	internalExecutionsChannel := make(chan FabricExecutions)
	defer close(internalExecutionsChannel)

	go debouncedRingBuffer(s.options.DebounceTime, internalExecutionsChannel, s.executionsChannel)

	executions, err := s.client.Executions(ctx, &proto.ExecutionsRequest{
		Agent:       s.options.Agent,
		LocalToData: s.options.LocalToData,
	})
	if err != nil {
		return err
	}

	aliveExecutions := map[FabricExecution]struct{}{}
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
			key := FabricExecution{
				Agent:    execution.Instance,
				Instance: execution.Execution,
			}

			if execution.Cancelled {
				delete(aliveExecutions, key)
			} else {
				aliveExecutions[key] = struct{}{}
			}
		}

		if outputReady {
			result := make(FabricExecutions, 0, len(aliveExecutions))

			for execution, _ := range aliveExecutions {
				result = append(result, execution)
			}

			internalExecutionsChannel <- result
		}
	}
	//return nil
}
