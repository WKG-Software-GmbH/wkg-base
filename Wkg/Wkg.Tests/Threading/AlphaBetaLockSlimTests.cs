using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Threading;

namespace Wkg.Tests.Threading;

[TestClass]
public class AlphaBetaLockSlimTests
{
    [TestMethod]
    public void TestEnterAndExitAlphaLock()
    {
        using AlphaBetaLockSlim abls = new();
        abls.EnterAlphaLock();
        Assert.IsTrue(abls.IsAlphaLockHeld);
        abls.ExitAlphaLock();
        Assert.IsFalse(abls.IsAlphaLockHeld);
    }

    [TestMethod]
    public void TestEnterAndExitBetaLock()
    {
        using AlphaBetaLockSlim abls = new();
        abls.EnterBetaLock();
        Assert.IsTrue(abls.IsBetaLockHeld);
        abls.ExitBetaLock();
        Assert.IsFalse(abls.IsBetaLockHeld);
    }

    [TestMethod]
    public void TestTryEnterAlphaLock()
    {
        using AlphaBetaLockSlim abls = new();
        bool entered = abls.TryEnterAlphaLock(TimeSpan.FromSeconds(1));
        Assert.IsTrue(entered);
        Assert.IsTrue(abls.IsAlphaLockHeld);
        abls.ExitAlphaLock();
    }

    [TestMethod]
    public void TestTryEnterBetaLock()
    {
        using AlphaBetaLockSlim abls = new();
        bool entered = abls.TryEnterBetaLock(TimeSpan.FromSeconds(1));
        Assert.IsTrue(entered);
        Assert.IsTrue(abls.IsBetaLockHeld);
        abls.ExitBetaLock();
    }

    [TestMethod, Timeout(10000)]
    public void AlphasAreMutuallyExclusiveFromBetas()
    {
        using AlphaBetaLockSlim abls = new();
        using Barrier barrier = new(2);
        Task.WaitAll(
            Task.Run(() =>
            {
                abls.EnterAlphaLock();
                barrier.SignalAndWait();
                Assert.IsTrue(abls.IsAlphaLockHeld);
                barrier.SignalAndWait();
                abls.ExitAlphaLock();
            }),
            Task.Run(() =>
            {
                barrier.SignalAndWait();
                Assert.IsFalse(abls.TryEnterBetaLock(0));
                Assert.IsFalse(abls.IsBetaLockHeld);
                barrier.SignalAndWait();
            }));
    }

    [TestMethod, Timeout(10000)]
    public void BetasAreMutuallyExclusiveFromAlphas()
    {
        using AlphaBetaLockSlim abls = new();
        using Barrier barrier = new(2);
        Task.WaitAll(
            Task.Run(() =>
            {
                abls.EnterBetaLock();
                barrier.SignalAndWait();
                Assert.IsTrue(abls.IsBetaLockHeld);
                barrier.SignalAndWait();
                abls.ExitBetaLock();
            }),
            Task.Run(() =>
            {
                barrier.SignalAndWait();
                Assert.IsFalse(abls.TryEnterAlphaLock(0));
                Assert.IsFalse(abls.IsAlphaLockHeld);
                barrier.SignalAndWait();
            }));
    }

    [TestMethod, Timeout(10000)]
    public void AlphasArePermittedToBeEnteredConcurrently()
    {
        using AlphaBetaLockSlim abls = new();
        using Barrier barrier = new(2);
        Task.WaitAll(
            Task.Run(() =>
            {
                abls.EnterAlphaLock();
                barrier.SignalAndWait();
                Assert.IsTrue(abls.IsAlphaLockHeld);
                barrier.SignalAndWait();
                abls.ExitAlphaLock();
                Assert.IsFalse(abls.IsAlphaLockHeld);
                barrier.SignalAndWait();
            }),
            Task.Run(() =>
            {
                barrier.SignalAndWait();
                Assert.IsTrue(abls.TryEnterAlphaLock(0));
                Assert.IsTrue(abls.IsAlphaLockHeld);
                barrier.SignalAndWait();
                Assert.IsTrue(abls.IsAlphaLockHeld);
                barrier.SignalAndWait();
                abls.ExitAlphaLock();
                Assert.IsFalse(abls.IsAlphaLockHeld);
                Assert.IsTrue(abls.TryEnterBetaLock(0));
                Assert.IsTrue(abls.IsBetaLockHeld);
                abls.ExitBetaLock();
                Assert.IsFalse(abls.IsBetaLockHeld);
                Assert.AreEqual(0, abls.WaitingAlphaCount);
                Assert.AreEqual(0, abls.WaitingBetaCount);
            }));
    }

