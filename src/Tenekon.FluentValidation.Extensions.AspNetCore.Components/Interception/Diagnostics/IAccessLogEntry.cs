namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal interface IAccessLogEntry
{
    AccessLogSubject Subject { get; }
    object? UntypedValue { get; }
}
