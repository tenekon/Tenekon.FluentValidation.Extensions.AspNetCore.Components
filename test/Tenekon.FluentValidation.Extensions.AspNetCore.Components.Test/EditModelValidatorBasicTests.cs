using System.Linq.Expressions;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

#pragma warning disable xUnit1042
public record ValidatorTestCase<TEditModelValidator>(
    // ReSharper disable once NotAccessedPositionalProperty.Global
    string Name,
    Action<ComponentParameterCollectionBuilder<TEditModelValidator>, Type, object, EditContext> CustomizeParameters)
    where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider;

public static class EditModelValidatorBasicTestCases
{
    private static IValidator CreateValidator(Type validatorType) =>
        (IValidator)(Activator.CreateInstance(validatorType) ?? throw new InvalidOperationException());

    public static IEnumerable<object[]> All => [
        [
            new ValidatorTestCase<EditModelValidatorRootpath>(
                "RootpathWithValidatorType",
                (p, validatorType, model, ctx) => { p.Add(x => x.ValidatorType, validatorType); })
        ],
        [
            new ValidatorTestCase<EditModelValidatorRootpath>(
                "RootpathWithValidatorInstance",
                (p, validatorType, model, ctx) => { p.Add(x => x.Validator, CreateValidator(validatorType)); })
        ],
        [
            new ValidatorTestCase<EditModelValidatorSubpath>(
                "SubpathWithModelAndValidatorType",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.ValidatorType, validatorType);
                    p.Add(x => x.Model, model);
                })
        ],
        [
            new ValidatorTestCase<EditModelValidatorSubpath>(
                "SubpathWithEditContextAndValidatorType",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.ValidatorType, validatorType);
                    p.Add(x => x.EditContext, ctx);
                })
        ],
        [
            new ValidatorTestCase<EditModelValidatorSubpath>(
                "SubpathWithModelAndValidatorInstance",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.Validator, CreateValidator(validatorType));
                    p.Add(x => x.Model, model);
                })
        ],
        [
            new ValidatorTestCase<EditModelValidatorSubpath>(
                "SubpathWithEditContextAndValidatorInstance",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.Validator, CreateValidator(validatorType));
                    p.Add(x => x.EditContext, ctx);
                })
        ]
    ];
}

public class EditModelValidatorBasicTests : TestContext
{
    public EditModelValidatorBasicTests() => Services.AddValidatorsFromAssemblyContaining<AssemblyMarker>(includeInternalTypes: true);

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void UsingActorEditContext_ValidModel_ValidationReturnsTrue<TEditModelValidator>(ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model("valid");
        var rootEditContext = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(rootEditContext);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, rootEditContext);
        });

        var actorEditContext = cut.Instance.ActorEditContext;

        var validationReqeustedEvent = EditContextAccessor.GetOnValidationRequested(actorEditContext);
        validationReqeustedEvent.ShouldNotBeNull();
        validationReqeustedEvent.GetInvocationList().Length.ShouldBe(1);

        var fieldChangedEvent = EditContextAccessor.GetOnFieldChanged(actorEditContext);
        fieldChangedEvent.ShouldNotBeNull();
        fieldChangedEvent.GetInvocationList().Length.ShouldBe(1);

        actorEditContext.Validate().ShouldBeTrue();

        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContext, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContext, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void UsingRootEditContext_ValidModel_ValidationReturnsTrue<TEditModelValidator>(ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model("valid");
        var rootEditContext = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(rootEditContext);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, rootEditContext);
        });

        var validationReqeustedEvent = EditContextAccessor.GetOnValidationRequested(rootEditContext);
        validationReqeustedEvent.ShouldNotBeNull();
        validationReqeustedEvent.GetInvocationList().Length.ShouldBe(1);

        rootEditContext.Validate().ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void UsingActorEditContext_InvalidModel_ValidationReturnsFalse<TEditModelValidator>(
        ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model("FAILURE");
        var rootEditContext = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(rootEditContext);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, rootEditContext);
        });

        var actorEditContext = cut.Instance.ActorEditContext;
        actorEditContext.Validate().ShouldBeFalse();
        actorEditContext.GetValidationMessages().Count().ShouldBe(1);

        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContext, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContext, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void UsingRootEditContext_InvalidModel_ValidationReturnsFalse<TEditModelValidator>(
        ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model("FAILURE");
        var rootEditContext = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(rootEditContext);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, rootEditContext);
        });

        rootEditContext.Validate().ShouldBeFalse();
        rootEditContext.GetValidationMessages().Count().ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void ValidModel_DirectFieldValidationReturnsFalse<TEditModelValidator>(ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model("FAILURE");
        var rootEditContext = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(rootEditContext);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, rootEditContext);
        });

        var modelFieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var actorEditContex = cut.Instance.ActorEditContext;
        actorEditContex.NotifyFieldChanged(modelFieldIdentifier);
        rootEditContext.IsValid(modelFieldIdentifier).ShouldBeFalse();

        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContex, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContex, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void ValidModel_NestedFieldValidationReturnsFalse<TEditModelValidator>(ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model { Child = { Field1 = "FAILURE" } };
        var rootEditContext = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(rootEditContext);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, rootEditContext);
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
        });

        var subPath = cut.FindComponent<EditModelValidatorRoutes>();
        var modelFieldIdentifier = FieldIdentifier.Create(() => model.Child.Field1);
        var subPathActorEditContext = subPath.Instance.ActorEditContext;
        subPathActorEditContext.NotifyFieldChanged(modelFieldIdentifier);
        rootEditContext.IsValid(modelFieldIdentifier).ShouldBeFalse();

        var cutActorEditContext = cut.Instance.ActorEditContext;
        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(cutActorEditContext, out _, out var cutActorEditContextCounter)
            .ShouldBeTrue();
        cutActorEditContextCounter.ShouldBe(expected: 2);

        EditContextPropertyAccessor.s_rootEditContextProperty
            .TryGetPropertyValue(subPathActorEditContext, out _, out var subPathActorEditContextCounter)
            .ShouldBeTrue();
        subPathActorEditContextCounter.ShouldBe(expected: 2);

        DisposeComponents();
        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(cutActorEditContext, out _).ShouldBeFalse();
        EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(subPathActorEditContext, out _).ShouldBeFalse();
    }
}
