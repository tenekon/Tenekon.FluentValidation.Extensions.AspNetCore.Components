using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;

internal static class FieldStateMetadata
{
    private const string FieldStateTypeName = "Microsoft.AspNetCore.Components.Forms.FieldState";
    private const string FormsAssemblyName = "Microsoft.AspNetCore.Components.Forms";
    private const string ValidationMessageStoresFieldName = "_validationMessageStores";
    private const string IsModifiedPropertyName = "IsModified";

    private static Type? s_type;
    private static ConstructorInfo? s_constructor;
    private static FieldInfo? s_validationMessageStoresField;
    private static PropertyInfo? s_isModifiedProperty;

    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties)]
    internal static Type Type => s_type ??= ResolveType();

    internal static ConstructorInfo Constructor => s_constructor ??= ResolveConstructor();

    internal static FieldInfo ValidationMessageStoresField =>
        s_validationMessageStoresField ??= ResolveValidationMessageStoresField();

    internal static PropertyInfo IsModifiedProperty => s_isModifiedProperty ??= ResolveIsModifiedProperty();

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties,
        FieldStateTypeName,
        FormsAssemblyName)]
    private static Type ResolveType()
        => EditContextAccessor.EditContextFieldStateMapMember.FieldType.GetGenericArguments()[1];

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicConstructors,
        FieldStateTypeName,
        FormsAssemblyName)]
    private static ConstructorInfo ResolveConstructor()
        => Type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(FieldIdentifier)]) ??
           throw new InvalidOperationException("A constructor of class FieldState was not found.");

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.NonPublicFields,
        FieldStateTypeName,
        FormsAssemblyName)]
    private static FieldInfo ResolveValidationMessageStoresField()
        => Type.GetField(ValidationMessageStoresFieldName, BindingFlags.NonPublic | BindingFlags.Instance) ??
           throw new InvalidOperationException($"FieldState does not expose {ValidationMessageStoresFieldName} anymore.");

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicProperties,
        FieldStateTypeName,
        FormsAssemblyName)]
    private static PropertyInfo ResolveIsModifiedProperty()
        => Type.GetProperty(IsModifiedPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
           throw new InvalidOperationException($"FieldState does not expose {IsModifiedPropertyName} anymore.");
}
