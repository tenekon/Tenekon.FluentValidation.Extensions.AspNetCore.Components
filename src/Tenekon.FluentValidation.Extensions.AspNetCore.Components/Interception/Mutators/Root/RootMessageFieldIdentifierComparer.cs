using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Attachment;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Intent;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Root;

internal sealed class RootMessageFieldIdentifierComparer(ValidationMessageStore validationMessageStore)
    : IEqualityComparer<FieldIdentifier>, IAccessLogCarrier
{
    public IAccessLogger? AccessLogger { get; private set; }

    public bool Equals(FieldIdentifier x, FieldIdentifier y) => x.Equals(y);

    public void AddAccessLogger(IAccessLogger accessLogger)
    {
        if (AccessLogger is null) {
            AccessLogger = accessLogger;
            return;
        }

        if (ReferenceEquals(AccessLogger, accessLogger)) {
            return;
        }

        if (AccessLogger is CompositeAccessLogger compositeAccessLogger) {
            compositeAccessLogger.Add(accessLogger);
            return;
        }

        AccessLogger = new CompositeAccessLogger(AccessLogger, accessLogger);
    }

    public int GetHashCode(FieldIdentifier obj)
    {
        if (ValidationMessageStorePromotionPolicy.ShouldPromoteMirroredFieldStateOnCurrentLookup()) {
            MirroredRootFieldStateRegistry.PromoteToLocal(
                ValidationMessageStoreAccessor.GetEditContext(validationMessageStore),
                obj);
        }

        AccessLogger?.LogValidationMessageStoreAccess(obj);
        MirroredFieldStateSynchronizer.TryDetachMirroredValidationMessageStore(obj, validationMessageStore);
        return obj.GetHashCode();
    }
}

