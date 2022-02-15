package compose

import (
	"context"
	"fmt"
	"os"
	"encoding/json"

	"golang.org/x/sync/errgroup"
	"k8s.io/client-go/util/retry"

	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/apis/meta/v1/unstructured"
	"k8s.io/apimachinery/pkg/runtime/schema"
	"k8s.io/client-go/dynamic"

	"github.com/obecto/perper/scaler/pkg/fabric"
)

type KubernetesScalerService interface {
	Start(context.Context) error
}

type KubernetesScalerOptions struct {
	Namespace string
}

type kubernetesScalerService struct {
	options KubernetesScalerOptions
	fabric  fabric.FabricService
	client  dynamic.Interface
}

var agentGRV = schema.GroupVersionResource{Group: "kudo.dev", Version: "v1beta1", Resource: "instances"}
var agentInstancesField = []string{"spec", "parameters", "instances"}
var agentLabel = "perper.obecto.com/agent"

func NewKubernetesScalerService(kubernetesClient dynamic.Interface, fabricService fabric.FabricService, options KubernetesScalerOptions) KubernetesScalerService {
	return &kubernetesScalerService{
		options: options,
		fabric:  fabricService,
		client:  kubernetesClient,
	}
}

func (c *kubernetesScalerService) Start(ctx context.Context) error {
	for {
		select {
		case <-ctx.Done():
			return nil
		case allInstances := <-c.fabric.Instances():
			group, updateCtx := errgroup.WithContext(ctx)

			for agent, instances := range allInstances {
				agentCopy := string(agent)

				instancesCopy, err := json.Marshal(instances)
				if err != nil {
					return err
				}

				group.Go(func() error {
					return retry.RetryOnConflict(retry.DefaultRetry, func() error {
						resourceIntreface := c.client.Resource(agentGRV).Namespace(c.options.Namespace)
						labelSelector := metav1.LabelSelector{MatchLabels: map[string]string{agentLabel: agentCopy}}

						result, err := resourceIntreface.List(ctx, metav1.ListOptions{LabelSelector: metav1.FormatLabelSelector(&labelSelector), Limit: 1})
						if err != nil {
							return err
						}

						if len(result.Items) == 0 {
							fmt.Fprintf(os.Stderr, "Unable to locate agent %s\n", agentCopy)
							return nil
						}

						for _, item := range result.Items {
							err = unstructured.SetNestedField(item.Object, string(instancesCopy), agentInstancesField...)
							if err != nil {
								return err
							}

							_, err = resourceIntreface.Update(updateCtx, &item, metav1.UpdateOptions{})
							return err
						}
						return nil
					})
				})
			}

			err := group.Wait()
			if err != nil {
				return err
			}
		}
	}
}
