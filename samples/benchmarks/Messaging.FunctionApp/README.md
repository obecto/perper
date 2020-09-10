# Messaging Benchmark

This is benchmark testing the performance of sending/receiving messages in Perper sent at random between a group of communicating "nodes".

Every node outputs the messages it wants to send to its output stream. Other nodes then filter that stream, locating and processing messages directed at them.
For sake of completeness, there are multiple strategies implemented for filtering the output streams of other nodes:

| Method | Description | Performance (i7-4790K, 8 core) |
| - | - |
| Enumeration | Use IAsyncEnumerable to receive messages, filter using an `if` | ~15000 operations/second, ~1500 reads/second for 10 nodes |
| Filtering | Use IAsyncEnumerable to receive messages, filter using IPerperStream.Filter | ~13000 operations/second and reads/second for 10 nodes |
| Querying | Wait for all messages to be written out, filter using PerperStreamContext.Query | ~500000 operations/second and reads/second for 10 nodes |

## Settings

The benchmark can be configured by changing the settings in the `settings` region of `Launcher.cs`.

| Setting | Description |
| - | - |
| MessageCount | The amount of messages to send before stopping. Useful in order to be able to measure read speed when not writing. Set to `-1` for an unlimited amount of messages. |
| NodeCount | The amount of nodes sending messages to each other. Lower numbers might be unable to fill up the pipe, while larger numbers need to filter out a larger percentage of messages. |
| EnumerateMessages | Use the Enumeration method for receiving messages. |
| FilterMessages | Use the Filtering method for receiving messages. |
| QueryMessages | Use the Querying method for receiving messages. |

Note that if multiple methods of receiving messages are on, the messages would just be processed multiple times.

## Output format

The benchmark outputs a table consisiting of multiple counters measuring the amount of messages transfered per second. A new row is appended to the table every second.

| Counter | Description |
| - | - |
| Sent | The amount of messages written to the outputs of all of the nodes. |
| Enumerated | The amount of messages received by the Enumeration method. |
| Filtered | The amount of messages received by the Filtering method. |
| Queried | The amount of messages received by the Querying method. Note that querying runs only once after all messages have been sent. |
| Processed | The amount of messages processed either by sending or receiving. |

## Code structure

The code has one entrypoint, the `Launcher` class. It creates `NodeCount` instances of `Node`, links them together in a `Peering`, starts the nodes, and finally uses a `"Dummy"` stream to engage the whole system. Afterwards, it would start a loop which prints out the output table, making sure to stop printing after all messages have been sent/received.

The `Node` stream starts up asynchronous workers for each of the receiving methods as well as a worker for sending out messages to other nodes. `Message`s are sent to a random node every time, mimicing real-world usage in [Apocryph](https://github.com/comrade-coop/apocryph/).

The `Peering` stream is pretty basic, just rebinding its output to that of all the `Node`s.

`Node`s communicate back to the `Launcher` stream using static instances of the `Stat` class. That way, statistical information does not have to propagate through Perper, and thus interfere with the benchmark. The `Stat` class is designed for multithreaded usage, and contains utilities for computing changes in the total value, necessary for displaying per-second values in the `Launcher`.
