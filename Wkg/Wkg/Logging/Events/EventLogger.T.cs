using System.Diagnostics;

namespace Wkg.Logging.Events;

public class EventLogger<TEventArgs>
{
    private readonly Action<object, TEventArgs> _onEventFired;
    private readonly string _callingClass;
    private readonly string _callingAssembly;
    private readonly string _eventName;
    private readonly ILogger _logger;
    private readonly string _instanceName;

    [StackTraceHidden]
    public EventLogger(string objectName, string eventName, Action<object, TEventArgs> onEventFired, ILogger? logger = null)
    {
        _logger = logger ?? Log.CurrentLogger;
        _onEventFired = onEventFired;
        _instanceName = objectName;
        _eventName = eventName;

        StackFrame frame = new();

        Type? caller = frame.GetMethod()?.DeclaringType;
        if (caller is null)
        {
            _callingAssembly = "UNKNOWN";
            _callingClass = "(UNKNOWN CALLER)";
            return;
        }
        else
        {
            _callingAssembly = caller.Assembly.GetName().Name ?? "UNKNOWN";
            _callingClass = caller.Name;
        }
    }

    /// <summary>
    /// The proxy <see cref="EventHandler{TEventArgs}"/> to handle, log and forward the event.
    /// </summary>
    /// <remarks>Needs to be subscribed to the target event.</remarks>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    public void OnEventFired(object sender, TEventArgs e)
    {
        _logger.Log(_instanceName, _eventName, e, _callingAssembly, _callingClass);
        _onEventFired?.Invoke(sender, e);
    }
}
