using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Descendant;

internal static class ValidationMessageStoreFieldProbe
{
    public static bool ContainsField(
        ValidationMessageStore validationMessageStore,
        FieldIdentifier fieldIdentifier)
    {
        var messagesDictionary = ValidationMessageStoreAccessor.GetMessagesDictionary(validationMessageStore);
        if (messagesDictionary is null) {
            return false;
        }

        foreach (var pair in messagesDictionary) {
            if (pair.Key.Equals(fieldIdentifier)) {
                return true;
            }
        }

        return false;
    }
}
