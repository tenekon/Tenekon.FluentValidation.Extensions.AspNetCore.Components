using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

// This component is highly specialized and tighly coupled with the internals of EditContext.
// The component wants to have an actor edit context with field references of the ancestor edit context, except the event invocations,
//   because the components alone wants to act on OnFieldChanged and OnValidationRequested independently from the ancestor edit context.
// CASCADING PARAMETER DEPENDENCIES:
//   IEditModelValidator provided by EditModelValidatorSubpath or EditModelValidatorRootpath
public abstract class EditModelScopeBase<TDerived> : EditContextualComponentBase<TDerived>, IEditContextualComponentTrait,
    IEditModelScopeBaseTrait where TDerived : EditModelScopeBase<TDerived>, IParameterSetTransitionHandlerRegistryProvider
{
    static EditModelScopeBase()
    {
        RuntimeHelpers.RunClassConstructor(typeof(EditContextualComponentBase<TDerived>).TypeHandle);

        // Because Subpath component is NOT validating, but redirecting plain edit context validation requested notifications,
        // we must not listen to validation requested notifications of root edit context, to prevent double notification.
        TDerived.ParameterSetTransitionHandlerRegistry.RemoveHandler(RefreshRootEditContextEventBindings);

        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            SetIsolatedActorEditContext,
            // ISSUE:
            //  The problem is, that root edit context may become the ancestor edit context, then the ancestor edit references of
            //  field states and properties are copied to new actor edit context, thus it is problematic to occupy counter-based
            //  properties before and then deoccupy counter-based properties after the copying.
            // PROPOSAL (IMPLEMETED):
            //  Copy field references before any mutation of any edit context on every parameters transition.
            //
            // TODO: If we want to make the registry API public, we should consider to make adding position relative to already added
            //  handlers, because now it is sufficient to insert the handler at the start to fulfill the contract required by the above
            //  handler.
            HandlerInsertPosition.After,
            SetProvidedEditContexts);
    }

    internal static Action<EditContextualComponentBaseParameterSetTransition> SetIsolatedActorEditContext { get; } =
        static transition => {
            if (transition.IsDisposing) {
                return;
            }

            var component = Unsafe.As<EditModelScopeBase<TDerived>>(transition.Component);
            var actorEditContextTransition = transition.ActorEditContext;
            var ancestorEditContextTransition = transition.AncestorEditContext;

            if (actorEditContextTransition.IsNewNull) {
                var lastTransition = Unsafe.As<EditModelScopeParameterSetTransition>(component.LastParameterSetTransition);
                if (lastTransition is { IsActorEditContextAncestorDerived: true } && ancestorEditContextTransition.IsNewSame) {
                    // Reuse old actor edit context if it was already derived from the ancestor and the ancestor didn't change.
                    actorEditContextTransition.New = actorEditContextTransition.Old;
                } else {
                    // Create a new actor edit context based on the ancestor model.
                    var newActorEditContext = new EditContext(ancestorEditContextTransition.New.Model);
                    actorEditContextTransition.New = newActorEditContext;

                    // Only copy field references if the ancestor is the direct ancestor.
                    if (component.Ancestor is { IsDirectAncestor: true }) {
                        // Cascade EditContext._fieldStates
                        var editContextFieldStatesMemberAccessor = EditContextAccessor.EditContextFieldStateMapMember;
                        var fieldStates = editContextFieldStatesMemberAccessor.GetValue(ancestorEditContextTransition.New);
                        editContextFieldStatesMemberAccessor.SetValue(newActorEditContext, fieldStates);

                        // Cascade EditContext.Properties
                        EditContextAccessor.GetProperties(newActorEditContext) =
                            EditContextAccessor.GetProperties(ancestorEditContextTransition.New);
                    } /* else:
                       * We MUST NOT cascade field states and properties, because we do not want to have shared field states,
                       * between different validation contexts to have the following behaviour:
                       * <EditForm ...> Context A
                       *   <EditModelScope> // Context B - for demonstation purposes
                       *     <EditModelValidatorRootpath .../> // Writes to A & B
                       *     <EditModelScope> // Context C
                       *       <EditModelValidatorRootpath .../> Writes to A & C
                       *     </EditModelScope>
                       *   </EditModelScope>
                       * </EditForm>
                       */
                }

                var transition2 = Unsafe.As<EditModelScopeParameterSetTransition>(transition);
                transition2.IsActorEditContextAncestorDerived = true;
            }
        };

    bool IEditModelScopeBaseTrait.HasActorEditContextBeenSetExplicitly { get; set; }
    EditContext? IEditModelScopeBaseTrait.ActorEditContext { get; set; }
    object? IEditModelScopeBaseTrait.Model { get; set; }

    EditContext? IEditContextualComponentTrait.ActorEditContext => ((IEditModelScopeBaseTrait)this).ActorEditContext;

    [Parameter]
    public Ancestor? Ancestor { get; set; }

    internal override EditModelScopeParameterSetTransition LastParameterSetTransition =>
        Unsafe.As<EditModelScopeParameterSetTransition>(Unsafe.As<ILastParameterSetTransitionTrait>(this).LastParameterSetTransition);

    protected override async Task OnParametersSetAsync()
    {
        await ((IEditModelScopeBaseTrait)this).OnSubpathParametersSetAsync();
        await base.OnParametersSetAsync();
    }

    internal override EditContextualComponentBaseParameterSetTransition CreateParameterSetTransition() =>
        new EditModelScopeParameterSetTransition();
}
