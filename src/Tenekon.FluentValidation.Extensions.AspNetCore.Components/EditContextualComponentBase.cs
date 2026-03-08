using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

// ReSharper disable StaticMemberInGenericType
public abstract class EditContextualComponentBase<TDerived> : ComponentBase, IEditContextualComponent, IEditContextualComponentTrait,
    ILastParameterSetTransitionTrait, IDisposable,
    IAsyncDisposable where TDerived : EditContextualComponentBase<TDerived>, IParameterSetTransitionHandlerRegistryProvider
{
    static EditContextualComponentBase()
    {
        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(SetProvidedEditContexts, HandlerInsertPosition.After);
        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(SetDerivedEditContexts, HandlerInsertPosition.After);
        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(RefreshRootEditContextEventBindings, HandlerInsertPosition.After);
        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(RefreshActorEditContextEventBindings, HandlerInsertPosition.After);
        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(PropagateRootEditContext, HandlerInsertPosition.After);
    }

    internal static Action<EditContextualComponentBaseParameterSetTransition> SetProvidedEditContexts { get; } = static transition => {
        if (transition.IsDisposing) {
            return;
        }

        var component = Unsafe.As<EditContextualComponentBase<TDerived>>(transition.Component);

        transition.AncestorEditContext.New = component.AncestorEditContext;
        transition.ActorEditContext.New = ((IEditContextualComponentTrait)component).ActorEditContext;
    };

    internal static Action<EditContextualComponentBaseParameterSetTransition> SetDerivedEditContexts { get; } = static transition => {
        if (transition.IsDisposing) {
            return;
        }

        var component = transition.Component;

        /* We have three definitions of an edit context.
         * 1. The root edit context is the one originating from an edit form.
         * 2. The ancestor edit context is the parent edit context. The ancestor edit context can be the root edit context.
         * 3. The actor edit context is the operating edit context of this validator.
         *    The actor edit context can be either the root edit context, the ancestor edit context or an new instance of an edit context.
         */

        var ancestorEditContext = transition.AncestorEditContext.New;
        if (ancestorEditContext is null) {
            throw new InvalidOperationException(
                $"{component.GetType()} requires a cascading parameter of type {nameof(EditContext)}. For example, you can use {component.GetType()} inside an EditForm component.");
        }

        var actorEditContext = transition.ActorEditContext.NewOrNull;
        if (actorEditContext is null) {
            throw new InvalidOperationException($"{component.GetType()} requires property {nameof(ActorEditContext)} being overriden.");
        }

        EditContext rootEditContext;
        var areActorEditContextAndAncestorEditContextEqual = ReferenceEquals(actorEditContext, ancestorEditContext);
        if (areActorEditContextAndAncestorEditContextEqual) {
            rootEditContext = ancestorEditContext;
        } else {
            if (EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(ancestorEditContext, out var rootEditContext2)) {
                rootEditContext = rootEditContext2;
            } else {
                rootEditContext = ancestorEditContext;
            }
        }

        transition.RootEditContext.New = rootEditContext;
    };

    internal static Action<EditContextualComponentBaseParameterSetTransition> RefreshRootEditContextEventBindings { get; } =
        static transition => {
            var component = (EditContextualComponentBase<TDerived>)transition.Component;
            var lastTransition = component.LastParameterSetTransition;

            var root = transition.RootEditContext;
            if (root.IsNewDifferent) {
                DeinitializeEditContext();

                if (root.IsNewNonNull) {
                    root.New.OnValidationRequested += component.OnValidateModel;
                }

                void DeinitializeEditContext()
                {
                    if (lastTransition.RootEditContext.TryGetNew(out var editContext)) {
                        editContext.OnValidationRequested -= component.OnValidateModel;
                    }
                }
            }
        };

    internal static Action<EditContextualComponentBaseParameterSetTransition> RefreshActorEditContextEventBindings { get; } =
        transition => {
            var component = (EditContextualComponentBase<TDerived>)transition.Component;
            var lastTransition = component.LastParameterSetTransition;

            var root = transition.RootEditContext;

            var actor = transition.ActorEditContext;
            if (actor.IsNewDifferent) {
                DeinitializeEditContext();

                if (actor.IsNewNonNull) {
                    actor.New.OnFieldChanged += component.OnValidateField;
                }

                void DeinitializeEditContext()
                {
                    if (lastTransition.ActorEditContext.TryGetNew(out var editContext)) {
                        editContext.OnFieldChanged -= component.OnValidateField;
                    }
                }
            }

            if (actor.IsNewDifferent || root.IsNewDifferent) {
                DeinitializeEditContext();

                if (transition.IsNewEditContextOfActorAndRootNonNullAndDifferent) {
                    actor.New.OnValidationRequested += component.BubbleUpOnValidationRequested;
                }

                void DeinitializeEditContext()
                {
                    if (lastTransition.IsNewEditContextOfActorAndAncestorNonNullAndDifferent) {
                        lastTransition.ActorEditContext.New.OnValidationRequested -= component.BubbleUpOnValidationRequested;
                    }
                }
            }
        };

    private static Action<EditContextualComponentBaseParameterSetTransition> PropagateRootEditContext { get; } = transition => {
        if (transition.ActorEditContext.IsNewDifferent || transition.RootEditContext.IsNewDifferent) {
            // TODO: && IsFirstTransition: false?
            if (transition.ActorEditContext is { IsOldNonNull: true }) {
                if (transition.RootEditContext.IsOldNonNull) {
                    EditContextLifecycleTrace.Emit(
                        EditContextLifecycleTraceEventKind.RootPropertyDisoccupy,
                        transition.RootEditContext.Old,
                        transition.ActorEditContext.Old);
                }

                EditContextPropertyAccessor.s_rootEditContextProperty.DisoccupyProperty(transition.ActorEditContext.Old);
            }

            if (transition.ActorEditContext.IsNewNonNull && transition.RootEditContext.IsNewNonNull) {
                EditContextPropertyAccessor.s_rootEditContextProperty.OccupyProperty(
                    transition.ActorEditContext.New,
                    transition.RootEditContext.New);
                EditContextLifecycleTrace.Emit(
                    EditContextLifecycleTraceEventKind.RootPropertyOccupy,
                    transition.RootEditContext.New,
                    transition.ActorEditContext.New);
            }
        }
    };

    private bool _didParametersTransitionedOnce;

    [field: AllowNull]
    [field: MaybeNull]
    EditContextualComponentBaseParameterSetTransition ILastParameterSetTransitionTrait.LastParameterSetTransition {
        get => field ??= CreateParameterSetTransition();
        set;
    }

    internal virtual ParameterSetTransitionHandlerRegistry ParameterSetTransitionHandlerRegistry =>
        TDerived.ParameterSetTransitionHandlerRegistry;

    internal virtual EditContextualComponentBaseParameterSetTransition LastParameterSetTransition =>
        Unsafe.As<ILastParameterSetTransitionTrait>(this).LastParameterSetTransition;

    IEditContextualComponentState IEditContextualComponent.ComponentState => LastParameterSetTransition;

    EditContext? IEditContextualComponentTrait.ActorEditContext => null;

    internal virtual EditContext ActorEditContext =>
        LastParameterSetTransition.ActorEditContext.NewOrNull ?? throw new InvalidOperationException(
            $"The {nameof(ActorEditContext)} property hos not been yet initialized. Typically initialized the first time during component initialization.");


    [CascadingParameter]
    internal EditContext? AncestorEditContext { get; set; }

    EditContext IEditContextualComponent.EditContext => LastParameterSetTransition.ActorEditContext.New;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override Task OnParametersSetAsync() => OnParametersTransitioningAsync();

    internal virtual Task OnParametersTransitioningAsync()
    {
        var parametersTransition = CreateParameterSetTransition();
        ConfigureParameterSetTransition(parametersTransition);
        _didParametersTransitionedOnce = true;

        foreach (var registrationItem in ParameterSetTransitionHandlerRegistry.GetRegistrationItems()) {
            registrationItem.Handler(parametersTransition);
        }

        Unsafe.As<ILastParameterSetTransitionTrait>(this).LastParameterSetTransition = parametersTransition;
        return Task.CompletedTask;
    }

    internal virtual EditContextualComponentBaseParameterSetTransition CreateParameterSetTransition() => new();

    internal virtual void ConfigureParameterSetTransition(EditContextualComponentBaseParameterSetTransition transition)
    {
        transition.Component = this;

        var isFirstTransition = !_didParametersTransitionedOnce;
        transition.IsFirstTransition = isFirstTransition;

        transition.ActorEditContext.Old = LastParameterSetTransition.ActorEditContext.NewOrNull;
        transition.AncestorEditContext.Old = LastParameterSetTransition.AncestorEditContext.NewOrNull;
        transition.RootEditContext.Old = LastParameterSetTransition.RootEditContext.NewOrNull;
        transition.ChildContent.Old = LastParameterSetTransition.ChildContent.NewOrNull;

        if (!transition.IsDisposing) {
            transition.ChildContent.New = ChildContent;
        }
    }

    internal virtual void ConfigureDisposalParameterSetTransition(EditContextualComponentBaseParameterSetTransition transition)
    {
        transition.IsDisposing = true;
        ConfigureParameterSetTransition(transition);
    }

    internal virtual void OnValidateModel(object? sender, ValidationRequestedEventArgs args)
    {
    }

    internal virtual void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
    }

    private void BubbleUpOnValidationRequested(object? sender, ValidationRequestedEventArgs e) =>
        LastParameterSetTransition.RootEditContext.New.Validate();

    protected void RenderEditContextualComponent(RenderTreeBuilder builder, RenderFragment? childContent)
    {
        if (LastParameterSetTransition.IsNewEditContextOfActorAndAncestorNonNullAndSame) {
            builder.AddContent(sequence: 0, childContent);
            return;
        }

        var actorEditContext = LastParameterSetTransition.ActorEditContext.NewOrNull;

        // Because OnParametersSetAsync is suspending before assigning actor edit context, premature rendering may occur.  
        if (actorEditContext is null) {
            return;
        }

        builder.OpenComponent<CascadingValue<EditContext>>(sequence: 1);
        // Because edit context instances can stay constant but its model not, we have to set a unique component identity 
        builder.SetKey(new EditContextIdentitySnapshot(actorEditContext, actorEditContext.Model));
        builder.AddComponentParameter(sequence: 2, "IsFixed", value: true);
        builder.AddComponentParameter(sequence: 3, "Value", actorEditContext);
        builder.AddComponentParameter(sequence: 4, nameof(CascadingValue<>.ChildContent), childContent);
        builder.CloseComponent();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => RenderEditContextualComponent(builder, ChildContent);

    #region Disposal Behaviour

    private int _disposalState;

    protected virtual void DisposeCommon()
    {
        if ((Interlocked.Or(ref _disposalState, (int)DisposalStates.CommonDisposed) & (int)DisposalStates.CommonDisposed) != 0) {
            return;
        }

        var parametersTransition = CreateParameterSetTransition();
        ConfigureDisposalParameterSetTransition(parametersTransition);

        foreach (var registrationItem in TDerived.ParameterSetTransitionHandlerRegistry.GetRegistrationItems()) {
            registrationItem.Handler(parametersTransition);
        }
    }

    /// <summary>Called to dispose this instance.</summary>
    /// <param name="disposing"><see langword="true" /> if called within <see cref="IDisposable.Dispose" />.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        if ((Interlocked.Or(ref _disposalState, (int)DisposalStates.SyncDisposed) & (int)DisposalStates.SyncDisposed) != 0) {
            return;
        }

        DisposeCommon();
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if ((Interlocked.Or(ref _disposalState, (int)DisposalStates.AsyncDisposed) & (int)DisposalStates.AsyncDisposed) != 0) {
            return;
        }

        await DisposeAsyncCore();
        DisposeCommon();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    [Flags]
    private enum DisposalStates
    {
        SyncDisposed = 1 << 0,
        AsyncDisposed = 1 << 1,
        CommonDisposed = 1 << 2
    }

    #endregion

    // We want to detect not only changes to the EditContext reference, but also to its associated Model reference.
    private class EditContextIdentitySnapshot(EditContext editContext, object model)
    {
        private readonly EditContext _editContext = editContext;
        private readonly object _model = model;

        public override bool Equals(object? obj) =>
            obj is EditContextIdentitySnapshot identity && ReferenceEquals(_editContext, identity._editContext) &&
            ReferenceEquals(_model, identity._model);

        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(_editContext), RuntimeHelpers.GetHashCode(_model));
    }
}
