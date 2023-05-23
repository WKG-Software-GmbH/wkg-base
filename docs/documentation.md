# `Wkg` Documentation

`Wkg` is a company-internal library providing reusable components for the development of any .NET project.

- [`Wkg` Documentation](#wkg-documentation)
  - [Components](#components)
    - [`Collections` Namespace](#collections-namespace)
      - [`CyclicQueue` and `CyclicStack`](#cyclicqueue-and-cyclicstack)
    - [VolatileArray](#volatilearray)
    - [ResizableBuffer](#resizablebuffer)
      - [Important Notices ❗](#important-notices-)
      - [Examples](#examples)
    - [`Data.Validation` Namespace](#datavalidation-namespace)
      - [Examples](#examples-1)
    - [`Extensions` Namespace](#extensions-namespace)
      - [`GuidExtensions` Class](#guidextensions-class)
        - [Examples](#examples-2)
      - [`NullableValueTypeExtensions` Class](#nullablevaluetypeextensions-class)
      - [`ObjectExtensions` Class](#objectextensions-class)
    - [`Unmanaged` Namespace](#unmanaged-namespace)
      - [Memory Management](#memory-management)
        - [The `MemoryManager` Class](#the-memorymanager-class)
          - [Configuration](#configuration)
          - [Allocation Tracking](#allocation-tracking)
          - [MemoryManager APIs](#memorymanager-apis)
        - [Important Notices ❗](#important-notices--1)
      - [`TypeReinterpreter` Class](#typereinterpreter-class)
    - [`Reflection` Namespace](#reflection-namespace)
    - [`SyntacticSugar` Class](#syntacticsugar-class)
      - [`Pass()` Method](#pass-method)
        - [Examples](#examples-3)
      - [Switch Expression Helpers](#switch-expression-helpers)
      - [The Double-Discard Object](#the-double-discard-object)

## Components

### `Collections` Namespace

The `Collections` namespace provides both general purpose as well as very specific and performance-oriented generic collections to be used internally or by dependant applications.

#### `CyclicQueue` and `CyclicStack`

The `CyclicQueue<T>` and `CyclicStack<T>` are FIFO and LIFO data structures implemented on top of a ring buffer (that's what the internal `RingBufferPointer<T>` is used for btw).

The inherent nature of the underlying ring buffer allows these data structures to override the oldest elements if a given threshold is reached and more elements are added. Other than that they have the same functionality as a normal `Stack` or `Queue`. Use-cases may include instances where only the most recent elements are of interest, e.g. for caching, logging, or other purposes.

### VolatileArray

As its name implies the volatile array can be used in multithreaded environments where multiple concurrent threads have to be able access the same array. Note that this doesn't make it thread-safe on its own. All it does is disabling certain memory optimizations and re-orderings during volatile reads and writes using memory barriers and therefore doesn't rule out the potential for race conditions (for more info see [`volatile` (C# Reference)](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile)).

### ResizableBuffer

The `unsafe struct ResizableBuffer<T> : IDisposable where T : unmanaged` is a highly performant collection operating on a continuous block of **unmanaged** memory. This enables us to increase the performance of our apps by writing (managed) allocation free code in places where `stackalloc` becomes less efficient for large allocation. Especially when handling chunked network streams this is an advantage as the only other option would be to use managed arrays that will have to be garbage-collected afterwards. The `ResizableBuffer<T>` supports both indexing as well as the `Add(Span<T>)` operation to append the contents of a `Span<T>` to the end of the used space in the buffer.
When the size of the allocated memory is exceeded a new block will automatically be allocated using the current [`MemoryManager.Realloc(void*, int)`](#memory-manager) implementation. 

#### Important Notices ❗

- As `ResizableBuffer<T>` is a struct ensure not to create unintentional copies as this may lead to dangling pointers if either the original or the copy is disposed. Therefore instances **must** be passed by reference (`ref`, `in`) or not at all.
- Ensure instances are properly disposed before falling out of scope. Otherwise unmanaged memory will be leaked.

#### Examples

An excellent example of code free of managed allocations for buffering using the `ResizableBuffer<T>` can be seen in the network code of _Clubmapp_ that reads a response stream into a UTF-8 JSON string:

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

### `Data.Validation` Namespace

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

### `Extensions` Namespace

The `Extensions` namespace provides a set of common extension methods for various types.

#### `GuidExtensions` Class

A common issue with .NET's `Guid` type is that its `ToString()` method returns a mixed-endian string representation of the `Guid` which is not compatible with the string representation used by most databases. In order to mitigate this issue the `GuidExtensions` class provides the performance-optimized `ToStringBigEndian()` extension method which returns a big-endian string representation of the `Guid` that can be used for database operations.

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

### `Unmanaged` Namespace 

The `Unmanaged` namespace provides easy access to unmanaged memory and exposes `Malloc()`, `Calloc<T>()`, `ReAlloc()` and `Free()` from `libc` while also providing optional allocation tracking to prevent memory leaks in production code.

#### Memory Management

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

##### Important Notices ❗

- Changing the memory manager implementation after it has been used may result in undefined behavior.

#### `TypeReinterpreter` Class

From time to time it may be necessary to reinterpret a given type as another type, in some of these cases the usual managed way of casting may not be sufficient, for example due to excessive runtime checks degrading performance, or when applying IEEE 754 floating point bit trickery on a given type. The `TypeReinterpreter` class emulates the behavior of `reinterpret_cast` in C++ in a performance-oriented way. It is important to note that the `TypeReinterpreter` class is not a replacement for the usual managed way of casting and should only be used when necessary and when the types are known to be compatible in any case. The `TypeReinterpreter` class uses a mixture of `Unsafe.As()` and pointer arithmetic to reinterpret a given type as another type. The following example shows how to reinterpret a `float` as an `int`:

```csharp
using static TypeReinterpreter;
...
bool foo = true;
byte bar = ReinterpretCast<bool, byte>(foo);
```

### `Reflection` Namespace

The `Reflection` namespace provides easy access to common reflective operations, primarily for interacting with generic types. It contains the following classes:

- `TypeExtensions` - Provides extension methods for the `Type` class. Primarily used for enumerating generic type arguments or checking whether a type implements or extends a generic type with specific generic type arguments.
- `TypeArray` - A factory class for creating `Type[]` arrays using the `TypeArray.Of<T1, T2, ...>()` method, which is more concise than the usual `new Type[] { typeof(T1), typeof(T2), ... }` syntax.
- `Bindings` - Provides common binding flags for use with reflection.
- `BackingFieldResolver` - Provides methods for resolving backing fields of properties.
- `UnsafeReflection` - A factory class for creating concrete `MethodInfo` instances for the generic `Unsafe.As<...>()` methods. This is primarily used for dynamic code generation, such as IL-emission, or when building performance-oriented `Expression` trees. 

### `SyntacticSugar` Class

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

The combination of these two features allows for switch expressions to be used as dispatchers for void-operations, which can significantly reduce the amount of boilerplate code required to implement such dispatchers.