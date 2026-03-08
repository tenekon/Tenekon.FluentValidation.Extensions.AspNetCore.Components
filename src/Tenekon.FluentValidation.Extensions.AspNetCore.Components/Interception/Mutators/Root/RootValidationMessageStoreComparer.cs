using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Root;

internal sealed class RootValidationMessageStoreComparer : IEqualityComparer<ValidationMessageStore>
{
    private IAccessLogger? _accessLogger;

    public bool Equals(ValidationMessageStore? x, ValidationMessageStore? y) => ReferenceEquals(x, y);

    public void AddAccessLogger(IAccessLogger accessLogger)
    {
        if (_accessLogger is null) {
            _accessLogger = accessLogger;
            return;
        }

        if (ReferenceEquals(_accessLogger, accessLogger)) {
            return;
        }

        if (_accessLogger is CompositeAccessLogger compositeAccessLogger) {
            compositeAccessLogger.Add(accessLogger);
            return;
        }

        _accessLogger = new CompositeAccessLogger(_accessLogger, accessLogger);
    }

    public int GetHashCode(ValidationMessageStore validationMessageStoreInput)
    {
        if (_accessLogger is not null) {
            ref var messagesMapOriginalRef = ref ValidationMessageStoreAccessor.GetMessagesDictionary(validationMessageStoreInput);
            if (messagesMapOriginalRef is RootMessagesDictionary rootMessagesDictionary &&
                rootMessagesDictionary.Comparer is RootMessageFieldIdentifierComparer rootMessagesComparer) {
                rootMessagesComparer.AddAccessLogger(_accessLogger);
            } else {
                var messagesEqualityComparer = new RootMessageFieldIdentifierComparer(validationMessageStoreInput);
                if (TryGetExistingAccessLogger(messagesMapOriginalRef, out var existingAccessLogger) &&
                    existingAccessLogger is not null) {
                    messagesEqualityComparer.AddAccessLogger(existingAccessLogger);
                }

                messagesEqualityComparer.AddAccessLogger(_accessLogger);

                var messagesMapCustom = new RootMessagesDictionary(messagesEqualityComparer);
                if (messagesMapOriginalRef is not null) {
                    foreach (var pair in messagesMapOriginalRef) {
                        messagesMapCustom.Add(pair.Key, pair.Value);
                    }
                }

                messagesMapOriginalRef = messagesMapCustom;
            }

            _accessLogger.LogFieldStateAccess(validationMessageStoreInput);
        }

        return validationMessageStoreInput.GetHashCode();
    }

    private static bool TryGetExistingAccessLogger(
        Dictionary<FieldIdentifier, List<string>>? messagesMap,
        out IAccessLogger? accessLogger)
    {
        if (messagesMap?.Comparer is not IAccessLogCarrier accessLoggerCarrier) {
            accessLogger = null;
            return false;
        }

        accessLogger = accessLoggerCarrier.AccessLogger;
        return accessLogger is not null;
    }
}
