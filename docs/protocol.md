# Perper Fabric Protocol

<details> <summary> Table of Contents </summary>

* [Versioning](#versioning)
* [Ignite caches](#ignite-caches)
  * [Executions cache](#executions-cache)
  * [Stream listeners cache](#stream-listeners-cache)
  * [Stream caches](#stream-caches)
  * [State caches](#state-caches)
  * [Standard objects](#standard-objects)
* [GRPC protocol](#grpc-protocol)
* [Operations](#operations)
  * [IDs](#ids)

</details>

The Perper Fabric Protocol is built on top of [Ignite's thin client protocol](https://ignite.apache.org/docs/2.12.0/thin-clients/getting-started-with-thin-clients) and [GRPC](https://grpc.io/). It uses specally-named caches to ensure different agents implementations can communicate with each other.

## Versioning

Versioning of the the Perper Fabric Protocol follows the [SemVer](https://semver.org/) version of Perper Fabric.

## Ignite caches

### Executions cache

The "executions" cache is a special cache of `ExecutionData` objects containing the following fields:
* (key), string: the Execution's ID.
* `instance`, string: the instance that is being called.
* `agent`, string: the agent that is being called.
* `delegate`, string: the delegate of the Execution; a string describing what "method" is being executed.
* `finished`, boolean: set to true to signal that the Execution has completed execution.
* `parameters`, object array, optional: arbitrary parameters passed to the call.
* `results`, object array, optional: arbitrary results returned by the call. Note that "null", an empty array, and an array of one "null" element can all be used to describe a null return value.
* `error`, string, optional: arbitrary error string of the call. Typically expected to be a short form of the exception that occured as exception-supporting languages would typically raise an exception containing this string.
  * If both `result` and `error` are set, implementations may opt to ignore the result and directly raise an error.

Since this cache has high read traffic, writing extra metadata to the ExecutionData object is discouraged.

### Stream listeners cache

The "stream-listeners" cache is a special cache of `StreamListener` objects containing the following fields:
* (key), string: the stream listener's ID.
* `stream`, string: the stream's ID.
* `position`, long: the index reached by the listener.

Using the "stream-listeners" cache is optional, but highly recommended, as it is what allows for ephemeral streams to work.

### Stream caches

Stream caches are caches named as `"{stream}"` keyed by long values, and containing arbitrary values. They are directly created by the stream creator using the Ignite Thin Client protocol.

### State caches

State caches are caches named as `"{instance}"` keyed by string values, and containing arbitrary values. They are directly created by the agent process using the Ignite Thin Client protocol's `GetOrCreateCache` operation.
Additionally, implementations may use `"{(instance) || (execution)}-{(state name)}"` for additional state caches linked to an agent or an Execution.

While we don't currently enforce a specific format for instance IDs, current implementations generate ids using `"{(agent)}-{UUID}"`.

### Standard objects

Usage of the standard objects types is currently not enforced; however, later versions of Perper Fabric may opt to track object references passed through the exchange of those object types between executions.

#### PerperAgent

`PerperAgent` objects describe a Perper agent instance and consist of the following fields:
* `agent`: The agent type in question.
* `instance`: The agent insance ID.

#### PerperStream

`PerperStream` objects describe a Perper stream and consist of the following fields:
* `stream`: The stream ID. (Hence, doubling as the name of the cache containing the stream's items)
* `startIndex`: The starting index for items in this stream. `-1` signifies "autodetect".
* `stride`: The stride (interger difference between keys) of this "packed" stream. `0` signifies a stream that is not packed.
* `localToData`: Whether the stream should be processed in a local-to-data manner.

Receivers of `PerperStream` objects may freely change the start index, stride or local to data values, so those should not be used for the purposes of security.

## GRPC protocol

The GRPC protocol is described in [`fabric.proto`](../proto/fabric.proto).

## Operations

* Executions
  * Create an Execution: Put a new `ExecutionData` in the `"executions"` cache with the respective values set and a newly-generated ID.
  * Cancel an Execution: Delete the `ExecutionData` with the respeciive ID from the `"executions"` cache.
  * Wait for an Execution to complete: Make an `ExecutionFinished` GRPC call -- it will complete when the Execution is finished.
  * Listen for Executions: Make an `Executions` GRPC call -- it will return a list of active executions, followed by an item with `startOfStream` set, followed by any changes to the list of active executions -- with `canceled` marking executions that are either finished or canceled, and thus no longer active.
  * Complete an Execution: Update the `ExecutionData` in the `"executions"`, setting `finished` to `true` and `result` and `error` to their respective values.
* Agents
  * Register an agent type: scaler-specific.
  * Start an Agent Instance:
    1. Create an Execution with ID set to the ID of the new Instance, `instance` set to the Agent Type, and `agent` set to `"Registry"`.
    2. Create an Execution calling `Start` on the new Instance, and wait for it to complete.
  * Stop an Agent Instance:
    1. Create an Execution calling `Stop` on the Instance to destoy, and wait for it to complete.
    2. Cancel the Execution with the ID set to the ID of the Instance to destoy.
* Streams
  * Create a Stream:
    1. If there is a linked Execution to the stream, create an Execution with ID set to the stream's ID.
    2. Create the stream's cache, optionally configuring indexing.
    3. If the stream is persistent, put a `StreamListener` in the `"stream-listeners"` cache for the stream with ID `"{stream}-persist"` and position set to `-(2**63)`.
    4. If the linked Execution needs an initial listener to start, put a `StreamListener` in the `"stream-listeners"` cache for the stream with ID `"{stream}-trigger"` and position set to `(2**63)-1`.
  * Wait for listeners for a Stream: Make a `ListenerAttached` GRPC call -- it will complete when the there is a listener for that stream.
  * Write an Stream Item: Write the new value in the stream's cache, with either a consequitive key (for a packed stream) or a key equal to the amount of 100-nanosecond ticks since Unix Epoch (for an unpacked stream).
  * Listen for Stream Items:
    1. Put a `StreamListener` in the `"stream-listeners"` cache with position set to `(2**63)-1`.
    2. Make a `StreamItems` GRPC call to receive an ordered list of the keys available in the stream's cache.
    3. Periodically update the position of the `StreamListener` to the key that is about to be processed.
* States
  * Access an Agent's State: Get or create the cache with name equal to the agent's ID.

### IDs

Currently, Fabric does not enforce any specific format for IDs, as long as they are unique. However, current implementations use the following formats for IDs:
* Executions (and Agent Instances and Streams linked to Executions) use `"{delegate}-{UUID}"`.
* Streams (when not linked to an Execution) use `"-{UUID}"`.
* Stream Listeners use ``"{(listening execution)}-{stream}-{(listener name) || UUID}"`, except for the special `"{stream}-persist"` and `"{stream}-trigger"` listeners.




