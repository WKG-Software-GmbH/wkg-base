using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1;

internal static class ReliableSpinner
{
    public static int Spin(int spinCount)
    {
        int result = 1;
        for (int i = 1; i <= spinCount; i++)
        {
            result = (result * i) % 1000000007;
        }
        return result;
    }
}
