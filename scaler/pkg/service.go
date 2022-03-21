package scaler

import (
	"context"
	"fmt"
	"os"
)

type Service interface {
	Start(context.Context) error
}

func Run(ctx context.Context, services ...Service) error {
	errorChannel := make(chan error)
	ctx, cancel := context.WithCancel(ctx)

	for _, service := range services {
		serviceRef := service
		go func() {
			err := serviceRef.Start(ctx)
			errorChannel <- err
		}()
	}

	var err error
	for _ = range services {
		serviceErr := <-errorChannel
		if serviceErr != nil {
			if err != nil {
				fmt.Fprintln(os.Stderr, err) // TODO: Make more conventional
			}
			err = serviceErr
			cancel()
		}
	}
	cancel()
	return err
}
