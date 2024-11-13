using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Unmanaged.MemoryManagement;
using Wkg.Unmanaged.MemoryManagement.Implementations;
using Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

namespace Wkg.Tests;

public abstract class BaseTest
{
    private static readonly Lock s_lock = new();
    private static bool s_isInitialized = false;

    private protected BaseTest()
    {
        lock (s_lock)
        {
            if (!s_isInitialized)
            {
                MemoryManager.UseImplementation<ThreadLocalAllocationTracker<NativeMemoryManager>>();
                s_isInitialized = true;
            }
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        AllocationSnapshot snapshot = MemoryManager.GetAllocationSnapshot(reset: true)!;
        Assert.AreEqual(0uL, snapshot.TotalByteCount, snapshot.ToString());
    }
}
