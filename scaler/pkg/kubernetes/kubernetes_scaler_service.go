package compose

import (
	"context"
	"encoding/json"
	"fmt"
	"os"
	"crypto/sha1"
    "encoding/hex"

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
var instanceLabel = "perper.obecto.com/instance"
var podGRV = schema.GroupVersionResource{Group: "", Version: "v1", Resource: "pods"}

func NewKubernetesScalerService(kubernetesClient dynamic.Interface, fabricService fabric.FabricService, options KubernetesScalerOptions) KubernetesScalerService {
	return &kubernetesScalerService{
		options: options,
		fabric:  fabricService,
		client:  kubernetesClient,
	}
}

func HashValues(values []fabric.Instance) []string {
	var r []string
	for _, v := range values {
		h := sha1.New()
    	h.Write([]byte(v))
		s := hex.EncodeToString(h.Sum(nil))
		r = append(r, s)
	}
	return r
}

func (c *kubernetesScalerService) Start(ctx context.Context) error {
	fmt.Fprintf(os.Stdout, "Starting scaler\n")
	for {
		select {
		case <-ctx.Done():
			return nil
		case allInstances := <-c.fabric.Instances():
			for agent, instances := range allInstances {
				agentCopy := string(agent)
				fmt.Fprintf(os.Stdout, "Got agent: %s\n", agentCopy)
				
				instancesCopy, err := json.Marshal(HashValues(instances))
				if err != nil {
					return err
				}

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
					currentInstancesStr, _, err := unstructured.NestedString(item.Object, agentInstancesField...)
					fmt.Fprintf(os.Stdout, "Agent instance: %s with instances: %s to be updated with %s\n", agentCopy, currentInstancesStr, string(instancesCopy))
					
					if len(currentInstancesStr) > 0 {
						var currentInstances []string
						if err := json.Unmarshal([]byte(currentInstancesStr), &currentInstances); err != nil {
							return err
						}
						for _, currentInstance := range currentInstances {
							found := false
							for _, newInstance := range instances {
								fmt.Fprintf(os.Stdout, "Compare: %s to with %s\n", currentInstance, string(newInstance))
								if currentInstance == string(newInstance) {
									found = true
									break
								}
							}
							if !found {
								podIntreface := c.client.Resource(podGRV).Namespace(c.options.Namespace)
								podSelector := metav1.LabelSelector{MatchLabels: map[string]string{instanceLabel: currentInstance}}
								fmt.Fprintf(os.Stdout, "Query pods with label %s: %s...\n", instanceLabel, currentInstance)
								pods, err := podIntreface.List(ctx, metav1.ListOptions{LabelSelector: metav1.FormatLabelSelector(&podSelector), Limit: 1})
								if err != nil {
									fmt.Fprintf(os.Stdout, "No pods found - %s", currentInstance)
									return err
								}
								for _, pod := range pods.Items {
									fmt.Fprintf(os.Stdout, "Attempt to delete pod %s...\n", pod.GetName())
									err = podIntreface.Delete(ctx, pod.GetName(), metav1.DeleteOptions{})
									if err != nil {
										return err
									}
								}
							}
						}
					}

					err = unstructured.SetNestedField(item.Object, string(instancesCopy), agentInstancesField...)
					if err != nil {
						return err
					}

					_, err = resourceIntreface.Update(ctx, &item, metav1.UpdateOptions{})
					if err != nil {
						return err
					}
				}	
			}
		}
	}
}
