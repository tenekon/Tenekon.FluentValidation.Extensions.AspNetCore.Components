namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal sealed class CompositeAccessLogger : IAccessLogger
{
    private readonly List<IAccessLogger> _accessLoggers = [];

    public CompositeAccessLogger(params IAccessLogger[] accessLoggers)
    {
        foreach (var accessLogger in accessLoggers) {
            Add(accessLogger);
        }
    }

    public void Add(IAccessLogger accessLogger)
    {
        if (_accessLoggers.Any(existingAccessLogger => ReferenceEquals(existingAccessLogger, accessLogger))) {
            return;
        }

        _accessLoggers.Add(accessLogger);
    }

    public void LogAccess<T>(AccessLogEntry<T> accessLogEntry)
    {
        foreach (var accessLogger in _accessLoggers) {
            accessLogger.LogAccess(accessLogEntry);
        }
    }
}
