using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Attachment;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Descendant;

internal sealed class DescendantValidationMessageStoreComparer(EditContext editContext, FieldIdentifier fieldIdentifier)
    : IEqualityComparer<ValidationMessageStore>
{
    internal IAccessLogger? AccessLogger { get; set; }

    public bool Equals(ValidationMessageStore? x, ValidationMessageStore? y) => ReferenceEquals(x, y);

    public int GetHashCode(ValidationMessageStore validationMessageStoreInput)
    {
        var actorFieldAlreadyAssociatesStore =
            MirroredFieldStateSynchronizer.ActorFieldAssociatesStore(editContext, fieldIdentifier, validationMessageStoreInput);

        if (AccessLogger is not null) {
            ref var messagesMapOriginalRef = ref ValidationMessageStoreAccessor.GetMessagesDictionary(validationMessageStoreInput);

            if (messagesMapOriginalRef is not DescendantMessagesDictionary) {
                var messagesEqualityComparer = new DescendantMessageFieldIdentifierComparer(validationMessageStoreInput);
                var messagesMapCustom = new DescendantMessagesDictionary(messagesEqualityComparer);

                if (messagesMapOriginalRef is { Count: > 0 }) {
                    foreach (var pair in messagesMapOriginalRef) {
                        messagesMapCustom.Add(pair.Key, pair.Value);
                    }

                    if (messagesMapOriginalRef.FirstOrDefault() is { Key: var key, Value: { Count: 0 } }) {
                        AccessLogger.LogAccess(AccessLogEntry.Of(key, AccessLogSubject.ValidationMessageStore, indexShift: -1));
                    }
                }

                messagesEqualityComparer.AccessLogger = AccessLogger;
                messagesMapOriginalRef = messagesMapCustom;
            }

            AccessLogger.LogFieldStateAccess(validationMessageStoreInput);
        }

        if (actorFieldAlreadyAssociatesStore) {
            MirroredFieldStateSynchronizer.TryDetachMirroredValidationMessageStoreDuringActorDissociation(
                editContext,
                fieldIdentifier,
                validationMessageStoreInput);
        } else if (ValidationMessageStoreFieldProbe.ContainsField(validationMessageStoreInput, fieldIdentifier)) {
            MirroredFieldStateSynchronizer.TryAttachMirroredValidationMessageStore(editContext, fieldIdentifier, validationMessageStoreInput);
        }

        return validationMessageStoreInput.GetHashCode();
    }
}
