using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal readonly record struct StackTraceMethodDescriptor(
    string? DeclaringAssemblyName,
    string? DeclaringTypeName,
    string Name)
{
    public bool IsDeclaredBy(Type type)
        => IsDeclaredByAssembly(type.Assembly) &&
           string.Equals(DeclaringTypeName, type.FullName, StringComparison.Ordinal);

    public bool IsEditContextIterator(string methodGroupName)
        => IsDeclaredByAssembly(typeof(EditContext).Assembly) &&
           DeclaringTypeName is not null &&
           DeclaringTypeName.StartsWith($"{typeof(EditContext).FullName}+", StringComparison.Ordinal) &&
           DeclaringTypeName.Contains(methodGroupName, StringComparison.Ordinal);

    private bool IsDeclaredByAssembly(Assembly assembly)
    {
        if (DeclaringAssemblyName is not { Length: > 0 } declaringAssemblyName) {
            return false;
        }

        if (string.Equals(declaringAssemblyName, assembly.GetName().Name, StringComparison.Ordinal) ||
            string.Equals(declaringAssemblyName, assembly.FullName, StringComparison.Ordinal)) {
            return true;
        }

        var separatorIndex = declaringAssemblyName.IndexOf(',');
        if (separatorIndex <= 0) {
            return false;
        }

        return string.Equals(
            declaringAssemblyName[..separatorIndex],
            assembly.GetName().Name,
            StringComparison.Ordinal);
    }
}
