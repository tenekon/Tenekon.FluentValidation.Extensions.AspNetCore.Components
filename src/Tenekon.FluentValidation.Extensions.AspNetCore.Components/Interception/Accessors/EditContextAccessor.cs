using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;

internal static class EditContextAccessor
{
    private const string EditContextFieldStatesFieldName = "_fieldStates";

    // We cannot use UnsafeAccessor and must work with reflection because part of the targeting signature is internal. :/
    [field: AllowNull]
    [field: MaybeNull]
    [field: DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, typeof(EditContext))]
    public static FieldInfo EditContextFieldStateMapMember =>
        field ??= typeof(EditContext).GetField(EditContextFieldStatesFieldName, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new NotImplementedException(
                $"{nameof(EditContext)} does not implement the {EditContextFieldStatesFieldName} field anymore.");

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Properties>k__BackingField")]
    public static extern ref EditContextProperties GetProperties(EditContext editContext);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Model>k__BackingField")]
    public static extern ref object GetModel(EditContext editContext);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "OnValidationRequested")]
    public static extern ref EventHandler<ValidationRequestedEventArgs>? GetOnValidationRequested(EditContext editContext);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "OnFieldChanged")]
    public static extern ref EventHandler<FieldChangedEventArgs>? GetOnFieldChanged(EditContext editContext);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "OnValidationStateChanged")]
    public static extern ref EventHandler<ValidationStateChangedEventArgs>? GetOnValidationStateChanged(EditContext editContext);
}
