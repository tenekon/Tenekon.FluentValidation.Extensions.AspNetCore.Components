using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Maps;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Root;

internal static class RootFieldStateMapMutatorFactory
{
    internal static readonly Func<EditContext, IFieldStateMapMutator> Create =
        static editContext => new RootFieldStateMapMutator(editContext);
}
