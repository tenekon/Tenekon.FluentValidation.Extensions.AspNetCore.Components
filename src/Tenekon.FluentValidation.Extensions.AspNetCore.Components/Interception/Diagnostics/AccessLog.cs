using System.Collections;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Reflection;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal sealed class AccessLog : IAccessLogger, IAccessLog
{
    private readonly List<object> _entries = [];

    void IAccessLogger.LogAccess<T>(AccessLogEntry<T> accessLogEntry) => _entries.Add(accessLogEntry);

    public IEnumerator<object> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    public void Clear() => _entries.Clear();
}
