using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal enum EditContextLifecycleTraceEventKind
{
    RootPropertyDisoccupy,
    RootPropertyOccupy,
    DescendantDetach,
    DescendantAttach
}

internal readonly record struct EditContextLifecycleTraceEvent(
    EditContextLifecycleTraceEventKind Kind,
    EditContext RootEditContext,
    EditContext ActorEditContext);

internal static class EditContextLifecycleTrace
{
    private sealed class ScopeHolder(Action<EditContextLifecycleTraceEvent> sink, ScopeHolder? previous)
    {
        public Action<EditContextLifecycleTraceEvent> Sink { get; } = sink;
        public ScopeHolder? Previous { get; } = previous;
    }

    private sealed class ScopeToken : IDisposable
    {
        private readonly ScopeHolder? _previous;

        public ScopeToken(ScopeHolder? previous) => _previous = previous;

        public void Dispose() => s_scope.Value = _previous;
    }

    private static readonly AsyncLocal<ScopeHolder?> s_scope = new();

    public static IDisposable BeginScope(Action<EditContextLifecycleTraceEvent> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        var current = s_scope.Value;
        s_scope.Value = new ScopeHolder(sink, current);
        return new ScopeToken(current);
    }

    public static void Emit(EditContextLifecycleTraceEventKind kind, EditContext rootEditContext, EditContext actorEditContext)
    {
        if (s_scope.Value is not { Sink: var sink }) {
            return;
        }

        sink(new EditContextLifecycleTraceEvent(kind, rootEditContext, actorEditContext));
    }
}
