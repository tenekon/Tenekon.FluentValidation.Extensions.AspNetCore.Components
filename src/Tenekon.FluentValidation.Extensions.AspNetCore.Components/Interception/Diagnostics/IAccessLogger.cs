namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal interface IAccessLogger
{
    void LogAccess<T>(AccessLogEntry<T> accessLogEntry);
}
