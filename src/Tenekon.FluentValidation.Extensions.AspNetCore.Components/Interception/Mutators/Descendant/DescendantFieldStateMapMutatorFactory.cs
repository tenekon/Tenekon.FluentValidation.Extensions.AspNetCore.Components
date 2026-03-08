using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Maps;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Descendant;

internal static class DescendantFieldStateMapMutatorFactory
{
    public static IFieldStateMapMutator Create(EditContext editContext)
        => new DescendantFieldStateMapMutator(editContext);
}
