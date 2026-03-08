namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal static class AccessLogEntry
{
    public static AccessLogEntry<T> Of<T>(T value, AccessLogSubject subject, int indexShift = 0)
        => new(value, subject, IndexShift: indexShift);
}
