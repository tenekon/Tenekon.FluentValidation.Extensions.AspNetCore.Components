using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public partial class DescendantEditContextMutationTests
{
    [Fact]
    public void FirstValidationMessage_Add_MutatorLogsFieldAndStore()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext1);
        validationMessageStore.Add(fieldIdentifier, "FAILURE");

        mutator.AccessLog.ShouldBe(
        [
            // [ValidationMessageStore]
            // (Add)
            //  > (GetOrCreateMessagesListForField)
            //     > (AssociateWithField)
            //        > _editContext.GetOrAddFieldState(*)
            //
            // [EditContext]
            // (GetOrAddFieldState)
            //  > _fieldStates.TryGetValue
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.EditContext),

            // [ValidationMessageStore]
            // (Add)
            //  > (GetOrCreateMessagesListForField)
            //     > (AssociateWithField)
            //        > _editContext.GetOrAddFieldState(*).AssociateWithValidationMessageStore(*)
            //
            // [FieldState]
            // (AssociateWithValidationMessageStore)
            //  > _validationMessageStores.Add
            //
            // [HashSet<ValidationMessageStore>]
            //   (Add) -> _comparer.GetHashCode
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.ValidationMessageStore, indexShift: -1),
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState),
        ]);
    }

    [Fact]
    public void FirstValidationMessage_AddThenClearThenAdd_MutatorLogsFieldAndStore()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext1);
        validationMessageStore.Add(fieldIdentifier, "FAILURE");
        validationMessageStore.Clear();
        mutator.AccessLog.Clear();

        validationMessageStore.Add(fieldIdentifier, "FAILURE");

        mutator.AccessLog.ShouldBe(
        [
            // [ValidationMessageStore]
            // (Add)
            //  > (GetOrCreateMessagesListForField)
            //     > _messages.TryGetValue
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.ValidationMessageStore),
            // [ValidationMessageStore]
            //   (Add)
            //    > (GetOrCreateMessagesListForField)
            //       > _messages.TryGetValue > _messages.Add
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.ValidationMessageStore),
            // [ValidationMessageStore]
            // (Add)
            //  > (GetOrCreateMessagesListForField)
            //     > _messages.TryGetValue > _messages.Add > (AssociateWithField)
            //                                                > _editContext.GetOrAddFieldState(*)
            //
            // [EditContext]
            // (GetOrAddFieldState)
            //  > _fieldStates.TryGetValue
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.EditContext),
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState),
        ]);
    }

    [Fact]
    public void SecondValidationMessage_AddForSameField_MutatorLogsOnlyStoreAccess()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext1);

        validationMessageStore.Add(fieldIdentifier, "FIRST_FAILURE");
        mutator.AccessLog.Clear();

        validationMessageStore.Add(fieldIdentifier, "SECOND_FAILURE");

        mutator.AccessLog.ShouldBe(
        [
            // [ValidationMessageStore]
            // (Add)
            //  > (GetOrCreateMessagesListForField)
            //     > _messages.TryGetValue(*,*)
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.ValidationMessageStore),
        ]);
    }
}


