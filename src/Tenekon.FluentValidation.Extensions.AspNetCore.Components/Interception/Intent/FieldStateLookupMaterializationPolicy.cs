using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Intent;

internal static class FieldStateLookupMaterializationPolicy
{
    private const string AddMethodName = "Add";
    private const string ClearMethodName = "Clear";
    private const string GetFieldStateMethodName = "GetFieldState";
    private const string GetOrAddFieldStateMethodName = "GetOrAddFieldState";
    private const string GetValidationMessagesMethodName = "GetValidationMessages";
    private const string NotifyFieldChangedMethodName = "NotifyFieldChanged";

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(EditContext))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(ValidationMessageStore))]
    public static bool ShouldMaterializeOnCurrentLookupMiss()
    {
        if (ForcedFieldStateMaterializationScope.IsActive) {
            return true;
        }

        var stackTrace = new StackTrace();
        var sawReadOnlyLookup = false;
        var sawCreateLookup = false;

        foreach (var frame in stackTrace.GetFrames() ?? []) {
            if (!StackTraceMethodDescriptorProvider.TryGet(frame, out var methodDescriptor)) {
                continue;
            }

            var isEditContextIterator = methodDescriptor.IsEditContextIterator(GetValidationMessagesMethodName);

            if (methodDescriptor.IsDeclaredBy(typeof(ValidationMessageStore))) {
                if (methodDescriptor.Name == AddMethodName) {
                    sawCreateLookup = true;
                    continue;
                }

                if (methodDescriptor.Name == ClearMethodName) {
                    sawReadOnlyLookup = true;
                }

                continue;
            }

            if (!methodDescriptor.IsDeclaredBy(typeof(EditContext)) && !isEditContextIterator) {
                continue;
            }

            if (methodDescriptor.Name == NotifyFieldChangedMethodName ||
                methodDescriptor.Name == GetOrAddFieldStateMethodName) {
                sawCreateLookup = true;
                continue;
            }

            if (methodDescriptor.Name == GetFieldStateMethodName ||
                methodDescriptor.Name == GetValidationMessagesMethodName ||
                isEditContextIterator) {
                sawReadOnlyLookup = true;
            }
        }

        if (sawCreateLookup) {
            return true;
        }

        if (sawReadOnlyLookup) {
            return false;
        }

        return false;
    }
}

