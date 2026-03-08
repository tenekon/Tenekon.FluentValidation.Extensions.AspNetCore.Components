using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Attachment;

internal static class MirroredRootFieldStateRegistry
{
    private sealed class RootFieldStateSet
    {
        public HashSet<FieldIdentifier> MirroredOnlyFields { get; } = [];
    }

    private static readonly ConditionalWeakTable<EditContext, RootFieldStateSet> s_rootFieldStates = new();

    public static void MarkMirroredOnly(EditContext rootEditContext, FieldIdentifier fieldIdentifier)
    {
        var mirroredFields = s_rootFieldStates.GetValue(rootEditContext, static _ => new RootFieldStateSet());
        mirroredFields.MirroredOnlyFields.Add(fieldIdentifier);
    }

    public static void PromoteToLocal(EditContext rootEditContext, FieldIdentifier fieldIdentifier)
    {
        if (s_rootFieldStates.TryGetValue(rootEditContext, out var mirroredFields)) {
            mirroredFields.MirroredOnlyFields.Remove(fieldIdentifier);
        }
    }

    public static bool IsMirroredOnly(EditContext rootEditContext, FieldIdentifier fieldIdentifier)
        => s_rootFieldStates.TryGetValue(rootEditContext, out var mirroredFields) &&
           mirroredFields.MirroredOnlyFields.Contains(fieldIdentifier);

    public static void Forget(EditContext rootEditContext, FieldIdentifier fieldIdentifier)
    {
        if (s_rootFieldStates.TryGetValue(rootEditContext, out var mirroredFields)) {
            mirroredFields.MirroredOnlyFields.Remove(fieldIdentifier);
        }
    }
}
