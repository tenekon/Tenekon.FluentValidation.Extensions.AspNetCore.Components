using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Attachment;

internal static class ValidationMessageStoreDetachReentrancyGuard
{
    [ThreadStatic]
    private static HashSet<ValidationMessageStore>? s_activeValidationMessageStores;

    public static bool TryEnter(ValidationMessageStore validationMessageStore)
    {
        s_activeValidationMessageStores ??= [];
        return s_activeValidationMessageStores.Add(validationMessageStore);
    }

    public static void Exit(ValidationMessageStore validationMessageStore)
        => s_activeValidationMessageStores?.Remove(validationMessageStore);
}
