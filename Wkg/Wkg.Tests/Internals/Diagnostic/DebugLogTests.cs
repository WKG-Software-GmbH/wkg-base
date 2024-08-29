using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Wkg.Internals.Diagnostic;
using Wkg.Logging;

namespace Wkg.Tests.Internals.Diagnostic;

[TestClass]
public class DebugLogTests
{
    [TestMethod]
    public void EnsureImplementsLogPattern()
    {
        // because DebugLog uses ConditionalAttribute, we cannot implement the ILog interface.
        // instead, we must ensure that the DebugLog class implements the same pattern as the ILog interface.
        Type type = typeof(DebugLog);
        Type interfaceType = typeof(ILog);

        MethodInfo[] methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        foreach (MethodInfo method in methods)
        {
            MethodInfo? implementation = type.GetMethod(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray());
            Assert.IsNotNull(implementation, $"The method {method.Name} is not implemented by the DebugLog class.");
            Assert.IsTrue(implementation.IsStatic, $"The method {method.Name} is not static.");
            Assert.IsTrue(implementation.IsPublic, $"The method {method.Name} is not public.");
        }
    }
}