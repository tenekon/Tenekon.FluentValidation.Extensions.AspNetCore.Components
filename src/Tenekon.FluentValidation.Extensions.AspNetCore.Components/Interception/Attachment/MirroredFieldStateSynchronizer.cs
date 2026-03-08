using System.Collections;
using Microsoft.AspNetCore.Components.Forms;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Accessors;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Intent;
using Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Maps;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Attachment;

internal static class MirroredFieldStateSynchronizer
{
    public static void AttachExistingMirroredFieldStates(EditContext rootEditContext, EditContext actorEditContext)
    {
        if (ReferenceEquals(rootEditContext, actorEditContext) ||
            EditContextAccessor.EditContextFieldStateMapMember.GetValue(actorEditContext) is not IDictionary actorFieldStateMap) {
            return;
        }

        foreach (DictionaryEntry entry in actorFieldStateMap) {
            if (entry.Key is not FieldIdentifier fieldIdentifier ||
                fieldIdentifier.FieldName == InterceptionWarmupMarkers.FieldIdentifier.FieldName ||
                entry.Value is null) {
                continue;
            }

            var actorValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(entry.Value);
            if (actorValidationMessageStores is null) {
                continue;
            }

            var actorOwnedValidationMessageStores = actorValidationMessageStores
                .Where(store => !ReferenceEquals(store, InterceptionWarmupMarkers.ValidationMessageStore) &&
                                ReferenceEquals(ValidationMessageStoreAccessor.GetEditContext(store), actorEditContext))
                .ToArray();
            if (actorOwnedValidationMessageStores.Length == 0) {
                continue;
            }

            var rootFieldState = EnsureRootFieldState(rootEditContext, entry.Value.GetType(), fieldIdentifier);
            if (rootFieldState is null) {
                continue;
            }

            var rootValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(rootFieldState);
            if (rootValidationMessageStores is null) {
                continue;
            }

            foreach (var actorOwnedValidationMessageStore in actorOwnedValidationMessageStores) {
                ValidationMessageStoreOwnerRegistry.Register(actorOwnedValidationMessageStore, actorEditContext);
                rootValidationMessageStores.Add(actorOwnedValidationMessageStore);
            }
        }
    }

    public static void TryAttachMirroredFieldState(
        EditContext actorEditContext,
        FieldIdentifier fieldIdentifier,
        object descendantFieldState,
        HashSet<ValidationMessageStore> validationMessageStores)
    {
        if (fieldIdentifier.FieldName == InterceptionWarmupMarkers.FieldIdentifier.FieldName ||
            !ContainsNonWarmupValidationMessageStore(validationMessageStores) ||
            !EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContext, out var rootEditContext) ||
            ReferenceEquals(rootEditContext, actorEditContext)) {
            return;
        }

        var rootFieldState = EnsureRootFieldState(rootEditContext, descendantFieldState.GetType(), fieldIdentifier);
        if (rootFieldState is null) {
            return;
        }

        var rootValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(rootFieldState);
        if (rootValidationMessageStores is null) {
            return;
        }