    [TestMethod, Timeout(10000)]
    public void BetasArePermittedToBeEnteredConcurrently()
    {
        using AlphaBetaLockSlim abls = new();
        using Barrier barrier = new(2);
        Task.WaitAll(
            Task.Run(() =>
            {
                abls.EnterBetaLock();
                barrier.SignalAndWait();
                Assert.IsTrue(abls.IsBetaLockHeld);
                barrier.SignalAndWait();
                abls.ExitBetaLock();
                Assert.IsFalse(abls.IsBetaLockHeld);
                barrier.SignalAndWait();
            }),
            Task.Run(() =>
            {
                barrier.SignalAndWait();
                Assert.IsTrue(abls.TryEnterBetaLock(0));
                Assert.IsTrue(abls.IsBetaLockHeld);
                barrier.SignalAndWait();
                Assert.IsTrue(abls.IsBetaLockHeld);
                barrier.SignalAndWait();
                abls.ExitBetaLock();
                Assert.IsFalse(abls.IsBetaLockHeld);
                Assert.IsTrue(abls.TryEnterAlphaLock(0));
                Assert.IsTrue(abls.IsAlphaLockHeld);
                abls.ExitAlphaLock();
                Assert.IsFalse(abls.IsAlphaLockHeld);
                Assert.AreEqual(0, abls.WaitingAlphaCount);
                Assert.AreEqual(0, abls.WaitingBetaCount);
            }));
    }

