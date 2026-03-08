using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal static class StackTraceMethodDescriptorProvider
{
    public static bool TryGet(StackFrame frame, out StackTraceMethodDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (TryGetDiagnostic(frame, out descriptor)) {
            return true;
        }

        return TryGetFallback(frame, out descriptor);
    }

#if NET9_0_OR_GREATER
    private static bool TryGetDiagnostic(StackFrame frame, out StackTraceMethodDescriptor descriptor)
    {
        var diagnosticMethodInfo = DiagnosticMethodInfo.Create(frame);
        if (diagnosticMethodInfo?.Name is not { Length: > 0 } name) {
            descriptor = default;
            return false;
        }

        descriptor = new(
            diagnosticMethodInfo.DeclaringAssemblyName,
            diagnosticMethodInfo.DeclaringTypeName,
            name);
        return true;
    }
#else
    private static bool TryGetDiagnostic(StackFrame frame, out StackTraceMethodDescriptor descriptor)
    {
        descriptor = default;
        return false;
    }
#endif

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Fallback path only applies when DiagnosticMethodInfo is unavailable; the published AOT host explicitly enables stack-trace support.")]
    private static bool TryGetFallback(StackFrame frame, out StackTraceMethodDescriptor descriptor)
    {
        var method = frame.GetMethod();
        if (method?.Name is not { Length: > 0 }) {
            descriptor = default;
            return false;
        }

        descriptor = new(
            method.DeclaringType?.Assembly.GetName().Name,
            method.DeclaringType?.FullName,
            method.Name);
        return true;
    }
}
