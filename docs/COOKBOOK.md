# Validator Components Cookbook

This cookbook is a task-oriented guide for `Tenekon.FluentValidation.Extensions.AspNetCore.Components`.

It focuses on the consumer-facing usage paths that can be traced directly to the current source and focused tests in this repository. In particular, routed recipes use the `Routes=` parameter on `EditModelValidatorRootpath` or `EditModelValidatorSubpath`, because that is the best-covered consumer entrypoint for routed validation in the current codebase.

## Table of Contents

- [Before You Start](#before-you-start)
- [Recipe 1. Validate the whole form (`EditModelValidatorRootpath`)](#recipe-1-validate-the-whole-form-editmodelvalidatorrootpath)
- [Recipe 2. Validate one nested object (`EditModelValidatorSubpath` with `Model`)](#recipe-2-validate-one-nested-object-editmodelvalidatorsubpath-with-model)
- [Recipe 3. Validate repeated items (`EditModelValidatorSubpath` in a loop)](#recipe-3-validate-repeated-items-editmodelvalidatorsubpath-in-a-loop)
- [Recipe 4. Validate one nested object with an explicit `EditContext` (`EditModelValidatorSubpath` with `EditContext`)](#recipe-4-validate-one-nested-object-with-an-explicit-editcontext-editmodelvalidatorsubpath-with-editcontext)
- [Recipe 5. Combine root and nested validators in one form](#recipe-5-combine-root-and-nested-validators-in-one-form)
- [Recipe 6. Route nested fields back to the parent validator (`Routes`)](#recipe-6-route-nested-fields-back-to-the-parent-validator-routes)
- [Recipe 7. Create an isolated validation region (`EditModelScope`)](#recipe-7-create-an-isolated-validation-region-editmodelscope)
- [About `EditModelValidatorRoutes`](#about-editmodelvalidatorroutes)
- [Tune Validation Behavior](#tune-validation-behavior)
- [Troubleshooting And Diagnostics](#troubleshooting-and-diagnostics)

## Before You Start

### Package setup

Install the package:

```bash
dotnet add package Tenekon.FluentValidation.Extensions.AspNetCore.Components
```

If you want to resolve validators through DI by using `ValidatorType`, install the FluentValidation DI helpers as well:

```bash
dotnet add package FluentValidation.DependencyInjectionExtensions
```

Then register your validators. The easiest path is assembly scanning:

```csharp
using FluentValidation;

builder.Services.AddValidatorsFromAssemblyContaining<CheckoutFormValidator>();
```

`ValidatorType` only requires that the validator type is available through normal DI. Assembly scanning is convenient, but manual service registration also works.

Current package facts:

- status: `1.0-alpha`
- target frameworks: `net8.0`, `net9.0`, `net10.0`

### Imports

Add these imports to `_Imports.razor` or directly in the component that uses the recipes:

```razor
@using Microsoft.AspNetCore.Components.Forms
@using Tenekon.FluentValidation.Extensions.AspNetCore.Components
```

### Shared sample types

The recipes below assume these sample types and validators:

```csharp
using FluentValidation;

public sealed class CheckoutForm
{
    public string? CustomerEmail { get; set; }
    public Address ShippingAddress { get; set; } = new();
    public PaymentStep Payment { get; set; } = new();
    public List<OrderLine> Lines { get; set; } = new() { new() };
}

public sealed class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
}

public sealed class PaymentStep
{
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
}

public sealed class OrderLine
{
    public string? Sku { get; set; }
    public int Quantity { get; set; } = 1;
}

public sealed class CheckoutFormValidator : AbstractValidator<CheckoutForm>
{
    public CheckoutFormValidator()
    {
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.ShippingAddress.Street).NotEmpty();
        RuleFor(x => x.ShippingAddress.City).NotEmpty();
        RuleFor(x => x.Payment.CardholderName).NotEmpty();
        RuleFor(x => x.Payment.CardNumber).NotEmpty().CreditCard();
        RuleForEach(x => x.Lines).SetValidator(new OrderLineValidator());
    }
}

public sealed class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
    }
}

public sealed class PaymentStepValidator : AbstractValidator<PaymentStep>
{
    public PaymentStepValidator()
    {
        RuleFor(x => x.CardholderName).NotEmpty();
        RuleFor(x => x.CardNumber).NotEmpty().CreditCard();
    }
}

public sealed class OrderLineValidator : AbstractValidator<OrderLine>
{
    public OrderLineValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
```

### Choose the right component

| Task | Component | Notes |
| --- | --- | --- |
| Validate the current form model | `EditModelValidatorRootpath` | Best first recipe. |
| Validate one nested object or one repeated item | `EditModelValidatorSubpath` | Requires exactly one of `Model` or `EditContext`. |
| Route nested field changes back to a parent validator | `Routes` on `EditModelValidatorRootpath` or `EditModelValidatorSubpath` | Route the complex object that owns the rendered fields. |
| Create an isolated editing region | `EditModelScope` | This scopes `EditContext`; it does not validate by itself. |

## Recipe 1. Validate the whole form (`EditModelValidatorRootpath`)

### Problem

You want one FluentValidation validator for the current form model.

### Use this when

- your inputs bind to root-level properties of the current form model
- you want the lowest-friction starting point
- you do not need a separate nested validation region yet

### Minimal example

```razor
<EditForm Model="_model">
    <EditModelValidatorRootpath Validator="_validator" />

    <InputText @bind-Value="_model.CustomerEmail" />
    <ValidationMessage For="() => _model.CustomerEmail" />

    <button type="submit">Place order</button>
</EditForm>

@code {
    private readonly CheckoutForm _model = new();
    private readonly CheckoutFormValidator _validator = new();
}
```

DI variant:

```razor
<EditModelValidatorRootpath ValidatorType="typeof(CheckoutFormValidator)" />
```

### What this changes in validation flow

- `EditModelValidatorRootpath` validates the current cascaded form model.
- In this self-closing form, it uses the current form `EditContext` directly.
- On submit, it performs full-model validation.
- On field change, it validates the changed field against the root validator.
- A wrapped `EditModelValidatorRootpath` behaves differently from this self-closing example: with `ChildContent` and no `Routes`, it creates a separate actor `EditContext` for its descendants.

### Failure modes

- You must supply exactly one of `Validator` or `ValidatorType`.
- The component still needs an outer cascaded `EditContext`, usually from `EditForm`.

### Do not use this when

Do not use this alone for nested object fields that need their own validation context. Move to `EditModelValidatorSubpath` or a routed parent validator.

### Next recipe

Next, validate one nested object by giving it its own validator and its own actor `EditContext`.

## Recipe 2. Validate one nested object (`EditModelValidatorSubpath` with `Model`)

### Problem

You want one nested object to validate as its own unit inside a larger form.

### Use this when

- the nested object already exists as a model instance
- that nested object should have its own validator
- you want field changes inside the nested region to validate against that nested validator

### Minimal example

```razor
@{
    var shipping = _model.ShippingAddress;
}

<EditForm Model="_model">
    <EditModelValidatorSubpath Model="shipping"
                               ValidatorType="typeof(AddressValidator)">
        <InputText @bind-Value="shipping.Street" />
        <ValidationMessage For="() => shipping.Street" />

        <InputText @bind-Value="shipping.City" />
        <ValidationMessage For="() => shipping.City" />
    </EditModelValidatorSubpath>
</EditForm>

@code {
    private readonly CheckoutForm _model = new();
}
```

### What this changes in validation flow

- `EditModelValidatorSubpath` creates an actor `EditContext` for `shipping`.
- Descendants inside the component use that nested actor context.
- The nested validator runs against the nested model, not the root model.
- Validation messages remain connected to the shared root validation tree.

### Failure modes

- You must supply exactly one of `Model` or `EditContext`.
- The component still needs an outer `EditForm` or another cascaded `EditContext`.
- If you use `ValidatorType`, the validator type must be registered in DI.

### Do not use this when

Do not use `Model=` if another component already owns the nested `EditContext`. In that case, use the next recipe.

### Next recipe

The same idea works for repeated items in a collection.

## Recipe 3. Validate repeated items (`EditModelValidatorSubpath` in a loop)

### Problem

You want every item in a collection to validate with its own nested validator.

### Use this when

- your form renders repeated nested items
- each item should validate independently
- each item already has its own model instance

### Minimal example

```razor
<EditForm Model="_model">
    @foreach (var line in _model.Lines)
    {
        <EditModelValidatorSubpath Model="line"
                                   ValidatorType="typeof(OrderLineValidator)">
            <InputText @bind-Value="line.Sku" />
            <ValidationMessage For="() => line.Sku" />

            <InputNumber @bind-Value="line.Quantity" />
            <ValidationMessage For="() => line.Quantity" />
        </EditModelValidatorSubpath>
    }
</EditForm>

@code {
    private readonly CheckoutForm _model = new()
    {
        Lines = new() { new(), new() }
    };
}
```

### What this changes in validation flow

- Each loop item gets its own nested actor `EditContext`.
- Each item is validated by `OrderLineValidator`.
- Validation messages still participate in the shared form-level validation view.

### Failure modes

- The loop item itself must be the `Model` you validate.
- Each repeated region still needs exactly one validator source.

### Do not use this when

Do not use this when one parent validator should stay responsible for those nested fields. Use routed parent validation instead.

### Next recipe

If the nested region already has an `EditContext`, supply that context explicitly instead of supplying the model.

## Recipe 4. Validate one nested object with an explicit `EditContext` (`EditModelValidatorSubpath` with `EditContext`)

### Problem

You already have a nested `EditContext`, and you want `EditModelValidatorSubpath` to validate against it directly.

### Use this when

- another component already creates the nested `EditContext`
- you want to keep ownership of that `EditContext`
- you still want the nested validator connected to the shared root validation tree

### Minimal example

```razor
@{
    var shipping = _model.ShippingAddress;
}

<EditForm Model="_model">
    <EditModelValidatorSubpath EditContext="_shippingEditContext"
                               ValidatorType="typeof(AddressValidator)">
        <InputText @bind-Value="shipping.Street" />
        <ValidationMessage For="() => shipping.Street" />

        <InputText @bind-Value="shipping.City" />
        <ValidationMessage For="() => shipping.City" />
    </EditModelValidatorSubpath>
</EditForm>

@code {
    private readonly CheckoutForm _model = new();
    private EditContext _shippingEditContext = default!;

    protected override void OnInitialized()
    {
        _shippingEditContext = new EditContext(_model.ShippingAddress);
    }
}
```

### What this changes in validation flow

- `EditModelValidatorSubpath` uses your explicit nested `EditContext`.
- It does not replace the need for the outer form `EditContext`.
- The nested actor `EditContext` stays stable even if the ancestor `EditContext` changes.

### Failure modes

- `EditModelValidatorSubpath` still requires the outer cascaded `EditContext`.
- `Model` and `EditContext` are mutually exclusive. Supplying both throws.

### Do not use this when

Do not create an explicit `EditContext` unless you already have a reason to own it. `Model=` is simpler when you just need a nested validator.

### Next recipe

Now combine a root validator and a nested validator in one real form.

## Recipe 5. Combine root and nested validators in one form

### Problem

You want one validator for root-level fields and a second validator for a nested region.

### Use this when

- the root model has fields that should stay on the root validator
- one nested object should validate independently
- you want both regions active in the same form

### Minimal example

```razor
@{
    var shipping = _model.ShippingAddress;
}

<EditForm Model="_model">
    <EditModelValidatorRootpath ValidatorType="typeof(CheckoutFormValidator)" />

    <InputText @bind-Value="_model.CustomerEmail" />
    <ValidationMessage For="() => _model.CustomerEmail" />

    <EditModelValidatorSubpath Model="shipping"
                               ValidatorType="typeof(AddressValidator)">
        <InputText @bind-Value="shipping.Street" />
        <ValidationMessage For="() => shipping.Street" />

        <InputText @bind-Value="shipping.City" />
        <ValidationMessage For="() => shipping.City" />
    </EditModelValidatorSubpath>
</EditForm>

@code {
    private readonly CheckoutForm _model = new();
}
```

### What this changes in validation flow

- Root-level fields are still validated by `CheckoutFormValidator`.
- Nested shipping fields are validated by `AddressValidator`.
- Both validators stay connected to the same shared root validation world.

### Failure modes

- Keep root-level fields on the root validator.
- Keep shipping fields inside the subpath region so they use the nested actor `EditContext`.

### Do not use this when

Do not use a nested validator if you want the parent validator to own those nested fields and paths. Use routed parent validation instead.

### Next recipe

Next, keep one parent validator but route nested object fields back to that parent validator.

## Recipe 6. Route nested fields back to the parent validator (`Routes`)

### Problem

You want one parent validator to stay responsible for nested object fields.

### Use this when

- the root validator already contains rules for nested properties
- you want nested inputs to validate against that root validator
- the routed region is made of complex objects that own the fields being edited

### Minimal example

```razor
<EditForm Model="_model">
    <EditModelValidatorRootpath ValidatorType="typeof(CheckoutFormValidator)"
                                Routes="[() => _model.ShippingAddress, () => _model.Payment]">
        <InputText @bind-Value="_model.ShippingAddress.Street" />
        <ValidationMessage For="() => _model.ShippingAddress.Street" />

        <InputText @bind-Value="_model.Payment.CardholderName" />
        <ValidationMessage For="() => _model.Payment.CardholderName" />

        <InputText @bind-Value="_model.Payment.CardNumber" />
        <ValidationMessage For="() => _model.Payment.CardNumber" />
    </EditModelValidatorRootpath>
</EditForm>

@code {
    private readonly CheckoutForm _model = new();
}
```

### What this changes in validation flow

- The validator still runs `CheckoutFormValidator`.
- Routed field changes are mapped back to their full root paths before validation.
- In this example, field changes are validated as `ShippingAddress.Street`, `Payment.CardholderName`, and `Payment.CardNumber` on the root model.

### Failure modes

- Route the complex object that owns the fields you render. Route `() => _model.Payment`, not `() => _model.Payment.CardNumber`.
- Route targets must be non-null and unique.
- The routed region still needs the outer cascaded `EditContext`.

### Do not use this when

Do not use routed parent validation when a nested model should have its own validator and its own validation rules.

### Next recipe

If you want an isolated editing region instead of a different validator, use `EditModelScope`.

## Recipe 7. Create an isolated validation region (`EditModelScope`)

### Problem

You want a separate local `EditContext` region without leaving the shared root validation tree.

### Use this when

- you want descendants to work inside a different local `EditContext`
- you still want messages to stay visible from the outer form
- you want to scope interaction first, then place validators inside that scope

### Minimal example

Default scoped region over the ancestor model:

```razor
<EditForm Model="_model">
    <EditModelScope>
        <EditModelValidatorRootpath ValidatorType="typeof(CheckoutFormValidator)" />

        <InputText @bind-Value="_model.CustomerEmail" />
        <ValidationMessage For="() => _model.CustomerEmail" />
    </EditModelScope>
</EditForm>

@code {
    private readonly CheckoutForm _model = new();
}
```

Scoped region over an explicit nested model:

```razor
@{
    var payment = _model.Payment;
}

<EditForm Model="_model">
    <EditModelScope Model="payment">
        <EditModelValidatorRootpath ValidatorType="typeof(PaymentStepValidator)" />

        <InputText @bind-Value="payment.CardholderName" />
        <ValidationMessage For="() => payment.CardholderName" />

        <InputText @bind-Value="payment.CardNumber" />
        <ValidationMessage For="() => payment.CardNumber" />
    </EditModelScope>
</EditForm>

@code {
    private readonly CheckoutForm _model = new();
}
```

### What this changes in validation flow

- `EditModelScope` creates or reuses a scoped actor `EditContext`.
- The scope itself does not validate anything.
- Validators inside the scope operate against that scoped actor `EditContext`.
- Validation messages from the scope still remain visible to the outer root form.

### Failure modes

- `EditModelScope` accepts zero or one of `Model` and `EditContext`, but never both.
- It still requires an outer cascaded `EditContext`.

### Do not use this when

Do not reach for `EditModelScope` if all you need is a nested validator. `EditModelValidatorSubpath` or routed parent validation is usually simpler.

### Next recipe

The final sections cover the advanced knobs and the most common failure messages.

## About `EditModelValidatorRoutes`

`EditModelValidatorRoutes` is part of the public API, but this cookbook uses the `Routes=` parameter on `EditModelValidatorRootpath` and `EditModelValidatorSubpath` as the primary routed recipe.

That choice is deliberate:

- it is the consumer-facing path covered most directly by the current focused tests
- the parent validator supplies the required validation notifier automatically
- the parent validator also provides the internal direct-ancestor marker used by the library when it renders `EditModelValidatorRoutes`

If you work directly with `EditModelValidatorRoutes`, keep these rules in mind:

- it must be inside `EditModelValidatorRootpath` or `EditModelValidatorSubpath`
- it still requires a cascaded `EditContext`
- its `Routes` entries must resolve to non-null complex objects, not primitive leaf members

## Tune Validation Behavior

### `Validator` vs `ValidatorType`

- `Validator` uses a direct validator instance and does not require DI.
- `ValidatorType` resolves the validator through DI and requires that the type is registered.
- `EditModelValidatorRootpath` requires exactly one of them.
- For cookbook-safe usage, treat `EditModelValidatorSubpath` the same way and supply exactly one validator source yourself.

### `MinimumSeverity`

`MinimumSeverity` is an inclusive threshold.

```razor
<EditModelValidatorRootpath ValidatorType="typeof(CheckoutFormValidator)"
                            MinimumSeverity="Severity.Warning" />
```

With `Severity.Warning`, warnings and errors are kept, while info messages are filtered out. The default is `Severity.Info`, which keeps all severities.

### `SuppressInvalidatableFieldModels`

Use this only for field-change paths when some field owners are not validatable by the current validator and you want to skip those field validations instead of throwing.

```razor
<EditModelValidatorRootpath ValidatorType="typeof(CheckoutFormValidator)"
                            SuppressInvalidatableFieldModels="true" />
```

This setting does not suppress full-model validation.

### `ConfigureValidationStrategy`

Use this for advanced FluentValidation selection, such as rule sets.

```razor
<EditModelValidatorRootpath ValidatorType="typeof(CheckoutFormValidator)"
                            ConfigureValidationStrategy="ConfigureCheckoutRules" />

@code {
    private static void ConfigureCheckoutRules(FluentValidation.Internal.ValidationStrategy<object> strategy)
    {
        strategy.IncludeRuleSets("Checkout");
    }
}
```

For direct and nested field validation, the component already adds `IncludeProperties(...)` before your callback runs.

## Troubleshooting And Diagnostics

### Common errors

| Message shape | Likely cause | Fix |
| --- | --- | --- |
| `requires exactly one parameter Validator ... or ValidatorType ...` | You set both or neither validator source. | Supply exactly one of `Validator` or `ValidatorType`. |
| `requires exactly one non-null Model parameter or non-null EditContext parameter` | `EditModelValidatorSubpath` got both or neither. | Supply exactly one of `Model` or `EditContext`. |
| `requires exactly one non-null Model parameter or non-null EditContext parameter` from `EditModelScope` | You supplied both `Model` and `EditContext`. | Use zero or one, but not both. |
| `requires a cascading parameter of type EditContext` | The component is outside `EditForm` or another `CascadingValue<EditContext>`. | Move it under an outer cascaded `EditContext`. |
| `requires a non-null cascading validation notifier` | Routed validation is being used without a parent validator in scope. | Put routed content under `EditModelValidatorRootpath` or `EditModelValidatorSubpath`, or use the parent `Routes=` parameter. |
| `The model of type ... is unrecognized. Is it registered as a potential route?` | A routed field change came from an object that is not present in `Routes`. | Route the exact complex object that owns the edited fields. |

### Diagnostic helper

If you are debugging scope nesting, you can inspect whether a local `EditContext` participates in a shared root tree:

```csharp
if (EditContextPropertyAccessor.TryGetRootEditContext(localEditContext, out var rootEditContext))
{
    // localEditContext belongs to a shared root validation tree
}
```

### Rules to keep in mind

- Every public component in this package still needs an outer cascaded `EditContext`.
- `EditModelValidatorRootpath` is the default starting point.
- `EditModelValidatorSubpath` is for nested model validation.
- `Routes` are for mapping nested field changes back to a parent validator.
- `EditModelScope` isolates the local editing region, but not the overall root validation visibility.
