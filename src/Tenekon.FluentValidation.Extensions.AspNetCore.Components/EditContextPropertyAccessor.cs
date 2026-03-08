using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public static class EditContextPropertyAccessor
{
    internal static readonly object s_rootEditContextLookupKey = new RootEditContextLookupKey();
    internal static readonly object s_descendantEditContextSetLookupKey = new DescendantEditContextSetLookupKey();

    internal static readonly EditContextPropertyRefCountedValue<EditContext> s_rootEditContextProperty = new(s_rootEditContextLookupKey);

    internal static readonly EditContextPropertyHashUniqueValueSet<EditContext> s_descendantEditContextSetProperty = new(
        s_descendantEditContextSetLookupKey);

    public static bool TryGetRootEditContext(EditContext editContext, [NotNullWhen(returnValue: true)] out EditContext? value) =>
        s_rootEditContextProperty.TryGetPropertyValue(editContext, out value);

    private class RootEditContextLookupKey;

    private class DescendantEditContextSetLookupKey;
}
