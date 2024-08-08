using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators.Helpers;
using Wkg.Logging.Intrinsics.CallStack;

namespace Wkg.Logging.Generators;

/// <summary>
/// A <see cref="DetailedAotLogEntryGenerator"/>-like implementation balancing runtime reflection requirements through caching with detailed log entries enumerated on compile time, generating log entries in the following format:
/// <code>
/// 2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> Output: 'This is a log message'
/// 2023-05-31 14:14:24.626 (UTC) MyAssembly: [ERROR->Thread_0x1(MAIN THREAD)] (MyClass.cs:L240->MyMethod) ==> [NullReferenceException] info: 'while trying to do a thing' original: 'Object reference not set to an instance of an object.' at:
///   StackTrace line 1
/// 2023-05-31 14:14:24.626 (UTC) MyAssembly: [Event->Thread_0x1(MAIN THREAD)] (MyClass.cs:L1337->MyMethod) ==> MyAssembly::MyClass::MyButtonInstance::OnClick(MyEventType: eventArgs)
/// </code>
/// </summary>
/// <remarks>
/// This class minimizes reflective enumeration of target site information and stack unwinding through caching, making it a good candidate for use in production environments.
/// </remarks>
[RequiresUnreferencedCode("Requires reflective access to determine the assembly name of the caller.")]
public class BalancedLogEntryGenerator : DetailedAotLogEntryGenerator, ILogEntryGenerator<BalancedLogEntryGenerator>
{
    private static readonly ConcurrentDictionary<int, string> s_assemblyNameCallSiteCache = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BalancedLogEntryGenerator"/> class.
    /// </summary>
    /// <param name="config">The <see cref="CompiledLoggerConfiguration"/> used to create this <see cref="BalancedLogEntryGenerator"/>.</param>
    protected BalancedLogEntryGenerator(CompiledLoggerConfiguration config) : base(config) => Pass();

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Requires reflective access to determine the assembly name of the caller.")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    public static new BalancedLogEntryGenerator Create(CompiledLoggerConfiguration config) => new(config);
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    /// <summary>
    /// Generates the header for the <paramref name="entry"/> and appends it to the <paramref name="builder"/>.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 2023-05-31 14:14:24.626 (UTC) AssemblyName: [Event->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==>
    /// </code>
    /// </remarks>
    /// <param name="entry">The <see cref="LogEntry"/> to generate the header for.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to append the header to.</param>
    [StackTraceHidden]
    protected override void AddHeader(ref LogEntry entry, StringBuilder builder)
    {
        if (entry.AssemblyName is null)
        {
            if (entry.CallerInfo.FilePath.Length > 0 && entry.CallerInfo.MemberName.Length > 0)
            {
                int hashCode = entry.CallerInfo.FilePath.GetHashCode() ^ entry.CallerInfo.MemberName.GetHashCode();
                if (!s_assemblyNameCallSiteCache.TryGetValue(hashCode, out string? assemblyName))
                {
                    assemblyName = GetAssemblyNameFromStackTrace();
                    s_assemblyNameCallSiteCache.TryAdd(hashCode, assemblyName);
                }
                entry.AssemblyName = assemblyName;
            }
            else
            {
                entry.AssemblyName = GetAssemblyNameFromStackTrace();
            }
        }

        string textLogLevel = LogLevelNames.NameForOrUnknown(entry.LogLevel);
        string mainThreadTag = string.Empty;
        int threadId = Environment.CurrentManagedThreadId;
        entry.ThreadId = threadId;
        if (threadId == _config.MainThreadId)
        {
            mainThreadTag = "(MAIN THREAD)";
            entry.IsMainThread = true;
        }
        entry.TimestampUtc = DateTime.UtcNow;
        int fileNameIndex = entry.CallerInfo.GetFileNameStartIndex();
        builder.Append(entry.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" (UTC) ")
            .Append(entry.AssemblyName)
            .Append(": [")
            .Append(textLogLevel)
            .Append("->Thread_0x")
            .Append(threadId.ToString("x"))
            .Append(mainThreadTag)
            .Append("] (")
            .Append(entry.CallerInfo.FilePath.AsSpan()[fileNameIndex..])
            .Append(":L")
            .Append(entry.CallerInfo.LineNumber)
            .Append("->")
            .Append(entry.CallerInfo.MemberName)
            .Append(") ==> ");
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetAssemblyNameFromStackTrace()
    {
        StackTrace stack = new();
        MethodBase? method = stack.GetFirstNonHiddenCaller();
        return method?.DeclaringType?.Assembly.GetName().Name ?? "<UnknownAssembly>";
    }
}
