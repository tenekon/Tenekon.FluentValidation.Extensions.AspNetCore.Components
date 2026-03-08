using System.Collections;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;

internal static class FieldStateAccessor
{
    internal static object Create(FieldIdentifier fieldIdentifier)
        => FieldStateMetadata.Constructor.Invoke([fieldIdentifier]);

    internal static HashSet<ValidationMessageStore>? GetValidationMessageStores(object fieldState)
        => (HashSet<ValidationMessageStore>?)FieldStateMetadata.ValidationMessageStoresField.GetValue(fieldState);

    internal static void SetValidationMessageStores(object fieldState, HashSet<ValidationMessageStore>? validationMessageStores)
        => FieldStateMetadata.ValidationMessageStoresField.SetValue(fieldState, validationMessageStores);

    internal static bool GetIsModified(object fieldState)
        => FieldStateMetadata.IsModifiedProperty.GetValue(fieldState) as bool? ?? false;
}
