using System.Diagnostics;

namespace Wkg.Logging.Events;

public static class EventLogger
{
    [StackTraceHidden]
    public static EventLogger<TEventArgs> For<TEventArgs>(string objectName, string eventName, Action<object, TEventArgs> onEventFired, ILogger? logger = null) =>
        new(objectName, eventName, onEventFired, logger);
}
