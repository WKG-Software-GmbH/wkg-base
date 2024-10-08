# `Wkg` Documentation

`Wkg` is a company-internal library providing reusable components for the development of any .NET project.

- [`Wkg` Documentation](#wkg-documentation)
  - [Components](#components)
    - [`Wkg.Collections` Namespace](#wkgcollections-namespace)
      - [`CyclicQueue` and `CyclicStack`](#cyclicqueue-and-cyclicstack)
      - [VolatileArray](#volatilearray)
      - [ResizableBuffer](#resizablebuffer)
      - [Examples](#examples)
    - [`Wkg.Data.Validation` Namespace](#wkgdatavalidation-namespace)
      - [Examples](#examples-1)
    - [`Wkg.IO` Namespace](#wkgio-namespace)
      - [`CachingStream` Class](#cachingstream-class)
    - [`Wkg.Extensions` Namespace](#wkgextensions-namespace)
      - [`GuidExtensions` Class](#guidextensions-class)
        - [Examples](#examples-2)
      - [`NullableValueTypeExtensions` Class](#nullablevaluetypeextensions-class)
      - [`ObjectExtensions` Class](#objectextensions-class)
    - [`Wkg.Logging` Namespace](#wkglogging-namespace)
      - [Interface Overview](#interface-overview)
      - [Default `Logger` Implementation](#default-logger-implementation)
      - [Configuration](#configuration)
      - [Built-In `ILogEntryGenerator` Implementations](#built-in-ilogentrygenerator-implementations)
      - [Logging Levels](#logging-levels)
      - [Extending Logging](#extending-logging)
    - [`Wkg.Unmanaged` Namespace](#wkgunmanaged-namespace)
      - [Memory Management](#memory-management)
        - [The `MemoryManager` Class](#the-memorymanager-class)
          - [Configuration](#configuration-1)
          - [Allocation Tracking](#allocation-tracking)
          - [MemoryManager APIs](#memorymanager-apis)
      - [`TypeReinterpreter` Class](#typereinterpreter-class)
    - [`Wkg.Reflection` Namespace](#wkgreflection-namespace)
    - [`Wkg.Web` Namespace](#wkgweb-namespace)
      - [Simple URI Manipulation](#simple-uri-manipulation)
    - [`Wkg.SyntacticSugar` Class](#wkgsyntacticsugar-class)
      - [`Pass()` Method](#pass-method)
        - [Examples](#examples-3)
      - [Switch Expression Helpers](#switch-expression-helpers)
      - [The Double-Discard Object](#the-double-discard-object)

> :warning: **Warning**
> This documentation is a work in progress and may not be complete or up-to-date. For the most accurate and up-to-date information, please refer to the source code and the XML documentation comments.

## Components

### `Wkg.Collections` Namespace

The `Collections` namespace provides both general purpose as well as very specific and performance-oriented generic collections to be used internally or by dependant applications.

#### `CyclicQueue` and `CyclicStack`

The `CyclicQueue<T>` and `CyclicStack<T>` are FIFO and LIFO data structures implemented on top of a ring buffer (that's what the internal `RingBufferPointer<T>` is used for btw).

The inherent nature of the underlying ring buffer allows these data structures to override the oldest elements if a given threshold is reached and more elements are added. Other than that they have the same functionality as a normal `Stack` or `Queue`. Use-cases may include instances where only the most recent elements are of interest, e.g. for caching, logging, or other purposes.

#### VolatileArray

As its name implies the volatile array can be used in multithreaded environments where multiple concurrent threads have to be able access the same array. Note that this doesn't make it thread-safe on its own. All it does is disabling certain memory optimizations and re-orderings during volatile reads and writes using memory barriers and therefore doesn't rule out the potential for race conditions (for more info see [`volatile` (C# Reference)](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile)).

#### ResizableBuffer

The `unsafe struct ResizableBuffer<T> : IDisposable where T : unmanaged` is a highly performant collection operating on a continuous block of **unmanaged** memory. This enables us to increase the performance of our apps by writing (managed) allocation free code in places where `stackalloc` becomes less efficient for large allocation. Especially when handling chunked network streams this is an advantage as the only other option would be to use managed arrays that will have to be garbage-collected afterwards. The `ResizableBuffer<T>` supports both indexing as well as the `Add(Span<T>)` operation to append the contents of a `Span<T>` to the end of the used space in the buffer.
When the size of the allocated memory is exceeded a new block will automatically be allocated using the current [`MemoryManager.Realloc(void*, int)`](#memory-manager) implementation. 

> :warning: **Warning** 
> As `ResizableBuffer<T>` is a struct ensure not to create unintentional copies as this may lead to dangling pointers if either the original or the copy is disposed. Therefore instances **must** be passed by reference (`ref`, `in`) or not at all.

> :x: **Caution**
> Ensure instances are properly disposed before falling out of scope. Otherwise unmanaged memory will be leaked.

#### Examples

The following example demonstrates how to use the `ResizableBuffer<T>` to read a network stream chunk by chunk and append the contents to the buffer:

```csharp
private static string ReadResponseStreamHelper(Stream responseStream)
{
	const int bufferLength = 512;
	Span<byte> streamBuffer = stackalloc byte[bufferLength];
	using ResizableBuffer<byte> buffer = new(bufferLength);
	int bytesRead;
	while ((bytesRead = responseStream.Read(streamBuffer)) != 0)
	{
		buffer.Add(streamBuffer, 0, bytesRead);
	}
	return Encoding.UTF8.GetString(buffer.AsSpan());
}
```

Using a stack allocated `Span<byte>` allows reading chunk by chunk from the network stream appending each chunk to the end of the `ResizableBuffer<byte>`. After all bytes have been read into unmanaged memory another `Span<byte>` is created using the underlying block of unmanaged memory. Finally the span is passed to `Encoding.UTF8.GetString()` which creates a managed C# string in one go without additional allocations. Using a `using` declaration for the `ResizableBuffer<byte>` ensures that all allocated resources will be freed after they fall out of scope.

### `Wkg.Data.Validation` Namespace

The `Data.Validation` namespace provides an API to be used for data and input validation. The core component of this namespace is the static `DataValidationService` class which exposes common regex patterns for input validation through a set of static validation methods. The regex patterns used for validation are well-known and widely used and were mostly adopted from the [.NET Framework 4.8 source code](https://referencesource.microsoft.com/#System.ComponentModel.DataAnnotations/DataAnnotations).

#### Examples

```csharp
// Validate a string to be a valid email address
bool isValidEmail = DataValidationService.IsEmailAddress(textBox.Text);
if (!isValidEmail)
{
    // Do something
}
```

### `Wkg.IO` Namespace

The `IO` namespace provides utilities for working with streams and files.

#### `CachingStream` Class

The `CachingStream` class is a read-only stream that caches the data read from the source stream, allowing random access to non-seekable source streams. This functionality may be required for use cases such as server-side data validation of uploaded files, where the provided stream must match a specific file signature or structure before being accepted and written to disk. As such, the `CachingStream` class can be used as an efficient way to buffer and process data without having to load the entire input stream into memory, but rather only the parts that are actually needed. Note that in the worst case scenario the entire stream will be read into memory, if read access to the end of the stream is required.

```csharp
IFormFile formFile = Request.Form.Files[0];
await using Stream source = formFile.OpenReadStream();
await using CachingStream buffer = new(source, leaveOpen: true);
// do some validation on the buffer, e.g. check the file signature, structure, etc.
...
// and if everything is fine write the buffer to disk
await using Stream destination = File.OpenWrite("output");
buffer.Seek(0, SeekOrigin.Begin);
await buffer.CopyToAsync(destination);
```


### `Wkg.Extensions` Namespace

The `Extensions` namespace provides a set of common extension methods for various types.

#### `GuidExtensions` Class

A common issue with .NET's `Guid` type is that its `ToString()` method returns a mixed-endian string representation of the `Guid` which is not compatible with the string representation used by most databases. In order to mitigate this issue the `GuidExtensions` class provides the performance-optimized `ToStringBigEndian()` extension method which returns a big-endian string representation of the `Guid` that can be used for database operations.

> :link: **See also** 
> If true UUIDs are required, consider using the `Uuid` structure provided through the `Wkg.EntityFrameworkCore` package.

##### Examples

The following example aims to demonstrate the difference between the default `Guid.ToString()` and the `GuidExtensions.ToStringBigEndian()` extension method:

```csharp
Guid guid = Guid.NewGuid();

// 0f8fad5b-d9cb-469f-a165-70867728950e
Console.WriteLine(guid.ToString());

// 5b0df80f-cbd9-9f46-a165-70867728950e
Console.WriteLine(guid.ToStringBigEndian());
```

#### `NullableValueTypeExtensions` Class

Determining whether a nullable value type has a non-null, non-default value is a common task in C#, but as of C# 11.0 there is no short and concise way to do so. The result often is a somewhat long and inconsise expression like the following:

```csharp
Guid? guid = GetMyGuid();

if (guid.HasValue && guid.Value != default)
{
    // Do something
}
```

The `NullableValueTypeExtensions` class provides the `IsNullOrDefault()` and `HasDefinedValue()` extension methods which can be used to simplify the above expression to the following:

```csharp
Guid? guid = GetMyGuid();

if (guid.HasDefinedValue())
{
    // Do something
}
```

It must be noted that the `HasDefinedValue()` extension method is not a replacement for the `HasValue` property and that in some cases its use may be semantically incorrect. For example the following expression will evaluate to `false`:

```csharp
int? myInt = 0;

if (myInt.HasDefinedValue())
{
    // Do something (will not be executed)
}
```

Therefore usage of these methods in conjunction with numeric types is discouraged, as it may be misleading.

#### `ObjectExtensions` Class

A common task when working with interfaces with explicit implementations is to cast a concrete implementation to the interface type to access the interface members. This usually results in an unweildy expression like the following:

```csharp
((IMyInterface)myObject).MyInterfaceMethod();
```

The `ObjectExtensions` class provides the `To<T>()` extension method which can be used to simplify the above expression to the following:

```csharp
myObject.To<IMyInterface>().MyInterfaceMethod();
```

A similar extension method is provided by the `ObjectExtensions` class for soft-casting an object to a specific type. This is useful when the type of an object is not known at compile-time and must be determined at runtime. The `As<T>()` extension method can be used to simplify the following expression:

```csharp
(myObject as IMyInterface)?.MyInterfaceMethod();
```

to the following:

```csharp
myObject.As<IMyInterface>()?.MyInterfaceMethod();
```

Another inconvenience when working with chained method calls is having to wrap null-coalescing expressions in parentheses. For example, the following expression:

```csharp
(myObject.As<MyObject>() ?? new MyObject()).MyInterfaceMethod();
```

can be simplified using the `Coalesce<T>()` extension method to the following:

```csharp
myObject.As<MyObject>().Coalesce(new MyObject()).MyInterfaceMethod();
```

### `Wkg.Logging` Namespace

The `Logging` namespace contains utilities used for debugging and logging during development and in production.

#### Interface Overview

| Interface | Description |
| --- | --- |
`ILog` | A collection of static methods representing the global entry point for logging messages and events to a configured `ILogger`.
`ILogger` | Represents a logger that can be used to log messages at different `LogLevels` and events. A logger can be configured to log to one or more `ILogSink`s and there can be multiple loggers used in parallel. One of these loggers may be used by an implementation of `ILog` to act as a global entry point for logging messages and events.
`IProxyLogger` | An `ILogger` exposing internal methods to allow for call site information to manually be passed to the logger. This is useful for custom loggers that want to use their own implementation to gather call site information.
`ILogSink` | Represents a sink that can be used to log messages and events to a specific target. A sink may write to a file, the console, the debug output, a remote server or any other target. A sink may be used by one or more `ILogger`s.
`ILogWriter` | An `ILogWriter` specifies *how* a message or event is written to the `ILogSink`s. It may write the message directly to the sink, or schedule it for writing in the background on a different thread. It may also write every message as soon as it is received or opt to buffer messages and write them in batches. Some common implementations of `ILogWriter` are provided via the static `LogWriter` class.
`ILogEntryGenerator` | An `ILogEntryGenerator` specifies how the data written to the sinks is formatted. It may format the data as plain text, JSON, XML or any other format. It may also enumerate additional data to be written to the sinks, such as the current timestamp, the thread ID, the process ID, and can even use reflection and call stack unwinding to gather information about the caller.

#### Default `Logger` Implementation

The default implementation of `ILogger` is the `Logger` class. It is the most simple implementation of `ILogger` and is capable of logging messages and events to one or more `ILogSink`s. Custom implementations of `ILogger` may be used to add additional functionality, such as filtering messages and events based on their `LogLevel`.

#### Configuration

Logging can be configured and customized using the `LoggerConfiguration` builder class. The following example demonstrates how to configure a logger to log to the console and the debug output:

```csharp
ILogger logger = Logger.Create(LoggerConfiguration.Create()
    // write to the console
    .AddSink<ConsoleSink>()
    // write messages directly to the sinks, potentially blocking the current thread
    .UseDefaultLogWriter(LogWriter.Blocking)
    // a simple AOT-friendy log entry generator that adds some useful extra information
    .UseEntryGenerator(AotLogEntryGenerator.Create));

logger.Log("Hello World!", LogLevel.Info);
// 2023-05-30 14:35:42.185 (UTC) Info on Thread_0x1 --> Output: 'Hello World!';

// or register the logger as the global logger using the default ILog implementation "Log"
Log.UseLogger(logger);
Log.WriteInfo("Hello World!");
// 2023-05-30 14:35:42.185 (UTC) Info on Thread_0x1 --> Output: 'Hello World!';
```

A more complex example that demonstrates how to configure a logger to log to the debug console, a file, the console using colors to highlight different log levels, and how to use a log entry generator more suitable for debugging:

```csharp
ILogger logger = Logger.Create(LoggerConfiguration.Create()
    .AddSink<ColoredConsoleSink>()                      // write to the console using colors
    .AddSink<DebugSink>()                               // write to the debug output
    .UseLogFile("log.txt")                              // write to a file
        .WithMaxFileSize(1024 * 1024 * 10)              // truncate after 10 MB
        .BuildToConfig()
    .UseDefaultLogWriter(LogWriter.Background)          // write to sinks in the background
    .UseEntryGenerator(TracingLogEntryGenerator.Create) // enumerate additional data from the call stack
    .RegisterMainThread(Thread.CurrentThread));         // the configured log entry generator adds "(MAIN THREAD)" for this thread

logger.Log("Hello World!", LogLevel.Info);
// 2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (Program::Main(String[])) ==> Output: 'Hello World!'
```

Custom `ILogSink`s, `ILogWriter`s and `ILogEntryGenerator`s can be registered using the `AddSink<T>()`, `UseDefaultLogWriter(ILogWriter)` and `UseEntryGenerator<T>()` methods respectively. The `UseLogFile()` method can be used to configure a file sink and the `RegisterMainThread()` method can be used to register the main thread to be used by the configured log entry generator.

#### Built-In `ILogEntryGenerator` Implementations

The `Logging.Generators` namespace provides a set of built-in `ILogEntryGenerator` implementations that can be used to gather different kinds of information about the caller, formatting the log entries in different ways. The following built-in implementations are provided:

- `AotLogEntryGenerator` - The default log entry generator that is used when no other log entry generator is specified. It is AOT-friendly and formats log entries in a minimalistic way:
    ```text
    2023-05-30 14:35:42.185 (UTC) Info on Thread_0x1 (Main Thread) --> Output: 'This is a log message';
    2023-05-30 14:35:42.185 (UTC) ERROR: NullReferenceException on Thread_0x1 (Main Thread) --> info: 'while trying to do a thing' original: 'Object reference not set to an instance of an object.' at:
      StackTrace line 1
    2023-05-30 14:35:42.185 (UTC) Event on Thread_0x1 (Main Thread) --> (MyAssembly) (MyClass::MyButtonInstance) ==> OnClick(MyEventType: eventArgs)
    ```
- `DetailedAotLogEntryGenerator` - An AOT-compatible log entry generator that uses compiler-evaluated [caller information attributes](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information) to gather additional information about the location the log entry originated from. It is more detailed than the default `AotLogEntryGenerator`:
    ```text
    2023-05-31 14:14:24.626 (UTC) [Info->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> Output: 'This is a log message'
    2023-05-31 14:14:24.626 (UTC) [ERROR->Thread_0x1(MAIN THREAD)] (MyClass.cs:L240->MyMethod) ==> [NullReferenceException] info: 'while trying to do a thing' original: 'Object reference not set to an instance of an object.' at:
      StackTrace line 1
    2023-05-31 14:14:24.626 (UTC) [Event->Thread_0x1(MAIN THREAD)] (MyClass.cs:L1337->MyMethod) ==> MyAssembly::MyClass::MyButtonInstance::OnClick(MyEventType: eventArgs)
    ```
- `BalancedLogEntryGenerator` - A production-ready log entry generator implementation balancing runtime reflection requirements through caching with detailed log entries enumerated on compile time. It extends the `DetailedAotLogEntryGenerator` through the dynamically determined assembly name of the caller. Reflection is used sparingly during the first call to the generator from each call site, making it suitable in production scenarios that don't rely on AOT compilation.
    ```text
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> Output: 'This is a log message'
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [ERROR->Thread_0x1(MAIN THREAD)] (MyClass.cs:L240->MyMethod) ==> [NullReferenceException] info: 'while trying to do a thing' original: 'Object reference not set to an instance of an object.' at:
      StackTrace line 1
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [Event->Thread_0x1(MAIN THREAD)] (MyClass.cs:L1337->MyMethod) ==> MyAssembly::MyClass::MyButtonInstance::OnClick(MyEventType: eventArgs)
    ```
    > :warning: **Warning**
    > Assembly information is cached after the first call to the generator from each call site. As the call site is identified through the compiler-provided attribute information (file name and member name), hash collisions may occur for identical call sites in different assemblies, causing potentially incorrect assembly information to be written in these edge cases.
- `TracingLogEntryGenerator` - A diagnostic log entry generator that liberally uses reflection to gather additional information about the caller. It is neither AOT-compatible nor resource-efficient and should only be used in development or debugging scenarios.
    ```text
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[], Boolean)) ==> Output: 'This is a log message'
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [ERROR->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[], Boolean)) ==> [NullReferenceException] info: 'while trying to do a thing' original: 'Object reference not set to an instance of an object.' at: 
       StackTrace line 1
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (MyClass::ByButton) ==> OnClick(MyEventType: { "Property": "JSON serialized model", "foo": 1234 })
    ```
- `TracingLogEntryGeneratorWithParamNames` - The most detailed log entry generator that uses reflection to gather additional information about the caller. It is neither AOT-compatible nor resource-efficient and should only be used in development or debugging scenarios. It extends the `TracingLogEntryGenerator` by including parameter names in the log entries.
    ```text
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[] args, Boolean myFlag)) ==> Output: 'This is a log message'
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [ERROR->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[] args, Boolean myFlag)) ==> [NullReferenceException] info: 'while trying to do a thing' original: 'Object reference not set to an instance of an object.' at: 
       StackTrace line 1
    2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (MyClass::ByButton) ==> OnClick(MyEventType: { "Property": "JSON serialized model", "foo": 1234 })
    ```

#### Logging Levels

The `LogLevel` enum provides different levels of logging with the following recommended use cases:

0. `Diagnostic` :mag: - _Function-Level Debugging and Unit Intrinsics_: 
   The most verbose logging level. Use this level for internal system events that may be useful for debugging of application intrinsics. Relevance of these events is often limited to the immediate scope of the call site, and is likely not of interest outside of the immediate method or class.
1. `Debug` :bug: - _Component-Level Debugging and Integration Intrinsics_: 
   Use this level for debugging information. This level is used for debugging information that may be useful to developers. This level is typically used for logging of debugging information that is relevant for debugging the integration of different components of the application. As such, no highly specific component intrinsics should be logged at this level.
2. `Event` :point_up_2: - _Input Event Logging_: 
   Use this level for logging of events that are relevant to the overall program flow. Event-level log entries often correspond to external events or user activity, such as button clicks, form submissions, or other user interactions that may be relevant to tracing back the user's actions in the application in post-mortem analysis. In multi-user applications, event-level log entries are often exchanged with `Diagnostic` or `Debug` log entries to reduce the amount of log data generated. `Event` log entries may also be used to log non-interactive events in the application, as long as they are relevant to the overall program flow and do not qualify as `Diagnostic` application intrinsics.
3. `Info` :information_source: - _Informational Logging with Global Relevance_: 
   Use this level for logging of informational messages. Informational log entries are used to provide information about the application's state, configuration, or other relevant information that may be useful outside of debugging scenarios. Entries of this level often include initialization messages, the loaded configurations, and other information that may be useful for understanding the application's behavior. The desciminating factor between `Info` and and lower log entries is that `Info` log entries have a higher relavance to the system and should not require intricate knowledge of the application's flow or structure to be understood.
4. `Warning` :warning: - _Service is Degraded or Endangered_: 
   Use this level for logging of warning messages. Log messages of the warning level indicate that an unexpected condition has occurred that may not be critical to the application's operation, but may require attention. Warning messages _should not occur_ during normal operation of the application, and indicate that the application has entered a state that may be undesirable or that may lead to unexpected behavior. Warning messages are often used to indicate that a fallback mechanism has been used, that a deprecated feature has been used, or that a non-critical error has occurred.
5. `Error` :x: - _Functionality is Unavailable_: 
   Use this level for logging of unhandled exceptions, errors, and other critical messages. Error messages indicate that a critical error has occurred that may prevent the application from functioning correctly. Messages of this level indicate a need for triage and may require immediate attention. Alerting and monitoring systems should be configured accordingly and integrated through `ILogSink` implementations.
6. `Fatal` :skull: - _Application is Unusable_: 
   Fatal messages indicate that the application has entered a state that is unrecoverable, often logged right before an undesired termination of the application with a non-zero exit code. Ensure to use `LogWriter.Blocking` to prolong the application's lifetime to allow for the log entry to be written to the sink before its untimely demise.
7. `System` :gear: - _Global, Unconditional Logging_: 
   System messages ignore the configured log level and are always logged. Use this level for logging of system messages that are always relevant, such as startup and shutdown messages, or messages that are always relevant, such as the application's version number or version control information of the application's components. For obvious reasons, use this level sparingly.

#### Extending Logging

For the most part adding a new component which defines custom behavior is as simple as implementing the appropriate interface and registering it using the appropriate method. However, in some cases it may be necessary to extend the logging system itself. For example, to acommodate for more extensive sink configuration, the system could be extended to allow for sub-builders to be used to configure sinks, similar to the `UseLogFile()` method, but maybe in a more generic way. To do this, a new 

```csharp
TSinkBuilder ConfigureSink<TSink, TSinkBuilder>() 
    where TSink : class, ILogSink 
    where TSinkBuilder : class, ILogSinkBuilder<TSink, TSinkBuilder>;
```

method could be added with new interfaces similar to the following:
    
```csharp
// the actual builder interface that would be implemented and extended by the sink builder classes
public interface ILogSinkBuilder<TSink> where TSink : ILogSink
{
    LoggerConfiguration BuildAndAdd();
}

// a static abstract factory interface that would be implemented by the concrete sink builder classes
public interface ILogSinkBuilder<TSink, TSinkBuilder> 
    where TSink : class, ILogSink 
    where TSinkBuilder : class, ILogSinkBuilder<TSink, TSinkBuilder>
{
    static abstract TSinkBuilder CreateBuilder(LoggerConfiguration configuration);
}
```

> :pray: **Feature Request**
> Feel free to open merge requests :slightly_smiling_face:

### `Wkg.Unmanaged` Namespace 

The `Unmanaged` namespace provides easy access to unmanaged memory and exposes `Malloc()`, `Calloc<T>()`, `ReAlloc()` and `Free()` from `libc` while also providing optional allocation tracking to prevent memory leaks in production code.

#### Memory Management

> :information_source: **Note**
> This namespace is ***heavily*** influenced by the [PrySec Memory Management implementation](https://github.com/PrySec/PrySec/tree/master/PrySec.Core/Memory/MemoryManagement) I wrote some time ago (it's basically a fork with some minor changes). Future versions of this namespace may benefit from occasional synchronization with the PrySec implementation.

##### The `MemoryManager` Class

The `MemoryManager` class represents the core component of the `Unmanaged` namespace and is used for perfromance oriented unmanaged (manual) memory management where using managed memory would cause too much pressure on the garbage collector.

###### Configuration

The `MemoryManager` can be configured to use a specific allocator by calling `MemoryManager.UseImplementation<T>()` where `T` is a type implementing `IMemoryManager`. The default implementation is `NativeMemoryManager` which uses the light weight `NativeMemory` `libc` wrapper provided by .NET 6+ to allocate and free unmanaged memory. 

###### Allocation Tracking

The `MemoryManager` can be configured to use allocation tracking by wrapping the memory manager implementation in an `AllocationTracker<T>` and passing it to `MemoryManager.UseImplementation<T>()`:

```csharp
MemoryManager.UseImplementation<AllocationTracker<NativeMemoryManager>>();
```

Once allocation tracking is configured any allocation using either `MemoryManager.Malloc()`, `MemoryManager.Calloc()` or `MemoryManager.ReAlloc()` will be tracked an can be retrieved calling `MemoryManager.GetAllocationSnapshot()`. Doing so will return an `AllocationSnapshot` which contains the total number of unmanaged bytes allocated by the memory manager as well as a list of target sites and stacktraces where these bytes were allocated.

Optionally, a `ThreadLocalAllocationTracker<T>` can be used to track allocations on a per-thread basis. This is useful when tracking allocations in a multithreaded environment where multiple threads are allocating memory concurrently. To do so, simply wrap the memory manager implementation in a `ThreadLocalAllocationTracker<T>` and pass it to `MemoryManager.UseImplementation<T>()`:

```csharp
MemoryManager.UseImplementation<ThreadLocalAllocationTracker<NativeMemoryManager>>();
```

###### MemoryManager APIs

The `MemoryManager` class provides the following APIs:

- Allocator API - The allocator API provides direct function pointers to the underlying allocator implementation of the `Malloc()`, `Calloc<T>()`, `ReAlloc()` and `Free()` functions.
- Allocation Tracking API - If allocation tracking is enabled the `MemoryManager` provides APIs to retrieve an `AllocationSnapshot` containing the total number of unmanaged bytes allocated by the memory manager as well as a list of target sites and stacktraces where these bytes were allocated. Additionally, external allocations may be registered for tracking using `MemoryManager.TryRegisterExternalAllocation()` and `MemoryManager.TryUnregisterExternalAllocation()`.
- Memory Operations API - Additional common memory operations from `libc` are exposed via `Memset()`, `Memcpy()`, and `ZeroMemory()`.

> :x: **Caution**
> Changing the memory manager implementation after it has been used may result in undefined behavior.

#### `TypeReinterpreter` Class

From time to time it may be necessary to reinterpret a given type as another type, in some of these cases the usual managed way of casting may not be sufficient, for example due to excessive runtime checks degrading performance, or when applying IEEE 754 floating point bit trickery on a given type. The `TypeReinterpreter` class emulates the behavior of `reinterpret_cast` in C++ in a performance-oriented way. It is important to note that the `TypeReinterpreter` class is not a replacement for the usual managed way of casting and should only be used when necessary and when the types are known to be compatible in any case. The `TypeReinterpreter` class uses a mixture of `Unsafe.As()` and pointer arithmetic to reinterpret a given type as another type. The following example shows how to reinterpret a `float` as an `int`:

```csharp
using static TypeReinterpreter;
...
bool foo = true;
byte bar = ReinterpretCast<bool, byte>(foo);
```

### `Wkg.Reflection` Namespace

The `Reflection` namespace provides easy access to common reflective operations, primarily for interacting with generic types. It contains the following classes:

- `BackingFieldResolver` - Provides methods for resolving backing fields of properties.
- `Bindings` - Provides common binding flags for use with reflection.
- `ExpressionExtensions` - Provides extension methods for Expression trees. These extensions are heavily influenced by the internals of Entity Framework Core and are primarily used for reflective inspection of member access expressions in fluent configuration APIs. We moved these extensions to this library in order to provide a stable/reliable version of this internal EF Core API for use in our own libraries.
- `TypeExtensions` - Provides extension methods for the `Type` class. Primarily used for enumerating generic type arguments or checking whether a type implements or extends a generic type with specific generic type arguments.
- `TypeArray` - A factory class for creating `Type[]` arrays using the `TypeArray.Of<T1, T2, ...>()` method, which is more concise than the usual `new Type[] { typeof(T1), typeof(T2), ... }` syntax.
- `UnsafeReflection` - A factory class for creating concrete `MethodInfo` instances for the generic `Unsafe.As<...>()` methods. This is primarily used for dynamic code generation, such as IL-emission, or when building performance-oriented `Expression` trees. 

### `Wkg.Web` Namespace

The `Web` namespace provides utilities for working with web technologies, such as HTTP, HTML, and URLs.

#### Simple URI Manipulation

The `SimpleUriParser` and `SimpleUriBuilder` structures provide a very lightweight and simple way to parse and build trusted URIs. These structures are designed to be used in scenarios where the full power of the `System.Uri` class is not required, such as when working with URIs that are expected to be well-formed and do not require extensive validation. The `SimpleUriParser` structure provides a simple way to parse a URI into its components, while the `SimpleUriBuilder` structure provides a simple way to build a URI from its components.

```csharp
string uri = "https://example.com/foo?key1=value1&key2=value2";
SimpleUriParser parser = SimpleUriParser.Parse(uri);
Console.WriteLine(parser.Scheme);           // "https"
Console.WriteLine(parser.Host);             // "example.com"
Console.WriteLine(parser.Path);             // "/foo"
Console.WriteLine(parser.Query);            // "key1=value1&key2=value2"
Console.WriteLine(parser.SchemaHost);       // "https://example.com"
Console.WriteLine(parser.SchemaHostPath);   // "https://example.com/foo"
Console.WriteLine(parser.Uri);              // "https://example.com/foo?key1=value1&key2=value2"
foreach (RouteDataRef query in parser.QueryParameters)
{
    Console.WriteLine($"{query.Key}: {query.Value}");
}
// key1: value1
// key2: value2
```

As shown in the example above, the `SimpleUriParser` structure provides properties for accessing parsed URI components, such as the scheme, host, and path, as well as for enumerating query parameters. Because the `SimpleUriParser` structure is designed to be very lightweight, all properties are `ReadOnlySpan<char>` instances over the original URI string. As such, the `SimpleUriParser` structure is a `ref struct` and and cannot be used in scenarios where the URI string may fall out of scope. Similarly, as implied by the name, the `RouteDataRef` query parameters are also restricted to stack-only usage, but can be copied to heap memory using the `CreateDeepCopy()` method, which returns a `RouteData` instance with key and value properties allocated as normal managed strings.

The `SimpleUriBuilder` structure provides a simple way to build a URI from its components:

```csharp
using SimpleUriBuilder builder = SimpleUriBuilder.Create("https://example.com", capacity: 256);
builder.AppendPath("foo/");
builder.AppendPath("/bar");
builder.AppendQuery("key1", "value1&value2");
builder.AppendQuery("key2", "value2");
string uri = builder.ToString(); // "https://example.com/foo/bar?key1=value1&amp;value2&key2=value2"
```

As shown in the example above, the `SimpleUriBuilder` structure provides methods for appending path segments and query parameters to the URI. Query parameters are automatically URL-encoded, and the `ToString()` method returns the built URI as a `string`. The `capacity` parameter of the `SimpleUriBuilder.Create()` method specifies the initial capacity of the internal pooled string builder used to build the URI. The overall length of the uri may grow beyond the initial capacity, but once the capacity is exceeded, the internal string builder will become ineligible for pooling and can no longer be reused.

`SimpleUriParser` and `SimpleUriBuilder` are designed to be compatible with each other, and can be used together to perform transformations on URIs. For example, the following code snippet demonstrates how to remove a query parameter from a URI:

```csharp
string uri = "https://example.com/foo?key1=value1&key2=value2&key3=value3&amp;4";
SimpleUriParser parser = SimpleUriParser.Parse(uri);
using SimpleUriBuilder builder = SimpleUriBuilder.Create(parser.SchemaHostPath, capacity: 256);
foreach (RouteDataRef query in parser.QueryParameters)
{
    if (query.Key != "key2")
    {
        builder.AppendQuery(query, urlEncode: false);
    }
}
string newUri = builder.ToString(); // "https://example.com/foo?key1=value1&key3=value3&amp;4"
```

In the example above, the `SimpleUriParser` structure is used to parse the original URI, and the `SimpleUriBuilder` structure is used to build a new URI without the `key2` query parameter. The `urlEncode` parameter of the `SimpleUriBuilder.AppendQuery()` method specifies whether the query parameter should be URL-encoded. By setting `urlEncode` to `false`, the query parameter is appended as-is, preventing double-encoding of the query parameter with `key3`.

### `Wkg.SyntacticSugar` Class

The `SyntacticSugar` class aims to increase the conciseness and maintainability of C# code, rather than providing new functionality.

#### `Pass()` Method

The `Pass()` method is a no-op method to explicitly indicate that a method implementation is intentionally empty. 

##### Examples

When implementing the `IEnumerator<out T>` interface, for example, one must also implement the `IDisposable` interface, regardless of whether the implementation actually requires any resources to be disposed. In this case, the `Pass()` method can be used to indicate that the implementation is intentionally empty:

```csharp
using static SyntacticSugar;

...

public void Dispose() => Pass();
```

The `Pass()` method will be inlined by the JIT compiler and will not result in any additional overhead compared to the usual empty implementation:

```csharp
public void Dispose()
{
    // Why is this empty? is the implementation missing, or is it intentionally empty?
}
```

#### Switch Expression Helpers

C# switch expressions are a great way to write concise and readable code, however, depending on the circumstances, they can not always be used. For example, when different operations do not produce a defined return type one must resort to using a switch statement, which often brings a lot of boilerplate code with it. For example, the following code snippet dispatches different void-operations depending on the value of `myEnum`:

```csharp
using static SyntacticSugar;

switch (myEnum)
{
    case MyEnum.Value1:
        Something(myString); // void
        break;
    case MyEnum.Value2:
        SomethingElse(); // void
        break;
    case MyEnum.Value3:
        SomethingElse(); // void
    default:
        throw new ArgumentOutOfRangeException(nameof(myEnum), myEnum, null);
}
```

The `SyntacticSugar` class provides the `Do()` method which can wrap any expression and will return a `null` object to satisfy the return type requirement of the switch statement. The above code snippet can be rewritten as follows:

```csharp
_ = myEnum switch
{
    MyEnum.Value1 => Do(Something, myString),
    MyEnum.Value2 => Do(SomethingElse),
    MyEnum.Value3 => Do(SomethingElse),
    _ => throw new ArgumentOutOfRangeException(nameof(myEnum), myEnum, null)
};
```

#### The Double-Discard Object

Similarly, in some cases it may be beneficial to use a switch expression even though one does not have a value to switch on and would otherwise have to resort to using an if-else statement. For example, the following code snippet executes different operations depending on the relationship between two values:

```csharp
if (myValue1 > myValue2)
{
    Something(myValue1); // void
}
else if (myValue1 < myValue2)
{
    SomethingElse(myValue2); // void
}
else
{
    SomethingElse(null); // void
}
```

The `SyntacticSugar` class provides the `__` Double-Discard Object (with the value `default(object?)`, or `null`) which can be used to satisfy the switch expression requirement of having a value to switch on. The above code snippet can be rewritten as follows:

```csharp
using static SyntacticSugar;

...

_ = __ switch
{
    _ when myValue1 > myValue2 => Do(Something, myValue1),
    _ when myValue1 < myValue2 => Do(SomethingElse, myValue2),
    _ => Do(SomethingElse, null)
};
```

In above example, the `__` Double-Discard Object is used to indicate that the switch expression should be evaluated solely based on the result of the following `when` clauses, and that the value used for the switch expression itself is irrelevant (therefore `__`, to indicate that the value is discarded).
At the same time, `Do()` is used to wrap the different void-operations and to satisfy the return type requirement of the switch expression.
The return value of the switch expression is explicitly discarded by assigning it to `_` (the [discard operator](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/discards)).

> :bulb: **Tip**
> The combination of `__` and `Do()` allows for switch expressions to be used as dispatchers for void-operations, which can significantly reduce the amount of boilerplate code required to implement such dispatchers.