        foreach (var validationMessageStore in validationMessageStores) {
            if (ReferenceEquals(validationMessageStore, InterceptionWarmupMarkers.ValidationMessageStore)) {
                continue;
            }

            rootValidationMessageStores.Add(validationMessageStore);
        }
    }

    public static void TryAttachMirroredValidationMessageStore(
        EditContext actorEditContext,
        FieldIdentifier fieldIdentifier,
        ValidationMessageStore validationMessageStore)
    {
        ValidationMessageStoreOwnerRegistry.Register(validationMessageStore, actorEditContext);

        if (ReferenceEquals(validationMessageStore, InterceptionWarmupMarkers.ValidationMessageStore) ||
            fieldIdentifier.FieldName == InterceptionWarmupMarkers.FieldIdentifier.FieldName ||
            !EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContext, out var rootEditContext) ||
            ReferenceEquals(rootEditContext, actorEditContext) ||
            !TryGetOrCreateRootFieldState(actorEditContext, rootEditContext, fieldIdentifier, out var rootFieldState) ||
            rootFieldState is null) {
            return;
        }

        var rootValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(rootFieldState);
        rootValidationMessageStores?.Add(validationMessageStore);
    }

    public static void TryDetachMirroredValidationMessageStore(
        FieldIdentifier fieldIdentifier,
        ValidationMessageStore validationMessageStore)
    {
        if (ReferenceEquals(validationMessageStore, InterceptionWarmupMarkers.ValidationMessageStore) ||
            fieldIdentifier.FieldName == InterceptionWarmupMarkers.FieldIdentifier.FieldName ||
            !ValidationMessageStoreDetachReentrancyGuard.TryEnter(validationMessageStore)) {
            return;
        }

        try {
            if (!ValidationMessageStoreOwnerRegistry.TryGetOwnerEditContext(validationMessageStore, out var actorEditContext) ||
                !EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContext, out var rootEditContext) ||
                ReferenceEquals(rootEditContext, actorEditContext) ||
                ActorFieldStillAssociatesStore(actorEditContext, fieldIdentifier, validationMessageStore) ||
                !ValidationMessageStoreContainsField(validationMessageStore, fieldIdentifier) ||
                EditContextAccessor.EditContextFieldStateMapMember.GetValue(rootEditContext) is not IDictionary rootFieldStateMap ||
                !TryFindFieldState(rootFieldStateMap, fieldIdentifier, out var rootFieldState) ||
                rootFieldState is null) {
                return;
            }

            var rootValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(rootFieldState);
            rootValidationMessageStores?.Remove(validationMessageStore);
        } finally {
            ValidationMessageStoreDetachReentrancyGuard.Exit(validationMessageStore);
        }
    }

    public static bool ActorFieldAssociatesStore(
        EditContext actorEditContext,
        FieldIdentifier fieldIdentifier,
        ValidationMessageStore validationMessageStore)
        => ActorFieldStillAssociatesStore(actorEditContext, fieldIdentifier, validationMessageStore);

    public static void TryDetachMirroredValidationMessageStoreDuringActorDissociation(
        EditContext actorEditContext,
        FieldIdentifier fieldIdentifier,
        ValidationMessageStore validationMessageStore)
    {
        if (ReferenceEquals(validationMessageStore, InterceptionWarmupMarkers.ValidationMessageStore) ||
            fieldIdentifier.FieldName == InterceptionWarmupMarkers.FieldIdentifier.FieldName ||
            !ValidationMessageStoreDetachReentrancyGuard.TryEnter(validationMessageStore)) {
            return;
        }

        try {
            if (!EditContextPropertyAccessor.s_rootEditContextProperty.TryGetPropertyValue(actorEditContext, out var rootEditContext) ||
                ReferenceEquals(rootEditContext, actorEditContext) ||
                !ActorFieldStillAssociatesStore(actorEditContext, fieldIdentifier, validationMessageStore) ||
                EditContextAccessor.EditContextFieldStateMapMember.GetValue(rootEditContext) is not IDictionary rootFieldStateMap ||
                !TryFindFieldState(rootFieldStateMap, fieldIdentifier, out var rootFieldState) ||
                rootFieldState is null) {
                return;
            }

            var rootValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(rootFieldState);
            rootValidationMessageStores?.Remove(validationMessageStore);
        } finally {
            ValidationMessageStoreDetachReentrancyGuard.Exit(validationMessageStore);
        }
    }

    public static void DetachMirroredFieldStates(EditContext rootEditContext, EditContext actorEditContext)
    {
        if (EditContextAccessor.EditContextFieldStateMapMember.GetValue(actorEditContext) is not IDictionary actorFieldStateMap ||
            EditContextAccessor.EditContextFieldStateMapMember.GetValue(rootEditContext) is not IDictionary rootFieldStateMap) {
            return;
        }

        foreach (DictionaryEntry entry in actorFieldStateMap) {
            if (entry.Key is not FieldIdentifier fieldIdentifier ||
                fieldIdentifier.FieldName == InterceptionWarmupMarkers.FieldIdentifier.FieldName) {
                continue;
            }

            if (entry.Value is null ||
                !TryFindFieldState(rootFieldStateMap, fieldIdentifier, out var rootFieldState) ||
                rootFieldState is null) {
                continue;
            }

            var actorValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(entry.Value);
            var rootValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(rootFieldState);
            if (actorValidationMessageStores is null || rootValidationMessageStores is null) {
                continue;
            }

            foreach (var actorOwnedValidationMessageStore in actorValidationMessageStores.Where(
                         store => !ReferenceEquals(store, InterceptionWarmupMarkers.ValidationMessageStore) &&
                                  ReferenceEquals(ValidationMessageStoreAccessor.GetEditContext(store), actorEditContext))) {
                rootValidationMessageStores.Remove(actorOwnedValidationMessageStore);
            }

            var remainingVisibleValidationMessageStores = rootValidationMessageStores
                .Where(store => !ReferenceEquals(store, InterceptionWarmupMarkers.ValidationMessageStore))
                .ToArray();

            if (remainingVisibleValidationMessageStores.Length == 0 &&
                MirroredRootFieldStateRegistry.IsMirroredOnly(rootEditContext, fieldIdentifier)) {
                rootFieldStateMap.Remove(fieldIdentifier);
                MirroredRootFieldStateRegistry.Forget(rootEditContext, fieldIdentifier);
            }
        }
    }

    private static object? EnsureRootFieldState(EditContext rootEditContext, Type descendantFieldStateType, FieldIdentifier fieldIdentifier)
    {
        var rootFieldStateMap = EditContextAccessor.EditContextFieldStateMapMember.GetValue(rootEditContext) as IDictionary;
        if (rootFieldStateMap is null) {
            return null;
        }

        if (TryFindFieldState(rootFieldStateMap, fieldIdentifier, out var rootFieldState)) {
            return rootFieldState;
        }

        using (ForcedFieldStateMaterializationScope.Enter()) {
            _ = rootEditContext.GetValidationMessages(fieldIdentifier).ToArray();
        }

        if (TryFindFieldState(rootFieldStateMap, fieldIdentifier, out rootFieldState)) {
            MirroredRootFieldStateRegistry.MarkMirroredOnly(rootEditContext, fieldIdentifier);
            return rootFieldState;
        }

        rootFieldState = CreateDetachedFieldState(descendantFieldStateType, fieldIdentifier, rootFieldStateMap);
        if (rootFieldState is not null) {
            MirroredRootFieldStateRegistry.MarkMirroredOnly(rootEditContext, fieldIdentifier);
        }

        return rootFieldState;
    }

    private static bool TryGetOrCreateRootFieldState(
        EditContext actorEditContext,
        EditContext rootEditContext,
        FieldIdentifier fieldIdentifier,
        out object? rootFieldState)
    {
        if (EditContextAccessor.EditContextFieldStateMapMember.GetValue(rootEditContext) is not IDictionary rootFieldStateMap) {
            rootFieldState = null;
            return false;
        }

        if (TryFindFieldState(rootFieldStateMap, fieldIdentifier, out rootFieldState) && rootFieldState is not null) {
            return true;
        }

        if (EditContextAccessor.EditContextFieldStateMapMember.GetValue(actorEditContext) is not IDictionary actorFieldStateMap ||
            !TryFindFieldState(actorFieldStateMap, fieldIdentifier, out var actorFieldState) ||
            actorFieldState is null) {
            rootFieldState = null;
            return false;
        }

        rootFieldState = EnsureRootFieldState(rootEditContext, actorFieldState.GetType(), fieldIdentifier);
        return rootFieldState is not null;
    }

    private static object? CreateDetachedFieldState(Type descendantFieldStateType, FieldIdentifier fieldIdentifier, IDictionary rootFieldStateMap)
    {
        var interceptedMapName = typeof(EditContextFieldStateMap<object>).Name.Split('`')[0];
        if (rootFieldStateMap.GetType().Name.StartsWith(interceptedMapName, StringComparison.Ordinal)) {
            return null;
        }

        var fieldState = FieldStateAccessor.Create(fieldIdentifier);
        rootFieldStateMap.Add(fieldIdentifier, fieldState);
        return fieldState;
    }

    private static bool TryFindFieldState(IDictionary fieldStateMap, FieldIdentifier fieldIdentifier, out object? fieldState)
    {
        foreach (DictionaryEntry entry in fieldStateMap) {
            if (entry.Key is FieldIdentifier currentFieldIdentifier && currentFieldIdentifier.Equals(fieldIdentifier)) {
                fieldState = entry.Value;
                return true;
            }
        }

        fieldState = null;
        return false;
    }

    private static bool ActorFieldStillAssociatesStore(
        EditContext actorEditContext,
        FieldIdentifier fieldIdentifier,
        ValidationMessageStore validationMessageStore)
    {
        if (EditContextAccessor.EditContextFieldStateMapMember.GetValue(actorEditContext) is not IDictionary actorFieldStateMap ||
            !TryFindFieldState(actorFieldStateMap, fieldIdentifier, out var actorFieldState) ||
            actorFieldState is null) {
            return false;
        }

        var actorValidationMessageStores = FieldStateAccessor.GetValidationMessageStores(actorFieldState);
        return actorValidationMessageStores?.Any(store => ReferenceEquals(store, validationMessageStore)) == true;
    }

    private static bool ValidationMessageStoreContainsField(ValidationMessageStore validationMessageStore, FieldIdentifier fieldIdentifier)
    {
        var messagesDictionary = ValidationMessageStoreAccessor.GetMessagesDictionary(validationMessageStore);
        if (messagesDictionary is null) {
            return false;
        }

        foreach (var pair in messagesDictionary) {
            if (pair.Key.Equals(fieldIdentifier)) {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsNonWarmupValidationMessageStore(IEnumerable<ValidationMessageStore> validationMessageStores)
        => validationMessageStores.Any(store => !ReferenceEquals(store, InterceptionWarmupMarkers.ValidationMessageStore));
}


