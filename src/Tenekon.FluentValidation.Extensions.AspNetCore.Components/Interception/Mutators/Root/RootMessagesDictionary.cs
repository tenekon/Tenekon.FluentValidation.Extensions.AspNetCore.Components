using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Root;

internal sealed class RootMessagesDictionary(IEqualityComparer<FieldIdentifier> equalityComparer)
    : Dictionary<FieldIdentifier, List<string>>(equalityComparer);
