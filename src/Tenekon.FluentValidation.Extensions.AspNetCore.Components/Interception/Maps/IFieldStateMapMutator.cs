using System.Diagnostics.CodeAnalysis;

using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Reflection;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Maps;

internal interface IFieldStateMapMutator
{
    IAccessLog AccessLog { get; }
    
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicFields,
        "Microsoft.AspNetCore.Components.Forms.FieldState",
        "Microsoft.AspNetCore.Components.Forms")]
    void DoMutation();
}
