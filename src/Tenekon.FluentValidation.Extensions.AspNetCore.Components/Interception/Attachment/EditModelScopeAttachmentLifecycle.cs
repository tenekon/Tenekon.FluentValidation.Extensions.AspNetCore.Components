using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Descendant;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Root;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Attachment;

internal static class EditModelScopeAttachmentLifecycle
{
    public static void Attach(EditContext rootEditContext, EditContext actorEditContext)
    {
        if (ReferenceEquals(rootEditContext, actorEditContext)) {
            return;
        }

        var didAttachDescendant = false;
        try {
            if (!IsDescendantRegistered(rootEditContext, actorEditContext)) {
                EditContextPropertyAccessor.s_descendantEditContextSetProperty.AttachValue(rootEditContext, actorEditContext);
                didAttachDescendant = true;
            }

            RootFieldStateMapMutatorFactory.Create(rootEditContext).DoMutation();
            DescendantFieldStateMapMutatorFactory.Create(actorEditContext).DoMutation();
            MirroredFieldStateSynchronizer.AttachExistingMirroredFieldStates(rootEditContext, actorEditContext);

            if (didAttachDescendant) {
                EditContextLifecycleTrace.Emit(
                    EditContextLifecycleTraceEventKind.DescendantAttach,
                    rootEditContext,
                    actorEditContext);
            }
        } catch {
            if (didAttachDescendant && IsDescendantRegistered(rootEditContext, actorEditContext)) {
                EditContextPropertyAccessor.s_descendantEditContextSetProperty.DetachValue(rootEditContext, actorEditContext);
            }

            throw;
        }
    }

    public static void Detach(EditContext rootEditContext, EditContext actorEditContext)
    {
        if (ReferenceEquals(rootEditContext, actorEditContext)) {
            return;
        }

        MirroredFieldStateSynchronizer.DetachMirroredFieldStates(rootEditContext, actorEditContext);

        if (IsDescendantRegistered(rootEditContext, actorEditContext)) {
            EditContextPropertyAccessor.s_descendantEditContextSetProperty.DetachValue(rootEditContext, actorEditContext);
            EditContextLifecycleTrace.Emit(
                EditContextLifecycleTraceEventKind.DescendantDetach,
                rootEditContext,
                actorEditContext);
        }
    }

    private static bool IsDescendantRegistered(EditContext rootEditContext, EditContext actorEditContext) =>
        EditContextPropertyAccessor.s_descendantEditContextSetProperty.TryGetPropertyValue(rootEditContext, out var descendants) &&
        descendants.Contains(actorEditContext);
}

