using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wkg.Threading;

public static class Wait
{
    public static bool Until(Func<bool> condition) => 
        Until(condition, Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(20));

    public static bool Until(Func<bool> condition, TimeSpan timeout) => 
        Until(condition, timeout, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(20));

    public static bool Until(Func<bool> condition, TimeSpan timeout, TimeSpan spinTimeout, TimeSpan sleepDuration) => 
        Until(condition, (int)timeout.TotalMilliseconds, (int)spinTimeout.TotalMilliseconds, (int)sleepDuration.TotalMilliseconds);

    public static bool Until(Func<bool> condition, int timeoutMillis = -1, int spinTimeoutMillis = 1000, int sleepDurationMillis = 20)
    {
        int startTimeTicks = Environment.TickCount;
        if (condition())
        {
            return true;
        }
        if (timeoutMillis == 0)
        {
            return false;
        }
        // Timeout.Infinite is -1 ^^
        int spinTimeout = Math.Min(spinTimeoutMillis, Math.Max(timeoutMillis, Timeout.Infinite));
        if (SpinWait.SpinUntil(condition, spinTimeout))
        {
            return true;
        }
        if (timeoutMillis == Timeout.Infinite)
        {
            while (!condition())
            {
                Thread.Sleep(sleepDurationMillis);
            }
            return true;
        }
        while (!condition())
        {
            if (Environment.TickCount - startTimeTicks >= timeoutMillis)
            {
                return false;
            }
            Thread.Sleep(sleepDurationMillis);
        }
        return true;
    }
}
