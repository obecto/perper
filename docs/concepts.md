# Concepts

Perper's programming model is based on "Agents" -- encapsulated distributed objects that can hold State, processes calls, and receive or produce streams.

In addition, Perper makes use of Object Capabilities. The only way for one Agent to be able to use another Agent or a Stream is if it has a reference to it -- either by virtue of creating a new Agent or by having been passed a reference earlier.

## Agent

Agents are similar to classes in conventinal OOP programming. They can be instanciated, producing an Agent Instance (confusingly often also named an "Agent"); they can have state; they can expose methods to be called through executions; and they can interact with streams. As such, agents are the basic unit of encapsulation in a Perper system.

### Agent Type

"Agent Type" is used for disambiguation in cases when "Agent" might be misinterpreted to mean "Agent Instance".

### Agent Instance

An Agent Instance is an individual instance of an Agent.

## Execution

Executions are used to model anything that needs to run, is currently running, or has just finished running. Every Execution is associated with an Agent Instance, a "delegate" specifying the method to be called, arbitrary parameters passed from the caller, and, if the Execution has finished, a result or error value to be passed back to the caller.

Keeping in our analogy with typical OOP programming, executions take on roughly the role of stack frames, though without storing intermediate state.

## State

In its essence, State is a distributed key-value store that can be accessed by executions in order to persist data. It ensures that even if a machine or a process dies, we can resume execution when the affected processes are restarted elsewhere.

A State can be linked to an Agent Instance or to an Execution; and an Instance or Execution can have multiple states that it can refer to by name. In later versons, there would also be the option for a State to be free-standing and freely shared between Agent Instances, akin to shared memory.

## Stream

Streams are similar to states in that they allow for data to be persisted. However, unlike states, they also allow for listeners to wait until there is new data available for reading. Perper allows for the construction of arbitrary stream graphs between agents (including cyclic graphs), allowing for complex streaming calculations to be expressed simply.

Streams can have multiple writers and multiple listeners -- where both writers and listeners are executions. Every listener receives the whole of the Stream.

### Ephemeral Stream

A Stream can be marked as "ephemeral", which would cause items to be deleted after the all listeners have finished processing them.

### Indexed Stream

A Stream can be indexed and later queried, allowing for the fast retrieval of items from the Stream without needing to process all of it.

### Stream Item

A Stream Item is just an value inside the Stream. Every Stream Item needs a monotonically increasing integer key, to allow all listeners to end up with the same view of the order of the items of the Stream. Streams marked as "packed" allow for items to be written out of order, as long as writers use consequitive keys -- those are, for example, useful for modelling time series data.
