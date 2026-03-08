using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Maps;

internal class EditContextFieldStateMap<TFieldState>(IEqualityComparer<FieldIdentifier> equalityComparer)
    : Dictionary<FieldIdentifier, TFieldState>(equalityComparer) where TFieldState : class;
