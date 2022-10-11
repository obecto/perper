# KEDA Scaler Configuration

For event-driven autoscaling, Perper Fabric implements the [KEDA external scaler protocol](https://keda.sh/docs/2.0/concepts/external-scalers/).

Example usage:

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: scaledobject-name # Name of the ScaledObject resource itself
  namespace: scaledobject-namespace
spec:
  scaleTargetRef:
    name: deployment-name # Name of the Deployment/etc. to scale
  triggers:
    - type: external
      metadata:
        scalerAddress: perper-fabric.svc.local:40400
        agent: ScaledAgent # Name of the agent that the Deployment represents
        targetExecutions: "10" # Target executions per instance
        # OR: targetInstances: "10"
```

<!-- TODO: Flesh docs out -->
