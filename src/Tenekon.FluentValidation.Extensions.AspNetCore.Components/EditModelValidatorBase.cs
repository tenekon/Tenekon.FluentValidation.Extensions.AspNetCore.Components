using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public abstract class EditModelValidatorBase<TDerived> : EditContextualComponentBase<TDerived>, IEditModelValidator,
    IEditModelValidationNotifier where TDerived : EditModelValidatorBase<TDerived>, IParameterSetTransitionHandlerRegistryProvider
{
    static EditModelValidatorBase()
    {
        RuntimeHelpers.RunClassConstructor(typeof(EditContextualComponentBase<TDerived>).TypeHandle);

        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(UnsetEditModelValidatorRoutesReference, HandlerInsertPosition.After);

        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            RefreshRootEditContextValidationMessageStore,
            HandlerInsertPosition.After);

        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            RefreshActorEditContextValidationMessageStore,
            HandlerInsertPosition.After);

        return;

        void UnsetEditModelValidatorRoutesReference(EditContextualComponentBaseParameterSetTransition transition)
        {
            if (transition.Routes is { IsNewNullStateChanged: true, IsNewNull: true }) {
                var component = Unsafe.As<EditModelValidatorBase<TDerived>>(transition.Component);
                component._editModelValidatorRoutesReference = null;
            }
        }
    }

    internal static Action<EditContextualComponentBaseParameterSetTransition> RefreshRootEditContextValidationMessageStore { get; } =
        static transition => {
            var component = (EditModelValidatorBase<TDerived>)transition.Component;
            var lastTransition = component.LastParameterSetTransition;

            var root = transition.RootEditContext;
            if (root.IsNewDifferent) {
                DeinitializeValidationMessageStore();

                if (root.IsNewNonNull) {
                    component._rootEditContextValidationMessageStore = new ValidationMessageStore(root.New);
                }

                void DeinitializeValidationMessageStore()
                {
                    if (lastTransition.RootEditContext.IsNewNonNull && component._rootEditContextValidationMessageStore is not null) {
                        component._rootEditContextValidationMessageStore.Clear();
                        component._rootEditContextValidationMessageStore = null;
                    }
                }
            }
        };

    internal static Action<EditContextualComponentBaseParameterSetTransition> RefreshActorEditContextValidationMessageStore { get; } =
        transition => {
            var component = (EditModelValidatorBase<TDerived>)transition.Component;
            var lastTransition = component.LastParameterSetTransition;

            var root = transition.RootEditContext;
            var actor = transition.ActorEditContext;
            if (actor.IsNewDifferent || root.IsNewDifferent) {
                DeinitializeValidationMessageStore();

                if (transition.IsNewEditContextOfActorAndRootNonNullAndDifferent) {
                    component._actorEditContextValidationMessageStore = new ValidationMessageStore(actor.New);
                }

                void DeinitializeValidationMessageStore()
                {
                    if (lastTransition.IsNewEditContextOfActorAndRootNonNullAndDifferent &&
                        component._actorEditContextValidationMessageStore is not null) {
                        component._actorEditContextValidationMessageStore.Clear();
                        component._actorEditContextValidationMessageStore = null;
                    }
                }
            }
        };

    private readonly RenderFragment _renderEditModelValidatorContent;
    private readonly RenderFragment<RenderFragment?> _renderEditContextualComponentFragment;
    private readonly RenderFragment _renderEditModelValidatorRoutesFragment;
    private readonly Action<ValidationStrategy<object>> _applyValidationStrategyAction;
    private readonly Action<object> _captureEditModelValidatorRoutesReferenceAction;
    private bool _havingValidatorSetExplicitly;
    private IValidator? _validator;
    private ServiceScopeSource _serviceScopeSource;
    private EditModelValidatorRoutes? _editModelValidatorRoutesReference;

    private ValidationMessageStore? _rootEditContextValidationMessageStore;
    private ValidationMessageStore? _actorEditContextValidationMessageStore;

    /* TODO: Make pluggable */
    // internal readonly LeafEditModelValidatorContext _leafEditModelValidatorContext = new();

    protected EditModelValidatorBase()
    {
        _renderEditModelValidatorContent = RenderEditModelValidatorContent;
        _renderEditContextualComponentFragment = childContent => builder => RenderEditContextualComponent(builder, childContent);
        _renderEditModelValidatorRoutesFragment = builder => RenderEditModelValidatorRoutes(builder, ChildContent);
        _applyValidationStrategyAction = ApplyValidationStrategy;
        _captureEditModelValidatorRoutesReferenceAction = CaptureEditModelValidatorRoutesReference;
        return;

        void CaptureEditModelValidatorRoutesReference(object reference)
        {
            _editModelValidatorRoutesReference = Unsafe.As<EditModelValidatorRoutes>(reference);
        }
    }

    internal override EditContext ActorEditContext => _editModelValidatorRoutesReference?.ActorEditContext ?? base.ActorEditContext;

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public IValidator? Validator {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get;
        set {
            field = value;
            _havingValidatorSetExplicitly = value is not null;
        }
    }

    [Inject]
    private IServiceProvider? ServiceProvider { get; set; }

    [Inject]
    private ILogger<EditModelValidatorBase<TDerived>>? Logger { get; set; }

    internal override EditModelValidatorBaseParameterSetTransition LastParameterSetTransition =>
        Unsafe.As<EditModelValidatorBaseParameterSetTransition>(
            Unsafe.As<ILastParameterSetTransitionTrait>(this).LastParameterSetTransition);

    [Parameter]
    public Type? ValidatorType { get; set; }

    [Parameter]
    public Action<ValidationStrategy<object>>? ConfigureValidationStrategy { get; set; }

    /// <summary>If true field identifiers with models not handable by validator won't throw.</summary>
    [Parameter]
    public bool SuppressInvalidatableFieldModels { get; set; }

    /// <summary>
    ///     Inclusive minimum severity to be treated as an validation error. The order of the severities is as follow:
    ///     <list type="bullet">
    ///         <item>
    ///             <see cref="Severity.Error" />
    ///         </item>
    ///         <item>
    ///             <see cref="Severity.Warning" />
    ///         </item>
    ///         <item>
    ///             <see cref="Severity.Info" />
    ///         </item>
    ///     </list>
    ///     For example, if severity is equal to <see cref="Severity.Warning" />, then any validation messages with severity
    ///     <see cref="Severity.Warning" /> and <see cref="Severity.Error" /> will pass, but validation messages with severity
    ///     <see cref="Severity.Info" /> not.
    /// </summary>
    [Parameter]
    public Severity MinimumSeverity { get; set; } = Severity.Info;

    [Parameter]
    public Expression<Func<object>>[]? Routes { get; set; }

    private void RenderEditModelValidatorRoutes(RenderTreeBuilder builder, RenderFragment? childContent)
    {
        builder.OpenComponent<EditModelValidatorRoutes>(sequence: 0);
        builder.AddComponentParameter(sequence: 1, nameof(EditModelValidatorRoutes.Routes), Routes);
        builder.AddComponentParameter(sequence: 2, nameof(ChildContent), childContent);
        builder.AddComponentParameter(sequence: 3, nameof(EditModelValidatorRoutes.Ancestor), Ancestor.DirectAncestor);
        builder.AddComponentReferenceCapture(sequence: 3, _captureEditModelValidatorRoutesReferenceAction);
        builder.CloseComponent();
    }

    private void RenderEditModelValidator(RenderTreeBuilder builder, RenderFragment childContent)
    {
        builder.OpenComponent<CascadingValue<IEditModelValidationNotifier>>(sequence: 0);
        builder.AddComponentParameter(sequence: 1, "IsFixed", value: true);
        builder.AddComponentParameter(sequence: 2, "Value", this);
        builder.AddComponentParameter(sequence: 3, nameof(CascadingValue<>.ChildContent), childContent);
        builder.CloseComponent();
    }

    private void RenderEditModelValidatorContent(RenderTreeBuilder builder)
    {
        // PROPOSAL: Isolate the child content by a new edit context (see EditModelValidatorRoutes)
        if (Routes is not null) {
            builder.AddContent(sequence: 0, _renderEditContextualComponentFragment, _renderEditModelValidatorRoutesFragment);
        } else {
            builder.AddContent(sequence: 1, _renderEditContextualComponentFragment, ChildContent);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        RenderEditModelValidator(builder, _renderEditModelValidatorContent);

    private void ApplyValidationStrategy(ValidationStrategy<object> validationStrategy) =>
        ConfigureValidationStrategy?.Invoke(validationStrategy);

    internal virtual void ConfigureValidationContext(ConfigueValidationContextArguments arguments)
    {
        /* TODO: Make pluggable */
        // arguments.ValidationContext.RootContextData[EditModelValidatorContextLookupKey.Standard] =
        //     _leafEditModelValidatorContext;
    }

    protected override async Task OnParametersSetAsync()
    {
        var serviceScopeSource = new ServiceScopeSource(ServiceProvider);

        // We do not allow setting the validator explicitly when the validator type is specified as the same type
        if (_havingValidatorSetExplicitly == ValidatorType is not null) {
            throw new InvalidOperationException(
                $"{GetType()} requires exactly one parameter {nameof(Validator)} of type {nameof(IValidator)} or {nameof(ValidatorType)} of type {nameof(Type)}.");
        }

        var validatorType = ValidatorType;
        // Whenever validator type is not null AND the validator type of the not yet or already materialized validator differs from that
        // validator type, then recreate.
        if (validatorType is not null && _validator?.GetType() != validatorType) {
            if (ServiceScopeSource.TryAcquireInitialization(ref serviceScopeSource)) {
                await DeinitalizeServiceScopeSourceAsync();
                // ReSharper disable once MethodHasAsyncOverload
                DeinitalizeServiceScopeSource();
                ServiceScopeSource.Initialize(ref serviceScopeSource, ref _serviceScopeSource, this);
            }

            _validator = (IValidator)serviceScopeSource.Value.ServiceProvider.GetRequiredService(validatorType);
        } else {
            Debug.Assert(Validator is not null);
            _validator = Validator;
        }

        await base.OnParametersSetAsync();
    }

    internal override async Task OnParametersTransitioningAsync() => await base.OnParametersTransitioningAsync();

    internal override EditModelValidatorBaseParameterSetTransition CreateParameterSetTransition() => new();

    internal override void ConfigureParameterSetTransition(EditContextualComponentBaseParameterSetTransition transition)
    {
        base.ConfigureParameterSetTransition(transition);
        var transition2 = Unsafe.As<EditModelValidatorBaseParameterSetTransition>(transition);
        transition2.Routes.Old = LastParameterSetTransition.Routes.NewOrNull;

        if (!transition2.IsDisposing) {
            transition2.Routes.New = Routes;
        }
    }

    void IEditModelValidationNotifier.EvaluateValidationScope(ValidationScopeContext candidate) =>
        candidate.IsWithinScope =
            (LastParameterSetTransition.RootEditContext.TryGetNew(out var rootEditContext) &&
                ReferenceEquals(rootEditContext, candidate.EditContext)) ||
            (EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(candidate.EditContext, out var candidateRootEditContext) &&
                ReferenceEquals(rootEditContext, candidateRootEditContext));

    private void ClearValidationMessageStores()
    {
        Debug.Assert(_rootEditContextValidationMessageStore is not null);
        _rootEditContextValidationMessageStore.Clear();
        _actorEditContextValidationMessageStore?.Clear();
    }

    private void AddValidationMessageToStores(FieldIdentifier fieldIdentifier, string errorMessage)
    {
        Debug.Assert(_rootEditContextValidationMessageStore is not null);
        _rootEditContextValidationMessageStore.Add(fieldIdentifier, errorMessage);

        // REMINDER: actor edit context validation store can be null,
        // if actor edit context == ancestor edit context == root edit context 
        _actorEditContextValidationMessageStore?.Add(fieldIdentifier, errorMessage);
    }

    void IEditModelValidator.ValidateFullModel() => LastParameterSetTransition.RootEditContext.New.Validate();

    private void ValidateModel()
    {
        Debug.Assert(_validator is not null);
        var actorEditContext = LastParameterSetTransition.ActorEditContext.New;

        var validationContext = ValidationContext<object>.CreateWithOptions(actorEditContext.Model, _applyValidationStrategyAction);
        ConfigureValidationContext(new ConfigueValidationContextArguments(validationContext));
        var validationResult = _validator.Validate(validationContext);

        ClearValidationMessageStores();
        foreach (var error in validationResult.Errors) {
            if (error.Severity > MinimumSeverity) {
                continue;
            }

            var fieldIdentifier = FieldIdentifierHelper.DeriveFieldIdentifier(actorEditContext.Model, error.PropertyName);
            AddValidationMessageToStores(fieldIdentifier, error.ErrorMessage);
        }

        actorEditContext.NotifyValidationStateChanged();
    }

    internal override void OnValidateModel(object? sender, ValidationRequestedEventArgs args) => ValidateModel();

    internal void NotifyModelValidationRequested(EditModelModelValidationRequestedArgs args)
    {
        // A. Whenever an actor edit context of a direct descendant of EditModelValidatorRoutes fires OnValidationRequested,
        //    it bubbles up to the root edit context, triggering OnValidateModel(object? sender, ValidationRequestedEventArgs args)
        //    and implicitly ValidateModel() of any validator component associated with the root edit context,
        //    except EditModelValidatorRoutes. This is because they do not subscribe to OnValidationRequested of the root edit context
        //    to avoid a second invocation of ValidateModel().
        // An additional safety net: To prevent a potential second invocation of ValidateModel(), we return early if the original source of
        // the event is reference-equal to the root edit context, since that instance already handles OnValidationRequested for
        // the root edit context.
        if (ReferenceEquals(LastParameterSetTransition.RootEditContext.NewOrNull, args.OriginalSource)) {
            return;
        }

        ValidateModel();
    }

    void IEditModelValidationNotifier.NotifyModelValidationRequested(EditModelModelValidationRequestedArgs args) =>
        NotifyModelValidationRequested(args);

    void IEditModelValidator.Validate() => ValidateModel();

    // TODO: Removable?
    private ValidationResult Validate(ValidationContext<object> validationContext)
    {
        Debug.Assert(_validator is not null);
        try {
            return _validator.Validate(validationContext);
        } catch (InvalidOperationException error) {
            throw new EditModelValidationException(
                $"{error.Message} Consider to make use of {nameof(EditModelValidatorSubpath)}, {nameof(EditModelValidatorRoutes)} or similiar.",
                error);
        }
    }

    private void ValidateDirectField(FieldIdentifier fieldIdentifier)
    {
        Debug.Assert(_validator is not null);

        if (SuppressInvalidatableFieldModels && !_validator.CanValidateInstancesOfType(fieldIdentifier.Model.GetType())) {
            Logger?.LogWarning(
                "Direct field identifier validation was supressed, because its model is invalidatable: {}",
                fieldIdentifier.Model.GetType());
            return;
        }

        var validationContext = ValidationContext<object>.CreateWithOptions(fieldIdentifier.Model, ApplyValidationStrategy2);
        ConfigureValidationContext(new ConfigueValidationContextArguments(validationContext));
        var validationResult = _validator.Validate(validationContext);

        ClearValidationMessageStores();
        foreach (var error in validationResult.Errors) {
            if (error.Severity > MinimumSeverity) {
                continue;
            }

            AddValidationMessageToStores(fieldIdentifier, error.ErrorMessage);
        }

        LastParameterSetTransition.ActorEditContext.New.NotifyValidationStateChanged();
        return;

        void ApplyValidationStrategy2(ValidationStrategy<object> validationStrategy)
        {
            validationStrategy.IncludeProperties(fieldIdentifier.FieldName);
            _applyValidationStrategyAction.Invoke(validationStrategy);
        }
    }

    void IEditModelValidationNotifier.NotifyDirectFieldValidationRequested(EditModelDirectFieldValidationRequestedArgs args) =>
        ValidateDirectField(args.FieldIdentifier);

    void IEditModelValidator.ValidateDirectField(FieldIdentifier fieldIdentifier) => ValidateDirectField(fieldIdentifier);

    private void ValidateNestedField(FieldIdentifier fullFieldPath, FieldIdentifier subFieldIdentifier)
    {
        Debug.Assert(_validator is not null);

        if (SuppressInvalidatableFieldModels && !_validator.CanValidateInstancesOfType(fullFieldPath.Model.GetType())) {
            Logger?.LogWarning(
                "Nested field identifier validation was supressed, because its model is invalidatable: {}",
                fullFieldPath.Model.GetType());
            return;
        }

        var validationContext = ValidationContext<object>.CreateWithOptions(fullFieldPath.Model, ApplyValidationStrategy2);
        ConfigureValidationContext(new ConfigueValidationContextArguments(validationContext));
        var validationResult = _validator.Validate(validationContext);

        ClearValidationMessageStores();
        foreach (var error in validationResult.Errors) {
            if (error.Severity > MinimumSeverity) {
                continue;
            }
            AddValidationMessageToStores(subFieldIdentifier, error.ErrorMessage);
        }

        LastParameterSetTransition.ActorEditContext.New.NotifyValidationStateChanged();
        return;

        void ApplyValidationStrategy2(ValidationStrategy<object> validationStrategy)
        {
            validationStrategy.IncludeProperties(fullFieldPath.FieldName);
            _applyValidationStrategyAction.Invoke(validationStrategy);
        }
    }

    internal override void OnValidateField(object? sender, FieldChangedEventArgs e) => ValidateDirectField(e.FieldIdentifier);

    void IEditModelValidationNotifier.NotifyNestedFieldValidationRequested(EditModelNestedFieldValidationRequestedArgs args) =>
        ValidateNestedField(args.FullFieldPath, args.SubFieldIdentifier);

    void IEditModelValidator.ValidateNestedField(FieldIdentifier fullFieldPath, FieldIdentifier subFieldIdentifier) =>
        ValidateNestedField(fullFieldPath, subFieldIdentifier);

    private void DeinitalizeServiceScopeSource()
    {
        if (ServiceScopeSource.TryAcquireSyncDisposal(ref _serviceScopeSource)) {
            _serviceScopeSource.Dispose();
        }
    }

    private async Task DeinitalizeServiceScopeSourceAsync()
    {
        if (ServiceScopeSource.TryAcquireAsyncDisposal(ref _serviceScopeSource)) {
            await _serviceScopeSource.DisposeAsync();
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await DeinitalizeServiceScopeSourceAsync();
        await base.DisposeAsyncCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) {
            DeinitalizeServiceScopeSource();
        }

        base.Dispose(disposing);
    }

    private struct ServiceScopeSource(IServiceProvider? serviceProvider) : IDisposable, IAsyncDisposable
    {
        private readonly IServiceProvider? _serviceProvider = serviceProvider;
        private AsyncServiceScope? _asyncServiceScope;
        private int _state;

        public readonly AsyncServiceScope Value {
            get {
                if (_state != (int)States.Initialized) {
                    throw new InvalidOperationException("Service scope is either not initialized or has already been dispsed.");
                }

                return _asyncServiceScope ?? throw new InvalidOperationException("Although the service scope is initialized, it is null.");
            }
        }

        public static bool TryAcquireInitialization(ref ServiceScopeSource source)
        {
            if ((Interlocked.Or(ref source._state, (int)States.Initialized) & (int)States.Initialized) != 0) {
                return false;
            }

            return true;
        }

        public static void Initialize(ref ServiceScopeSource source, ref ServiceScopeSource target, object caller)
        {
            if (source._serviceProvider is null) {
                throw new InvalidOperationException(
                    $"{caller.GetType()} requires a dependency injection available value of type {nameof(IValidator)}.");
            }

            source._asyncServiceScope ??= source._serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
            target = source;
        }

        public static bool TryAcquireSyncDisposal(ref ServiceScopeSource source) =>
            (Interlocked.Or(ref source._state, (int)States.SyncDisposed) & (int)(States.SyncDisposed | States.Initialized)) ==
            (int)States.Initialized;

        public static bool TryAcquireAsyncDisposal(ref ServiceScopeSource source) =>
            (Interlocked.Or(ref source._state, (int)States.AsyncDisposed) & (int)(States.AsyncDisposed | States.Initialized)) ==
            (int)States.Initialized;

        public readonly void Dispose() => _asyncServiceScope?.Dispose();

        public readonly ValueTask DisposeAsync()
        {
            if (!_asyncServiceScope.HasValue) {
                return ValueTask.CompletedTask;
            }

            return _asyncServiceScope.Value.DisposeAsync();
        }

        [Flags]
        private enum States
        {
            Initialized = 1 << 0,
            SyncDisposed = 1 << 1,
            AsyncDisposed = 1 << 2
        }
    }
}
