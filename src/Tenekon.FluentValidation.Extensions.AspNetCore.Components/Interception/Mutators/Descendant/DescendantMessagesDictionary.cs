using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Descendant;

internal sealed class DescendantMessagesDictionary(IEqualityComparer<FieldIdentifier> equalityComparer)
    : Dictionary<FieldIdentifier, List<string>>(equalityComparer);
