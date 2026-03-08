using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class EditModelValidatorRootpath : EditModelValidatorBase<EditModelValidatorRootpath>, IEditContextualComponentTrait,
    IParameterSetTransitionHandlerRegistryProvider
{
    static EditModelValidatorRootpath()
    {
        RuntimeHelpers.RunClassConstructor(typeof(EditModelValidatorBase<EditModelValidatorRootpath>).TypeHandle);

        ParameterSetTransitionHandlerRegistryAccessor<EditModelValidatorRootpath>.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            CopyAncestorEditContextFieldReferencesToActorEditContextAction,
            HandlerInsertPosition.After,
            SetProvidedEditContexts);
    }

    static ParameterSetTransitionHandlerRegistry IParameterSetTransitionHandlerRegistryProvider.ParameterSetTransitionHandlerRegistry {
        get;
    } = new();

    private static Action<EditContextualComponentBaseParameterSetTransition>
        CopyAncestorEditContextFieldReferencesToActorEditContextAction { get; } = static transition => {
        if (transition.IsDisposing) {
            return;
        }

        var transition2 = Unsafe.As<EditModelValidatorBaseParameterSetTransition>(transition);
        var ancestorEditContextTransition = transition2.AncestorEditContext;

        if (ancestorEditContextTransition.IsNewDifferent || transition2.ChildContent.IsNewNullStateChanged ||
            transition2.Routes.IsNewNullStateChanged) {
            // We only isolate actor edit context if ChildContent is null and Routes are not, because if Routes is not null,
            // then Subpath already provides event-isolated edit context.
            if (ancestorEditContextTransition.IsNewNonNull && transition2.ChildContent.IsNewNonNull && transition2.Routes.IsNewNull) {
                var actorEditContextTransition = transition2.ActorEditContext;
                // We must re-create the sentinel, to allow correct deinitialiazation of possible non-null old ancestor edit context
                var newActorEditContext = new EditContext(ancestorEditContextTransition.New.Model);
                actorEditContextTransition.New = newActorEditContext;

                /* REVISE: Once (if ever) Rootpath becomes internally a direct descendant (not direct ancestor),
                 * we must cascade field references of the ancestor to the actor edit context.
                 */
            } // else { /* Do not set any actor edit context, thus actor edit context becomes ancestor edit context. */ }  
        } else if (transition2.ActorEditContext.IsOldNonNull) {
            transition.ActorEditContext.New = transition2.ActorEditContext.Old;
        }
    };

    EditContext? IEditContextualComponentTrait.ActorEditContext => AncestorEditContext;

    // TODO: Consolidate duplicate code in EditModelValidatorSubpath
    /* TODO: Make pluggable */
    // protected override void OnAncestorEditContextChanged(EditContextChangedEventArgs args)
    // {
    //     RootEditModelValidatorContext rootEditModelValidatorContext;
    //     if (args.New.Properties.TryGetValue(EditModelValidatorContextLookupKey.Standard, out var validatorContext)) {
    //         if (validatorContext is not RootEditModelValidatorContext rootValidatorContext2) {
    //             throw new InvalidOperationException("Root validator context lookup key was misued from a third-party.");
    //         }
    //         rootEditModelValidatorContext = rootValidatorContext2;
    //     } else {
    //         rootEditModelValidatorContext = new RootEditModelValidatorContext();
    //         args.New.Properties[EditModelValidatorContextLookupKey.Standard] = rootEditModelValidatorContext;
    //     }
    //
    //     rootEditModelValidatorContext.AttachValidatorContext(_leafEditModelValidatorContext);
    //     base.OnAncestorEditContextChanged(args);
    // }


    /* TODO: Make pluggable */
    // protected override void DeinitializeAncestorEditContext()
    // {
    //     var editContext = _ancestorEditContext;
    //     if (editContext is null) {
    //         return;
    //     }
    //
    //     if (!editContext.TryGetEditModelValidatorContext<RootEditModelValidatorContext>(out var rootValidatorContext)) {
    //         throw new InvalidOperationException(
    //             "Root validator context lookup key was removed before the own implementation had the chance to properly detach its validator context.");
    //     }
    //
    //     if (rootValidatorContext.DetachValidatorContext(_leafEditModelValidatorContext)) {
    //         editContext.Properties.Remove(EditModelValidatorContextLookupKey.Standard);
    //     }
    //
    //     base.DeinitializeAncestorEditContext();
    // }
}
