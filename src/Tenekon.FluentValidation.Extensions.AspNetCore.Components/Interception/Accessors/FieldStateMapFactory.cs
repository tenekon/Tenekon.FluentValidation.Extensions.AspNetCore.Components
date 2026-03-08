using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Maps;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;

internal static class FieldStateMapFactory
{
    private const string RuntimeAssemblyName = "Tenekon.FluentValidation.Extensions.AspNetCore.Components";
    private const string FieldStateMapTypeName =
        "Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Maps.EditContextFieldStateMap`1[[Microsoft.AspNetCore.Components.Forms.FieldState, Microsoft.AspNetCore.Components.Forms]]";

    private static Type? s_type;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    internal static Type Type => s_type ??= ResolveType();

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicConstructors,
        FieldStateMapTypeName,
        RuntimeAssemblyName)]
    internal static IDictionary Create(IEqualityComparer<FieldIdentifier> equalityComparer)
        => (IDictionary)(Activator.CreateInstance(Type, equalityComparer) ??
                         throw new InvalidOperationException($"Could not create {Type.FullName}."));

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicConstructors,
        FieldStateMapTypeName,
        RuntimeAssemblyName)]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:RequiresDynamicCode",
        Justification = "The only closed generic instantiation is FieldStateMap<FieldState>, which is explicitly rooted and verified by the AOT matrix publish/run path.")]
    private static Type ResolveType()
        => typeof(EditContextFieldStateMap<>).MakeGenericType(FieldStateMetadata.Type);
}
