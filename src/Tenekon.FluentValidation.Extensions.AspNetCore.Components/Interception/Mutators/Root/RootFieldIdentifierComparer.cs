using System.Collections;
using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Intent;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Reflection;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Root;

internal sealed class RootFieldIdentifierComparer(IAccessLogger accessLogger)
    : IEqualityComparer<FieldIdentifier>
{
    internal IDictionary? Dictionary;
    internal IAccessLog AccessLog { get; } = accessLogger as IAccessLog ?? throw new InvalidOperationException();

    bool IEqualityComparer<FieldIdentifier>.Equals(FieldIdentifier x, FieldIdentifier y) => x.Equals(y);

    int IEqualityComparer<FieldIdentifier>.GetHashCode(FieldIdentifier obj)
    {
        var dictionary = Interlocked.Exchange(ref Dictionary, null);
        if (dictionary is null) {
            goto hashCode;
        }

        accessLogger.LogEditContextAccess(obj);

        if (!dictionary.Contains(obj)) {
            if (!FieldStateLookupMaterializationPolicy.ShouldMaterializeOnCurrentLookupMiss()) {
                Dictionary = dictionary;
                goto hashCode;
            }

            var fieldState = FieldStateAccessor.Create(obj);
            var validationMessageStoreEqualityComparer = new RootValidationMessageStoreComparer();
            validationMessageStoreEqualityComparer.AddAccessLogger(accessLogger);
            var validationMessageStoreHashSet = new HashSet<ValidationMessageStore>(validationMessageStoreEqualityComparer) {
                InterceptionWarmupMarkers.ValidationMessageStore
            };
            validationMessageStoreHashSet.TrimExcess();
            FieldStateAccessor.SetValidationMessageStores(fieldState, validationMessageStoreHashSet);
            dictionary.Add(obj, fieldState);
        }

        Dictionary = dictionary;

        hashCode:
        return obj.GetHashCode();
    }
}


