using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Unmanaged.MemoryManagement;
using Wkg.Unmanaged.MemoryManagement.Implementations;
using Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

namespace Wkg.Tests;

public abstract class BaseTest
{
    private static readonly object _lock = new();
    private static bool _isInitialized = false;

    private protected BaseTest()
    {
        lock (_lock)
        {
            if (!_isInitialized)
            {
                MemoryManager.UseImplementation<ThreadLocalAllocationTracker<NativeMemoryManager>>();
                _isInitialized = true;
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