    [TestMethod, Timeout(10000)]
    public void AlphaToBetaChain()
    {
        using AlphaBetaLockSlim abls = new();
        using ManualResetEventSlim mres = new();
        abls.EnterAlphaLock();
        Task t = Task.Factory.StartNew(() =>
        {
            Assert.IsFalse(abls.TryEnterBetaLock(TimeSpan.FromMilliseconds(10)));
            Task.Run(() => mres.Set());
            mres.Wait();
            abls.EnterBetaLock();
            abls.ExitBetaLock();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        mres.Wait();
        abls.ExitAlphaLock();
        t.GetAwaiter().GetResult();
    }

    [TestMethod, Timeout(10000)]
    public void AlphaToBetaChainEnsureWait()
    {
        using AlphaBetaLockSlim abls = new();
        using ManualResetEventSlim mres = new();
        abls.EnterAlphaLock();
        Task t = Task.Factory.StartNew(() =>
        {
            Assert.IsFalse(abls.TryEnterBetaLock(TimeSpan.FromMilliseconds(10)));
            Task.Run(() => Task.Delay(100).ContinueWith(_ => mres.Set()));
            abls.EnterBetaLock();
            abls.ExitBetaLock();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        mres.Wait();
        abls.ExitAlphaLock();
        t.GetAwaiter().GetResult();
    }

    [TestMethod, Timeout(10000)]
    public void BetaToAlphaChain()
    {
        using AlphaBetaLockSlim abls = new();
        using ManualResetEventSlim mres = new();
        abls.EnterBetaLock();
        Task t = Task.Factory.StartNew(() =>
        {
            Assert.IsFalse(abls.TryEnterAlphaLock(TimeSpan.FromMilliseconds(10)));
            Task.Run(() => mres.Set());
            mres.Wait();
            abls.EnterAlphaLock();
            abls.ExitAlphaLock();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        mres.Wait();
        abls.ExitBetaLock();
        t.GetAwaiter().GetResult();
    }

    [TestMethod, Timeout(10000)]
    public void BetaToAlphaChainEnsureWait()
    {
        using AlphaBetaLockSlim abls = new();
        using ManualResetEventSlim mres = new();
        abls.EnterBetaLock();
        Task t = Task.Factory.StartNew(() =>
        {
            Assert.IsFalse(abls.TryEnterAlphaLock(TimeSpan.FromMilliseconds(10)));
            Task.Run(() => Task.Delay(100).ContinueWith(_ => mres.Set()));
            abls.EnterAlphaLock();
            abls.ExitAlphaLock();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        mres.Wait();
        abls.ExitBetaLock();
        t.GetAwaiter().GetResult();
    }

    [TestMethod]
    public void ReleaseBetasWhenWaitingAlphaTimesOut()
    {
        using AlphaBetaLockSlim abls = new();
        // Enter the beta lock
        abls.EnterBetaLock();
        // Typical order of execution: 0

        Thread alphaWaiterThread;
        using (ManualResetEvent beforeTryEnterAlphaLock = new(false))
        {
            alphaWaiterThread = new Thread(() =>
            {
                // Typical order of execution: 1

                // Add a alpha to the wait list for enough time to allow successive betas to enter the wait list while this
                // alpha is waiting
                beforeTryEnterAlphaLock.Set();
                if (abls.TryEnterAlphaLock(1000))
                {
                    // The typical order of execution is not guaranteed, as sleep times are not guaranteed. For
                    // instance, before this write lock is added to the wait list, the two new read locks may be
                    // acquired. In that case, the test may complete before or while the write lock is taken.
                    abls.ExitAlphaLock();
                }

                // Typical order of execution: 4
            })
            {
                IsBackground = true
            };
            alphaWaiterThread.Start();
            beforeTryEnterAlphaLock.WaitOne();
        }
        Thread.Sleep(500); // wait for TryEnterAlphaLock to enter the wait list

        // An alpha should now be waiting, add betas to the wait list. Since a beta lock is still acquired, the alpha
        // should time out waiting, then these betas should enter and exit the lock.
        void EnterAndExitReadLock()
        {
            // Typical order of execution: 2, 3
            abls.EnterBetaLock();
            // Typical order of execution: 5, 6
            abls.ExitBetaLock();
        }
        Thread[] betaThreads = [new Thread(EnterAndExitReadLock), new Thread(EnterAndExitReadLock)];
        foreach (Thread betaThread in betaThreads)
        {
            betaThread.IsBackground = true;
            betaThread.Start();
        }
        foreach (Thread readerThread in betaThreads)
        {
            readerThread.Join();
        }

        abls.ExitBetaLock();
        // Typical order of execution: 7

        alphaWaiterThread.Join();
        betaThreads[0].Join();
        betaThreads[1].Join();
    }

    [TestMethod]
    public void InvalidTimeouts()
    {
        using AlphaBetaLockSlim abls = new();
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => abls.TryEnterBetaLock(-2));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => abls.TryEnterAlphaLock(-3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => abls.TryEnterAlphaLock(-4));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => abls.TryEnterBetaLock(TimeSpan.MaxValue));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => abls.TryEnterAlphaLock(TimeSpan.MinValue));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => abls.TryEnterAlphaLock(TimeSpan.FromMilliseconds(-2)));
    }

    [TestMethod]
    public void InvalidExit()
    {
        using AlphaBetaLockSlim abls = new();
        Assert.ThrowsException<SynchronizationLockException>(abls.ExitAlphaLock);
        Assert.ThrowsException<SynchronizationLockException>(abls.ExitBetaLock);
    }

    [TestMethod]
    public void InvalidTryEnter()
    {
        using AlphaBetaLockSlim abls = new();
        abls.EnterAlphaLock();
        Assert.ThrowsException<LockRecursionException>(() => abls.TryEnterAlphaLock(0));
        Assert.ThrowsException<InvalidOperationException>(() => abls.TryEnterBetaLock(0));
        abls.ExitAlphaLock();

        abls.EnterBetaLock();
        Assert.ThrowsException<InvalidOperationException>(() => abls.TryEnterAlphaLock(0));
        Assert.ThrowsException<LockRecursionException>(() => abls.TryEnterBetaLock(0));
        abls.ExitBetaLock();
    }

    [TestMethod]
    public void InvalidEnter()
    {
        using AlphaBetaLockSlim abls = new();
        abls.EnterAlphaLock();
        Assert.ThrowsException<LockRecursionException>(abls.EnterAlphaLock);
        Assert.ThrowsException<InvalidOperationException>(abls.EnterBetaLock);
        abls.ExitAlphaLock();

        abls.EnterBetaLock();
        Assert.ThrowsException<InvalidOperationException>(abls.EnterAlphaLock);
        Assert.ThrowsException<LockRecursionException>(abls.EnterBetaLock);
        abls.ExitBetaLock();
    }
}
