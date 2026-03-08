using System.Collections;
using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Maps;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Reflection;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Mutators.Root;

internal sealed class RootFieldStateMapMutator(EditContext editContext)
    : IFieldStateMapMutator
{
    private readonly AccessLog _accessLog = new();

    public IAccessLog AccessLog => _accessLog;

    void IFieldStateMapMutator.DoMutation()
    {
        var dictionary = EditContextAccessor.EditContextFieldStateMapMember.GetValue(editContext) as IDictionary;

        if (dictionary is not null && dictionary.GetType() == FieldStateMapFactory.Type) {
            return;
        }

        var equalityComparer = new RootFieldIdentifierComparer(_accessLog);
        var derivedDictionary = CreateMigratedFieldStateMap(dictionary, equalityComparer);
        equalityComparer.Dictionary = derivedDictionary;
        FieldStateMapAccessLogRegistry.Register(derivedDictionary, _accessLog);
        EditContextAccessor.EditContextFieldStateMapMember.SetValue(editContext, derivedDictionary);
    }

    private IDictionary CreateMigratedFieldStateMap(
        IDictionary? originalDictionary,
        IEqualityComparer<FieldIdentifier> equalityComparer)
    {
        var warmupFieldIdentifier = new FieldIdentifier(new object(), InterceptionWarmupMarkers.FieldIdentifier.FieldName);
        var migratedDictionary = FieldStateMapFactory.Create(equalityComparer);
        migratedDictionary.Add(warmupFieldIdentifier, FieldStateAccessor.Create(warmupFieldIdentifier));

        if (originalDictionary is null) {
            return migratedDictionary;
        }

        foreach (DictionaryEntry pair in originalDictionary) {
            if (pair.Key is not FieldIdentifier fieldIdentifier ||
                pair.Value is null ||
                fieldIdentifier.FieldName == InterceptionWarmupMarkers.FieldIdentifier.FieldName) {
                continue;
            }

            MigrateFieldState(pair.Value);
            migratedDictionary.Add(pair.Key, pair.Value);
        }

        return migratedDictionary;
    }

    private void MigrateFieldState(object fieldState)
    {
        var validationMessageStores = FieldStateAccessor.GetValidationMessageStores(fieldState);
        FieldStateAccessor.SetValidationMessageStores(
            fieldState,
            CreateHookableValidationMessageStoreSet(validationMessageStores));
    }

    private HashSet<ValidationMessageStore> CreateHookableValidationMessageStoreSet(
        IEnumerable<ValidationMessageStore>? existingValidationMessageStores)
    {
        var equalityComparer = new RootValidationMessageStoreComparer();
        equalityComparer.AddAccessLogger(_accessLog);

        var migratedValidationMessageStores = new HashSet<ValidationMessageStore>(equalityComparer) {
            InterceptionWarmupMarkers.ValidationMessageStore
        };

        if (existingValidationMessageStores is not null) {
            foreach (var validationMessageStore in existingValidationMessageStores) {
                if (ReferenceEquals(validationMessageStore, InterceptionWarmupMarkers.ValidationMessageStore)) {
                    continue;
                }

                migratedValidationMessageStores.Add(validationMessageStore);
            }
        }

        migratedValidationMessageStores.TrimExcess();
        return migratedValidationMessageStores;
    }
}

