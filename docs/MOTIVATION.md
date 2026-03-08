<!-- omit from toc -->
# Validator Components Motivation [![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components?label=Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)

This document records the motivation behind the package.
It is not the normative architecture document.

For the current architecture, see [ARCHITECTURE.md](ARCHITECTURE.md).
For usage scenarios, see [COOKBOOK.md](COOKBOOK.md).

<!-- omit from toc -->
## Table of Contents

- [The Core Motivation](#the-core-motivation)
- [Why Four Primitives Instead of One](#why-four-primitives-instead-of-one)
- [Relation to Other Integration Styles](#relation-to-other-integration-styles)
- [What This Package Optimizes For](#what-this-package-optimizes-for)
- [What It Does Not Try to Optimize For](#what-it-does-not-try-to-optimize-for)

## The Core Motivation

The motivating idea behind the package is simple:
make FluentValidation work naturally in Blazor forms that are split into nested components, nested models, and scoped regions, without collapsing everything back into one undifferentiated validation component.

The package is therefore not motivated by a rejection of Blazor validation or of FluentValidation.
It is motivated by a particular gap:

1. the input that changes may live in a descendant component
2. the field owner may be a descendant model instance
3. the validator that should decide validity may still belong to an ancestor or root model

Once those three concerns are no longer the same concern, one validator shape is usually not enough.

## Why Four Primitives Instead of One

The package is split into four primitives because the motivating responsibilities are different responsibilities:

1. `EditModelValidatorRootpath` exists for root-model validation.
2. `EditModelValidatorSubpath` exists for validation over an explicit local model.
3. `EditModelValidatorRoutes` exists for path reconstruction and routing.
4. `EditModelScope` exists for local edit-context scoping without becoming a validator.

The motivation is modularity, but not abstraction for abstraction's sake.
The aim is to keep each primitive narrow enough that its role stays understandable:

1. rootpath validates
2. subpath validates
3. routes translates and delegates
4. scope isolates and attaches

That separation is the main design choice of the package.

## Relation to Other Integration Styles

There are several general ways to integrate FluentValidation into Blazor forms.

One style centers the entire form around a single validator component bound to one dominant edit context.
That style is simple when the whole form behaves as one region.
It becomes less expressive when a form needs local validation surfaces, nested model validators, or deliberate scoping boundaries.

Another style introduces a broader abstraction layer above the raw edit-context structure.
That can be useful when the main goal is a more generalized validation framework.
This package is motivated by a different goal:
stay close to Blazor's own `EditContext` model and make the composition points explicit instead of hiding them.

Another design choice in the ecosystem is how implicit validator resolution should be.
This package intentionally keeps validator ownership visible at the component boundary.
The motivating direction is still explicit composition.

The point is therefore not that one integration style is universally better than another.
The point is that this package optimizes for scoped, nestable validation over explicit edit-context relationships.

## What This Package Optimizes For

This package is designed for the following situations:

1. forms that are decomposed into nested reusable components
2. nested model graphs that should not all be validated by the same local validator
3. repeated or descendant regions that still need to report validation through a common root form
4. applications that want explicit control over where validation happens and how it is routed

In short:
it optimizes for forms whose validation topology is richer than a single flat component tree.

## What It Does Not Try to Optimize For

This package does not try to be the most implicit validation integration possible.
It does not try to hide edit-context structure completely.
It does not try to make every validation scenario look like one and the same component.

It also does not treat routing, scoping, and validation as one concern.
The package is intentionally more explicit than that.

That explicitness is the tradeoff.
It asks the consumer to choose the right primitive.
In return, it makes the validation topology of the form visible and composable.
