using FluentValidation;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class ModelValidator : AbstractValidator<Model>
{
    public ModelValidator()
    {
        When(x => x.Field1 is not null, () => RuleFor(x => x.Field1).Equal("Field1"));
        When(x => x.Field2 is not null, () => RuleFor(x => x.Field1).Equal("Field1"));
        When(x => x.Child.Field1 is not null, () => RuleFor(x => x.Child.Field1).Equal("Field1"));
    }
}
