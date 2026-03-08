using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Attachment;

internal static class ValidationMessageStoreOwnerRegistry
{
    private sealed class OwnerHolder(EditContext editContext)
    {
        public EditContext EditContext { get; set; } = editContext;
    }

    private static readonly ConditionalWeakTable<ValidationMessageStore, OwnerHolder> s_ownerTable = new();

    public static void Register(ValidationMessageStore validationMessageStore, EditContext editContext)
    {
        var ownerHolder = s_ownerTable.GetValue(validationMessageStore, _ => new OwnerHolder(editContext));
        ownerHolder.EditContext = editContext;
    }

    public static bool TryGetOwnerEditContext(ValidationMessageStore validationMessageStore, out EditContext editContext)
    {
        if (s_ownerTable.TryGetValue(validationMessageStore, out var ownerHolder)) {
            editContext = ownerHolder.EditContext;
            return true;
        }

        editContext = null!;
        return false;
    }
}
