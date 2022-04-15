# .NET Usage Tutorial

In this usage tutorial, we are going to build a small demo app that streams a "Hello World" message. To this end, we are going to create 3 functions: a `Launcher` that starts a `HelloWorldGenerator` and passes the resulting stream to a `StreamPrinter`.

![Usage sample architecture](./images/usage-sample-app.drawio.png)

## Table of Contents

* [Prerequisites](#prerequisites)
* [Starting Perper Fabric](#starting-perper-fabric)
* [Step by step tutorial](#step-by-step-tutorial)
  * [Creating an agent](#creating-an-agent)
  * [Writing the Launcher](#writing-the-launcher)
  * [Adding and calling another function](#adding-and-calling-another-function)
  * [Adding a stream](#adding-a-stream)
  * [Adding another agent](#adding-another-agent)
* [Usage sample code](#usage-sample-code)
  * [Exploration ideas](#exploration-ideas)
* [Closing note](#closing-note)

## Prerequisites

* A cursory understanding of the [Concepts](./concepts.md)
* A [.NET Runtime and SDK](https://dotnet.microsoft.com/en-us/download) (at least version 5.0)
* Either [Docker](https://docs.docker.com/get-docker/) (/[Podman](https://podman.io/getting-started/installation)) or [Java JDK 11 or higher](https://openjdk.java.net/install/)

## Starting Perper Fabric

Before we begin, start an instance of Perper Fabric; this will allow us to run parts of the sample as we proceed.

```bash
$ docker run --rm -p 10800:10800 -p 40400:40400 obecto/perper-fabric:0.8.0 config/example.xml
```

<details> <summary>(Alternatively, if using a local Perper clone)</summary>

From the root of a clone of the Perper repository, run:

```bash
$ cd path/to/perper/fabric
fabric$ ./gradlew run --args="config/example.xml"
```

</details>

Note that in `0.8.0-beta1` you might want to occasionally restart Fabric by interrupting (Ctrl-C) the process and running it again, since starting our `Launcher` over and over will result in many `HelloWorldGenerator` / `StreamPrinter`-s running.

## Step by step tutorial

Note that if you feel like following the step-by-step tutorial and want to go directly for some off-roads experimentation, you can jump down to the [Exploration ideas](#exploration-ideas) at the end.

### Creating an agent

To create a new agent, create a new Console .NET project and add a reference to the [`Perper`](https://www.nuget.org/packages/Perper) NuGet package:

```bash
$ dotnet new console -o MyFirstAgent
$ cd MyFirstAgent
MyFirstAgent$ dotnet add package Perper
```

<details> <summary>(Alternatively, if using a local Perper clone)</summary>

Change the last command to:

```bash
MyFirstAgent$ dotnet add reference path/to/perper/agent/dotnet/src/Perper
```

</details>

Then, change the default `Program.cs` boilerplate to:

```c#
using Perper.Application;
await new PerperStartup().AddAssemblyHandlers("MyFirstAgent").WithDeployInit().RunAsync(default).ConfigureAwait(false);
```

Congratulations, you now have a Perper agent! An empty one, but an agent nontheless.

If you were to run the project right now, it would print something like the following before exiting:

```
APACHE_IGNITE_ENDPOINT: 127.0.0.1:10800
PERPER_FABRIC_ENDPOINT: http://127.0.0.1:40400
```

#### What just happened?

* The agent process's entrypoint, `Program.cs`, got called.
* In turn, it instanced a `PerperStartup`, a utility that lets us quickly scaffold Perper agents by providing a way to add handlers for the different delegates (methods) that may be called on the instances of that agent.
* It then called `AddAssemblyHandlers`, which went through the classes in the assembly's root namespace that have a `Run` or `RunAsync` method and added them as handlers for `"MyFirstAgent"` to the `PerperStartup`.
  * `AddAssemblyHandlers` can also use another assembly or look for classes filtered by another namespace. Alternatively, there is `AddClassHandlers` which would add all of a class's methods as handlers. Or, one could also use `AddHandler`/`AddInitHandler` to specify handlers manually. For now, we will stick to `AddAssemblyHandlers`, as it allows us to split our code more easily.
* Then it used `WithDeployInit` to signal that the init handlers should run when we run the agent _process_, as opposed to when we create an instance" of the agent; this function should go away by the time `0.8.0` is released, but for now it is the primary way for us to get some code executing initially.
* Finally, `PerperStartup`'s `RunAsync` method is called, and it printed some debug information before connecting to Fabric.
* At this point, we still haven't defined any non-init handlers, so our agent process had nothing to do and promptly exited... but we are going to fix in a monment.

### Writing the Launcher

Let's start implementing our agent by adding the launcher function, which would then set up the stream graph printing "Hello World". As we don't have anything for that launcher to call yet, it will just directly print "Hello World" for now.

`AddAssemblyHandlers` expects our launcher to be a class named `Init` somewhere under the `MyFirstAgent` namespace, so, add a new file called `Init.cs` with the following code:

```c#
using System;
using System.Threading.Tasks;
namespace MyFirstAgent // <- AddAssemblyHandlers is going to be looking in this namespace by default.
{
    public static class Init // <- The class name is used for the handler's name, "Init"
                            // Note that static is not required; non-static handler classes are automatically instanced
    {
        public static async Task RunAsync() // <- All classes with a Run or RunAsync method are turned into handlers
        {
            Console.WriteLine("Hello world from Init!");
            await Task.Delay(1); // <- A delay to avoid the warning about async method lacking await.
        }
    }
}
```

Now, if you run the project... you should see something like this:
```
...
Hello world from Init!
```

Yay!

#### What just happened?

* `AddAssemblyHandlers` found our `Init` class, and used `AddInitHandler` to register it to `PerperStartup`.
* `PerperStartup` called our `RunAsync` function as part of starting the process.
* The process exits as soon as our init handler is complete, since we have no non-init handlers that might keep it alive. Let's fix that!

### Adding and calling another function

Next, let's add the `StreamPrinter` function and call it. For the time being, we won't actually pass it a stream, but just showcase calling a function in Perper.

Create a `StreamPrinter.cs` file (similar to the `Init.cs` from before):

```c#
using System;
using System.Threading.Tasks;
namespace MyFirstAgent
{
    public static class StreamPrinter // <- Creates a handler for the "StreamPrinter" delegate
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Hello world from StreamPrinter!");
            await Task.Delay(1);
        }
    }
}
```

Then modify `Init.cs` so it would call the `StreamPrinter`:

```c#
using System;
using System.Threading.Tasks;
using Perper.Extensions;
namespace MyFirstAgent
{
    public static class Init
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Hello world from Init!");
            await PerperContext.CallAsync("StreamPrinter"); // <- Call the "StreamPrinter" handler on the same agent
        }
    }
}
```

And now, running the project should result in something like this:
```
...
Hello world from Init
Hello world from StreamPrinter!
```

#### What just happened?

* `AddAssemblyHandlers` found our `StreamPrinter` class as well, using `AddHandler` to register it.
* `PerperStartup` called our `Init` handler. In parallel, it started listening for executions for `StreamPrinter`.
* Our `Init` handler used `PerperContext.CallAsync` to create a new execution for `StreamPrinter`.
* `PerperStartup` picked up the execution for `StreamPrinter` and called our handler.
* Our `StreamPrinter` handler printed a hello world message. Except... it's still not comming from a stream.

### Adding a stream

To meet our initial objective of receiving the hello world message over a stream, let's add the `HelloWorldGenerator` stream.

Create a `HelloWorldGenerator.cs` file, with the following contents:

```c#
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace MyFirstAgent
{
    public static class HelloWorldGenerator // <- Creates a handler for the "HelloWorldGenerator" delegate
    {
        public static async IAsyncEnumerable<char> RunAsync() // The IAsyncEnumerable return value makes this a stream handler
        {
            Console.WriteLine("Hello world from HelloWorldGenerator!");
            foreach (var ch in "Hello world through stream!")
            {
                yield return ch; // (see also https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/async-return-types#async-streams-with-iasyncenumerablet)
                await Task.Delay(100); // <- By delaying the time between characters, we'd be able to actually see them pop on screen one-by-one
            }
        }
    }
}
```

Then modify `Init.cs` to call the new `HelloWorldGenerator` and pass the resulting stream to `StreamPrinter`:

```c#
using System;
using System.Threading.Tasks;
using Perper.Model;
using Perper.Extensions;
namespace MyFirstAgent
{
    public static class Init
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Hello world from Init!");
            PerperStream stream = await PerperContext
                .Stream("HelloWorldGenerator")
                .StartAsync(); // <- Create an object representing our stream
            await PerperContext.CallAsync("StreamPrinter", stream); // <- Pass the PerperStream to StreamPrinter
        }
    }
}
```

And finally, modify `StreamPrinter.cs` to receive and process the stream:

```c#
using System;
using System.Threading.Tasks;
using Perper.Model;
using Perper.Extensions;
namespace MyFirstAgent
{
    public static class StreamPrinter
    {
        public static async Task RunAsync(PerperStream streamToPrint) // <- Receive the passed PerperStream as a parameter
        {
            Console.WriteLine("Hello world from StreamPrinter!");
            await foreach (var ch in streamToPrint.EnumerateAsync<char>())
            {
                Console.Write(ch);
            }
        }
    }
}
```

And now, running the project should finally result in something looking like this:
```
...
Hello world from Init
Hello world from StreamPrinter!
Hello world from HelloWorldGenerator!
Hello World through stream!
```

#### What just happened?

* `PerperStartup` called our `Init` handler.
* Our `Init` handler used `PerperContext.Stream` to create a new execution for `HelloWorldGenerator` and get a `PerperStream` representing the resulting stream.
* `PerperStartup` called our `HelloWorldGenerator` handler, only to notice it returns an `IAsyncEnumerable`. It then started waiting for a listener for that stream.
  * It is also possible to make a stream start without waiting for a listener by using `StreamBuilder.Action`.
* Our `Init` handler used `PerperContext.CallAsync` to create a new execution for `StreamPrinter`, and passed it the `PerperStream` object.
* Our `StreamPrinter` handler used `PerperStreamExtensions.EnumerateAsync` to start listening for items added to the stream.
* Our `HelloWorldGenerator` handler started executing, writing the hello world text to the stream one character at a time.
* Our `StreamPrinter` handler started receiving and printing the hello world message. Success!

Note: This is the first time you might need to restart Fabric between runs. Since stream completion is [not yet implemented](https://github.com/obecto/perper/issues/68), `StreamPrinter` never completes, and restarting the agent process would slowly result in many leftover `StreamPrinter`-s accumulating.

### Adding another agent

Our code so far achieves the stated goal of streaming a hello world text through Perper. However, to demonstrate more of Perper's functionallity and to, uh, to promote code reusability, we can move `StreamPrinter` to it's own seperate agent.

To this end, make another agent project called `StreamPrinterAgent`, adding a reference to Perper like [before](#creating-an-agent).

Then, change it's `Program.cs` to:

```c#
using Perper.Application;
await new PerperStartup().AddAssemblyHandlers("StreamPrinterAgent").RunAsync(default).ConfigureAwait(false);
```

(We don't need `WithDeployInit`, as we are not going to need `Init` for this agent.)

Then, move `StreamPrinter.cs` to the new project, changing it's namespace:

```c#
using System;
using System.Threading.Tasks;
using Perper.Model;
using Perper.Extensions;
namespace StreamPrinterAgent // <- AddAssemblyHandlers likes to have namespaces named just like the assembly/project
{
    public static class StreamPrinter
    {
        public static async Task RunAsync(PerperStream streamToPrint)
        {
            Console.WriteLine("Hello world from StreamPrinterAgent's StreamPrinter!");
            await foreach (var ch in streamToPrint.EnumerateAsync<char>())
            {
                Console.Write(ch);
            }
        }
    }
}
```

And finally, modify the `Init.cs` in the original project.

```c#
using System;
using System.Threading.Tasks;
using Perper.Model;
using Perper.Extensions;
namespace MyFirstAgent
{
    public static class Init
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Hello world from Init!");
            PerperStream stream = await PerperContext
                .Stream("HelloWorldGenerator")
                .StartAsync();
            PerperAgent agent = await PerperContext.StartAgentAsync("StreamPrinterAgent"); // <- Create an object representing our agent
            await agent.CallAsync("StreamPrinter", stream); // <- Call the agent's StreamPrinter
        }
    }
}
```

Now, if you run both projects, in seperate terminals, you should see that `Init` and `HelloWorldGenerator` run in the first agent, while `StreamPrinter` runs in the other agent.

```
... (first terminal)
Hello world from Init
Hello world from HelloWorldGenerator!
... (second terminal)
Hello world from StreamPrinterAgent's StreamPrinter!
Hello World through stream!
```

#### What just happened?

* The first agent's `PerperStartup` called our `Init` handler.
* As before, our `Init` handler used `PerperContext.Stream` to get a `PerperStream` for the `HelloWorldGenerator` stream.
* Our `Init` handler used `PerperContext.StartAgent` to create a new insance of the for `StreamPrinterAgent` agent.
* The stream printer's `PerperStartup` then would have called our `Start` handler, had we provided one.
* Our `Init` handler used `PerperContext.CallAsync` to create a new execution for `StreamPrinter`, and passed it the `PerperStream` object.
* The stream printer's `PerperStartup` picked the execution up, and started the `StreamPrinter` handler.
* As before, `StreamPrinter` subscribed to `HelloWorldGenerator`, and the hello world text started flowing across, one character at a time.

## Usage sample code

You can find the whole code of the usage sample created as part of this tutorial in [`samples/dotnet/MyFirstAgent`](../samples/dotnet/MyFirstAgent) and [`samples/dotnet/StreamPrinterAgent`](../samples/dotnet/StreamPrinterAgent).

### Exploration ideas

If you feel like playing around with the sample further, here is a list of things you could try. Do note that restarting Fabric might be needed to clear out stale executions.

* Try running the agents without Fabric. Or try running just one of the agents / running them in different order.
* Try changing the `"StreamPrinterAgent"` string in both `StreamPrinterAgent/Program.cs` and `MyFirstAgent/Init.cs`. What happens on mismatch?
* Try changing `HelloWorldGenerator`'s return type. `PerperStream` objects are untyped, so it is possible to enumerate them as the wrong type -- what happens then?
* Try calling `StreamPrinter` multiple times in parallel with the same stream. (Hint: you might need to use `Task.WhenAll`)
* Make `HelloWorldGenerator` accept the string to print as a parameter. Does it work with default arguments?
* Make a `GetHello` function which returns a `string` (or `Task<string>`) with the hello world text to print, then call that instead of hardcoding the string. (Hint: use `CallAsync<T>` to get the result value.)
* Refactor `HelloWorldGenerator` into its own agent. (Hint: unlike `CallAsync`, `Stream` does not work with a `PerperAgent` object. Use an extra function.)
* Store the `PerperStream` object for `HelloWorldGenerator` inside of `PerperState` instead of a variable.
* Make one of the `Program.cs`-s use `AddInitHandler<T>`/`AddHandler<T>` instead of `AddAssemblyHandlers`.
* Make one of the `Program.cs`-s use `AddClassHandlers<T>` instead of `AddAssemblyHandlers`. (Hint: move all of the functions in that project to one class, and rename the methods to `{Delegate}Async` instead of `RunAsync`)
* Make a new Perper project which includes the other projects, and use multiple invocations of `AddAssemblyHandlers` to host all the agents in that one process.

## Closing note

Thank you for following this tutorial; I hope it was useful in figuring out the basics of using Perper through C#! If you want to read more in-depth documentation, feel free to check out the [Architecture](./arciitecture.md) page. Or, if you just want to dive into using the framework, feel free to check out the Reference (currently nonexistent, but the classes in the [`Perper.Extensions`](../agent/dotnet/src/Perper/Extensions/) namespace should be a good place to start)!
