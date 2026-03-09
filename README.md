# Tenekon.FluentValidation.Extensions.AspNetCore.Components

[![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)
[![License](https://img.shields.io/github/license/tenekon/Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://github.com/tenekon/Tenekon.FluentValidation.Extensions.AspNetCore.Components/blob/main/LICENSE)
[![Activity](https://img.shields.io/github/last-commit/tenekon/Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://github.com/tenekon/Tenekon.FluentValidation.Extensions.AspNetCore.Components/commits/main/)
[![Stars](https://img.shields.io/github/stars/tenekon/Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://github.com/tenekon/Tenekon.FluentValidation.Extensions.AspNetCore.Components/stargazers)
[![Discord](https://img.shields.io/discord/1288602831095468157?label=tenekon%20community)](https://discord.gg/VCa8ePSAqD)

Scoped, nestable FluentValidation for Blazor forms. Use it to validate the form root, a nested model, or a routed region of a form without giving up Blazor's `EditContext` flow.

> [!NOTE]
> Status: `1.0-alpha`. The public API is intended to be stable, but changes may occur as the library matures.

> [!NOTE]
> Repository rename: this repository used to be called `Tenekon.FluentValidation.Extensions`. It was renamed to `Tenekon.FluentValidation.Extensions.AspNetCore.Components` to match the single package it ships.

## Why This Package

Blazor's built-in validation flow is centered around a single `EditContext`, but real forms often split into nested components, repeating items, and scoped sub-regions.

This package lets you:

- validate the whole form with FluentValidation
- attach validators to nested models and collection items
- keep nested validators connected to the parent form lifecycle
- limit validation to selected branches of the model when needed

## Installation

Install the package:

```bash
dotnet add package Tenekon.FluentValidation.Extensions.AspNetCore.Components
```

If you want to resolve validators from dependency injection by using `ValidatorType`, also install:

```bash
dotnet add package FluentValidation.DependencyInjectionExtensions
```

If you prefer, you can skip the DI extensions package and pass a validator instance through the `Validator` parameter instead.

## Quickstart

### 1. Register your validators

```csharp
using FluentValidation;

builder.Services.AddValidatorsFromAssemblyContaining<PersonValidator>();
```

### 2. Create a model and validator

```csharp
using FluentValidation;

public sealed class Person
{
    public string? Name { get; set; }
}

public sealed class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

### 3. Use `EditModelValidatorRootpath` inside your form

Add the package namespace to `_Imports.razor` or directly in the component:

```razor
@using Microsoft.AspNetCore.Components.Forms
@using Tenekon.FluentValidation.Extensions.AspNetCore.Components
```

Then wire the validator into your `EditForm`:

```razor
<EditForm Model="_model" OnValidSubmit="HandleValidSubmit">
    <EditModelValidatorRootpath ValidatorType="typeof(PersonValidator)" />

    <label for="name">Name</label>
    <InputText id="name" @bind-Value="_model.Name" />
    <ValidationMessage For="() => _model.Name" />

    <button type="submit">Save</button>
</EditForm>

@code {
    private readonly Person _model = new();

    private Task HandleValidSubmit()
    {
        return Task.CompletedTask;
    }
}
```

If you want to bypass DI, use `Validator="new PersonValidator()"` instead of `ValidatorType="typeof(PersonValidator)"`.

## Which Component Should I Use?

| Component                    | Use it when                                                                                                                                    |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| `EditModelValidatorRootpath` | You want to validate the main model of the current form or cascaded `EditContext`.                                                             |
| `EditModelValidatorSubpath`  | You want to validate a nested object or a repeated item inside the main model.                                                                 |
| `EditModelValidatorRoutes`   | You want a validator to act only on selected branches of the model. Use it inside `EditModelValidatorRootpath` or `EditModelValidatorSubpath`. |
| `EditModelScope`             | You want to create a scoped validation region with its own `EditContext` behavior.                                                             |

For worked examples of all four components, see the [Validator Components Cookbook](docs/COOKBOOK.md).

## Compatibility And Dependencies

- Target frameworks: `net8.0`, `net9.0`, `net10.0`
- FluentValidation: `12.x`
- Blazor forms integration: the package selects the matching `Microsoft.AspNetCore.Components.Forms` version for each target framework
- Internal dependency: `FastExpressionCompiler` `5.x`

## Documentation

- [Cookbook](docs/COOKBOOK.md): start here for usage patterns and copyable scenarios.
- [Architecture](docs/ARCHITECTURE.md): read this when you want the mental model behind root, subpath, routes, and scope behavior.
- [Motivation](docs/MOTIVATION.md): read this when you want the design rationale and why the package is split into rootpath, subpath, routes, and scope.

## Development

```bash
git clone https://github.com/tenekon/Tenekon.FluentValidation.Extensions.AspNetCore.Components.git
cd Tenekon.FluentValidation.Extensions.AspNetCore.Components
dotnet test
```

Questions, feedback, and design discussion are welcome in the [Tenekon Community Discord](https://discord.gg/VCa8ePSAqD).

## License

MIT License. See [LICENSE](LICENSE) for details.
