using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.Queuing.Classful.Intrinsics;

internal class EmptyCounter
{
    // the emptiness counter is a 64 bit value that is split into two parts:
    // the first 32 bits are the generation counter, the last 32 bits are the actual counter
    // the generation counter is incremented whenever the counter is reset
    // we use a single 64 bit value to allow for atomic operations on both parts
    private ulong _state;

    public void SetEmpty() => Interlocked.Exchange(ref _state, ulong.MaxValue);

    public void Reset()
    {
        ulong state, newState;
        do
        {
            state = Volatile.Read(ref _state);
            uint generation = (uint)(state >> 32);
            newState = (ulong)(generation + 1) << 32;
        } while (Interlocked.CompareExchange(ref _state, newState, state) != state);
        DebugLog.WriteDiagnostic($"Reset emptiness counter. Current token: {newState >> 32}.", LogWriter.Blocking);
    }

    /// <summary>
    /// Increments the counter if the specified token is valid.
    /// </summary>
    /// <param name="token">The token previously obtained from <see cref="GetToken"/>.</param>
    /// <returns>The actual counter value after the increment. If the token is invalid, the current counter value is returned.</returns>
    public uint TryIncrement(uint token)
    {
        ulong state, newState;
        do
        {
            state = Volatile.Read(ref _state);
            uint currentGeneration = (uint)(state >> 32);
            if (currentGeneration != token)
            {
                // the generation changed, so the counter was reset
                // we can't increment the counter, since it's not the current generation
                DebugLog.WriteDebug($"Ignoring invalid emptiness counter token {token}. Current token is {currentGeneration}.", LogWriter.Blocking);
                return (uint)state;
            }
            // this is probably safe, if the counter ever overflows into the generation
            // then you definitely have bigger problems than a broken round robin qdisc :)
            newState = state + 1;
        } while (Interlocked.CompareExchange(ref _state, newState, state) != state);
        DebugLog.WriteDiagnostic($"Incremented emptiness counter to {newState & uint.MaxValue}.", LogWriter.Blocking);
        return (uint)newState;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetToken() => (uint)(Volatile.Read(ref _state) >> 32);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetCount() => (uint)Volatile.Read(ref _state);
}
