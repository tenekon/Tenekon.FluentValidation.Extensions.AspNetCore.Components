using FluentValidation;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class ChildModelValidator : AbstractValidator<Model.ChildModel>
{
    public ChildModelValidator() => When(x => x.Field1 is not null, () => RuleFor(x => x.Field1).NotEqual("FAILURE"));
}
