using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Intent;

internal static class ValidationMessageStorePromotionPolicy
{
    private const string AddMethodName = "Add";
    private const string GetOrCreateMessagesListForFieldMethodName = "GetOrCreateMessagesListForField";

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(ValidationMessageStore))]
    public static bool ShouldPromoteMirroredFieldStateOnCurrentLookup()
    {
        var stackTrace = new StackTrace();

        foreach (var frame in stackTrace.GetFrames() ?? []) {
            if (!StackTraceMethodDescriptorProvider.TryGet(frame, out var methodDescriptor) ||
                !methodDescriptor.IsDeclaredBy(typeof(ValidationMessageStore))) {
                continue;
            }

            if (methodDescriptor.Name == AddMethodName ||
                methodDescriptor.Name == GetOrCreateMessagesListForFieldMethodName) {
                return true;
            }

            if (methodDescriptor.Name == nameof(ValidationMessageStore.Clear)) {
                return false;
            }
        }

        return false;
    }
}
