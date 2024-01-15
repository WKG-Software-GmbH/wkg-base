using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1;
internal class Class1
{
    public static void TestAsync()
    {
        //TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ThreadPool.QueueUserWorkItem(_ => Test());
    }

    private static void Test()
    {
        Console.WriteLine("Blah");
    }
}
