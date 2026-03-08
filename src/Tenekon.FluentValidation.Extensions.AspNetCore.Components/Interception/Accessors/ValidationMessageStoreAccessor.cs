using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;

internal static class ValidationMessageStoreAccessor
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_editContext")]
    public static extern ref EditContext GetEditContext(ValidationMessageStore validationMessageStore);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_messages")]
    public static extern ref Dictionary<FieldIdentifier, List<string>>? GetMessagesDictionary(
        ValidationMessageStore validationMessageStore);
}
