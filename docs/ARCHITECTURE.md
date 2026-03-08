<!-- omit from toc -->
# Validator Components Architecture [![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components?label=Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)

This document describes the current architecture of the validator components.
It is the place for component roles, edit-context relationships, and validation flow at the architectural level.

For worked usage examples, see [COOKBOOK.md](COOKBOOK.md).
For design rationale, see [MOTIVATION.md](MOTIVATION.md).

<!-- omit from toc -->
## Table of Contents

- [Problem Overview](#problem-overview)
- [Behavior of EditContext](#behavior-of-editcontext)
- [Field Identifier Semantics](#field-identifier-semantics)
- [Component Specification](#component-specification)
  - [`EditModelValidatorRootpath`](#editmodelvalidatorrootpath)
  - [`EditModelValidatorSubpath`](#editmodelvalidatorsubpath)
  - [`EditModelValidatorRoutes`](#editmodelvalidatorroutes)
  - [`EditModelScope`](#editmodelscope)

## Problem Overview

FluentValidation and Blazor fit naturally together as long as the form surface and the validation surface are the same surface.

The difficulty starts when a form is decomposed:

1. the input may be rendered in a descendant component
2. the field may belong to a descendant model instance
3. the validator may still need to validate against an ancestor or root model path

At that point, plain form-level validation is no longer enough.
The problem is not only component nesting.
The problem is the loss of root-relative path information once field interaction is observed on descendant model instances.

The proposal in this package is therefore modular.
Instead of one validator component trying to cover every case, the package separates four roles:

1. validate the form root
2. validate an explicit local model
3. route descendant field changes back to an ancestor path
4. introduce a local actor edit context without becoming a validator

## Behavior of EditContext

Every public component in this package requires a cascading `EditContext`.
That nearest cascaded context is the ancestor edit context.

The package then works with three edit-context roles:

1. the ancestor edit context: the nearest `EditContext` received through Blazor cascading parameters
2. the actor edit context: the `EditContext` on which the current component operates locally
3. the root edit context: the `EditContext` that owns the current validation scope

The actor edit context is the context whose field-change events the component observes.
It is also the context the component may re-cascade to descendants.
Depending on the component and parameter combination, the actor edit context may be:

1. the same instance as the ancestor edit context
2. a new `EditContext` created from a supplied model
3. an explicit `EditContext` supplied by parameter
4. an isolated routing or scoping context derived from the ancestor model

In the validator components there is one additional practical distinction:
the component's local actor edit context is the actor it uses for its own validation logic,
while the effective actor edit context exposed to descendants may be replaced by an internal routed actor when `Routes` is active.

The root edit context is resolved from the ancestor chain.
If actor and ancestor are the same instance, the ancestor edit context is the root edit context.
If actor and ancestor differ, the component first checks whether the ancestor edit context already carries a propagated root marker.
If such a marker exists, it determines the root.
Otherwise the ancestor edit context itself becomes the root.

After the root has been resolved, the package propagates that root onto the current actor edit context.
This propagation is actor-scoped and ref-counted.
It gives descendants a stable way to recover the governing root edit context from the actor edit context they currently receive.

Child content receives a new `CascadingValue<EditContext>` only when actor and ancestor differ.
If actor and ancestor are the same instance, child content continues on the existing cascade.
If they differ, child content is re-cascaded under the actor edit context.

Validation entry points are intentionally split:

1. model validation is root-driven
2. field validation is actor-driven

Concretely:

1. root `OnValidationRequested` drives full-model validation
2. actor `OnFieldChanged` drives field validation
3. when actor and root differ, actor `OnValidationRequested` bubbles upward to `root.Validate()`

Validation visibility follows the same structure.
Validator-produced results are visible from the effective root edit context.
If a validator itself operates through a distinct actor edit context, the same results are also visible from that actor edit context.
If actor and root are the same instance, there is only one visible message surface.

## Field Identifier Semantics

Consider the following model graph:

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

var a = new A();

record A(string? AB = null)
{
    public string? AB { get; set; } = AB;

    [field: AllowNull]
    [field: MaybeNull]
    public B B { get => field ??= new B(); }
}

record B(string? BA = null)
{
    public string? BA { get; set; } = BA;
}

var topLevel = FieldIdentifier.Create(() => a.AB);
var nested = FieldIdentifier.Create(() => a.B.BA);
```

The resulting identifiers mean:

1. `FieldIdentifier.Create(() => a.AB)` yields `Model = a` and `FieldName = "AB"`
2. `FieldIdentifier.Create(() => a.B.BA)` yields `Model = a.B` and `FieldName = "BA"`

This is the decisive constraint.
A nested field identifier already points at the descendant owner.
It does not, by itself, preserve the full root-relative path.

That has two architectural consequences:

1. direct field validation can operate on the incoming `FieldIdentifier` as-is
2. ancestor-rooted validation needs a routing step that reconstructs the full ancestor path first

This package therefore distinguishes between two different path domains:

1. route-registration paths
2. validation-result projection paths

Route-registration paths are the expressions configured through `Routes`.
They are currently simple member-accessor expressions over non-null reference-typed objects.
Indexer expressions such as `() => model.List[0]` are not part of the current route-registration contract.

Validation-result projection paths are the property paths emitted by FluentValidation during full-model validation and then projected back into Blazor `FieldIdentifier`s.
Those paths may contain dotted and indexed segments.
That is a separate concern from route registration.

Direct-field and routed nested-field validation follow a different message-association rule.
Those flows validate against the incoming field identifier or the reconstructed full ancestor path, but they still report messages back through the field identifier chosen by the validator scope itself.

The purpose of `EditModelValidatorRoutes` is therefore precise:
it reconstructs full ancestor-model paths for descendant field changes before delegating validation upward.

## Component Specification

The package currently exposes four public primitives.
The clauses below describe their current behavior in the same order so that the differences are easy to compare.

1. `EditModelValidatorRootpath`
2. `EditModelValidatorSubpath`
3. `EditModelValidatorRoutes`
4. `EditModelScope`

### `EditModelValidatorRootpath`

- **Role:** `EditModelValidatorRootpath` is the validator whose full-model target is the model carried by the current ancestor edit context.
- **Preconditions:** It requires a cascading ancestor `EditContext` and exactly one validator source: `Validator`, or a `ValidatorType` resolvable from dependency injection.
- **Root Edit Context:** The root edit context is resolved by the shared rule defined above. If the local actor edit context and ancestor edit context are the same instance, the ancestor is the root; otherwise an already propagated root marker on the ancestor wins, and if none exists the ancestor becomes the root.
- **Local Actor Edit Context:** Its local actor edit context is conditional. If `ChildContent` is present and `Routes` is null, it creates a new `EditContext` from the ancestor model. Otherwise its local actor edit context remains the ancestor edit context. When rootpath creates a derived local actor, that actor is reused across plain rerenders and recreated when the ancestor edit context changes.
- **Effective Actor Edit Context Exposed To Descendants:** If `Routes` is null, descendants continue under the local actor edit context. When the local actor edit context and ancestor edit context are the same instance, no new `CascadingValue<EditContext>` is introduced. If `Routes` is non-null, descendants receive the actor edit context of the internal `EditModelValidatorRoutes` child.
- **Validator Ownership:** It owns one validator instance resolved from `Validator` or `ValidatorType`. If `ValidatorType` is used, dependency injection must be able to resolve it. It also owns a root-scoped `ValidationMessageStore`, and when its local actor edit context differs from the root it owns an additional actor-scoped `ValidationMessageStore` on that local actor edit context.
- **Model Validation:** Full-model validation is root-driven. When validation executes, rootpath validates the model of its local actor edit context. If that local actor edit context differs from the root, actor-side `OnValidationRequested` first bubbles upward to `root.Validate()`.
- **Field Validation:** Field validation is actor-driven. Direct field changes observed on the local actor edit context are validated directly. If `Routes` is active, descendant field changes are translated by the routed child and delegated back as direct-field or nested-field validation requests.
- **Validation Message Visibility:** Validator-produced messages are always written to the root-scoped store. When the local actor edit context differs from the root, the same messages are also written to the actor-scoped store owned by rootpath.
- **Invalid Or Excluded Cases:** Missing the cascading ancestor `EditContext` is invalid. Supplying both `Validator` and `ValidatorType`, or neither of them, is invalid. `Routes` does not redefine the model used for full-model validation.

### `EditModelValidatorSubpath`

- **Role:** `EditModelValidatorSubpath` is the validator whose primary model is an explicit local model selected through `Model` or `EditContext`.
- **Preconditions:** It requires a cascading ancestor `EditContext` and exactly one non-null local actor source: `Model` or `EditContext`.
- **Root Edit Context:** The root edit context is resolved by the shared rule defined above. If the local actor edit context and ancestor edit context are the same instance, the ancestor is the root; otherwise an already propagated root marker on the ancestor wins, and if none exists the ancestor becomes the root.
- **Local Actor Edit Context:** If `Model` is supplied and is not reference-equal to the current actor model, subpath creates a new `EditContext(Model)`. If `EditContext` is supplied, that context becomes the local actor edit context. When the local actor edit context is model-backed and the model reference stays the same, it is reused across plain rerenders and across changes to the outer cascading ancestor edit context.
- **Effective Actor Edit Context Exposed To Descendants:** If `Routes` is null, descendants receive subpath's local actor edit context. If `Routes` is non-null, descendants receive the actor edit context of the internal `EditModelValidatorRoutes` child instead.
- **Validator Ownership:** Subpath validates through its own configured validator source. Exactly one of `Validator` or `ValidatorType` may be supplied. If both are omitted, the current implementation derives a fallback service type of `IValidator<TActorModel>` from the current local actor model type, assigns that to `ValidatorType`, and resolves it through dependency injection. Subpath also owns a root-scoped `ValidationMessageStore`, plus an actor-scoped `ValidationMessageStore` when its local actor edit context differs from the root.
- **Model Validation:** Full-model validation is root-driven. When validation executes, subpath validates the model of its local actor edit context. If that local actor edit context differs from the root, actor-side `OnValidationRequested` first bubbles upward to `root.Validate()`.
- **Field Validation:** Field validation is actor-driven. Direct field changes observed on the local actor edit context are validated directly. If `Routes` is active, descendant field changes are translated by the routed child and delegated back as direct-field or nested-field validation requests.
- **Validation Message Visibility:** Validator-produced messages are always written to the root-scoped store. When the local actor edit context differs from the root, the same messages are also written to the actor-scoped store owned by subpath.
- **Invalid Or Excluded Cases:** Missing the cascading ancestor `EditContext` is invalid. Supplying both non-null local actor sources, or neither of them, is invalid. Supplying both `Validator` and `ValidatorType` is invalid. If both validator source parameters are omitted and dependency injection cannot resolve the fallback service type `IValidator<TActorModel>` for the current local actor model, initialization fails. `Routes` does not redefine the model used for full-model validation.

### `EditModelValidatorRoutes`

- **Role:** `EditModelValidatorRoutes` is a routing scope owned by a surrounding validator. It is not a standalone validator root.
- **Preconditions:** It requires a cascading ancestor `EditContext` and a surrounding validator scope in the same effective root scope. In normal usage that surrounding validator scope is provided by `EditModelValidatorRootpath` or `EditModelValidatorSubpath`.
- **Root Edit Context:** The root edit context is resolved by the shared rule defined above. If the local actor edit context and ancestor edit context are the same instance, the ancestor is the root; otherwise an already propagated root marker on the ancestor wins, and if none exists the ancestor becomes the root.
- **Local Actor Edit Context:** It creates a new local actor `EditContext` from the ancestor model. When that actor was previously derived from the same ancestor `EditContext`, it is reused across plain rerenders; when the ancestor `EditContext` changes, the routed actor is recreated.
- **Effective Actor Edit Context Exposed To Descendants:** Descendants receive the routed actor edit context created by `EditModelValidatorRoutes`.
- **Validator Ownership:** It owns no validator and no validator-owned `ValidationMessageStore`. Its validation work is delegation upward to the surrounding validator scope.
- **Model Validation:** Model validation from the routed actor is forwarded upward to the surrounding validator scope. The surrounding validator continues to run full-model validation against its own actor model, and `Routes` does not turn that into branch-restricted full-model validation.
- **Field Validation:** If the changed field already belongs to the ancestor model, routes delegates that request upward unchanged as direct-field validation. Otherwise the changed model instance must match a registered route target; when it does, routes prepends the registered ancestor path and delegates the request upward as nested-field validation. After delegating, the routed actor raises `NotifyValidationStateChanged()` on itself.
- **Validation Message Visibility:** This component writes no validation messages itself and owns no message stores. Visible validation messages come from the surrounding validator scope and the edit-context composition that scope establishes.
- **Invalid Or Excluded Cases:** Missing the surrounding validator scope is invalid. Each `Routes` expression must be a supported member-access chain whose evaluated target is a non-null reference-typed descendant model instance. Route uniqueness is enforced by target model instance identity: two expressions may not evaluate to the same target object reference. If a field change comes from a model instance that is neither the ancestor model nor a registered route target, the routed component throws. `Routes` does not provide branch-restricted full-model validation.

### `EditModelScope`

- **Role:** `EditModelScope` is the non-validating scoping primitive. It defines a local actor edit context without owning a validator.
- **Preconditions:** It requires a cascading ancestor `EditContext` and accepts zero or one non-null local actor source. `Model` and `EditContext` may not both be set.
- **Root Edit Context:** The root edit context is resolved by the shared rule defined above. If the local actor edit context and ancestor edit context are the same instance, the ancestor is the root; otherwise an already propagated root marker on the ancestor wins, and if none exists the ancestor becomes the root.
- **Local Actor Edit Context:** If `EditContext` is supplied, that context becomes the local actor edit context. If `Model` is supplied and is not reference-equal to the current actor model, scope creates a new `EditContext(Model)`. If neither is supplied, it creates a new `EditContext` from the ancestor model. When the local actor edit context was derived from the same ancestor edit context, it is reused across plain rerenders. When the ancestor edit context changes, that ancestor-derived actor is recreated.
- **Effective Actor Edit Context Exposed To Descendants:** If the local actor edit context differs from the ancestor edit context, descendants receive it through a new `CascadingValue<EditContext>`. If both are the same instance, descendants continue on the existing cascade.
- **Validator Ownership:** It owns no validator and does not execute FluentValidation.
- **Model Validation:** It does not validate models. When its local actor edit context differs from the root, actor-side `OnValidationRequested` still bubbles upward to `root.Validate()` through the shared edit-context behavior.
- **Field Validation:** It does not validate fields. Its own actor-side `OnFieldChanged` handling performs no validation work.
- **Validation Message Visibility:** It creates no validation message stores. When its local actor edit context differs from the root, the attachment lifecycle keeps actor-owned validation-message visibility reachable from the root while the scope remains attached.
- **Invalid Or Excluded Cases:** Missing the cascading ancestor `EditContext` is invalid. Supplying non-null `Model` and non-null `EditContext` together is invalid. Plain public `EditModelScope` does not, by default, share the ancestor field-state map or the ancestor `EditContext.Properties` object.
