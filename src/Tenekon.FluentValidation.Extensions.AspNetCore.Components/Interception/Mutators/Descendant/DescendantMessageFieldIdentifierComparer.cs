using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Attachment;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Descendant;

internal sealed class DescendantMessageFieldIdentifierComparer(ValidationMessageStore validationMessageStore)
    : IEqualityComparer<FieldIdentifier>, IAccessLogCarrier
{
    public IAccessLogger? AccessLogger { get; set; }

    public bool Equals(FieldIdentifier x, FieldIdentifier y) => x.Equals(y);

    public int GetHashCode(FieldIdentifier obj)
    {
        AccessLogger?.LogValidationMessageStoreAccess(obj);
        MirroredFieldStateSynchronizer.TryDetachMirroredValidationMessageStore(obj, validationMessageStore);
        return obj.GetHashCode();
    }
}
