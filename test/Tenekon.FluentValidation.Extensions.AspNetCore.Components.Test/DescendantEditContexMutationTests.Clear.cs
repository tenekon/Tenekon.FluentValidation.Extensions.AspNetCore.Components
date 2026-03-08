using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public partial class DescendantEditContextMutationTests
{
    [Fact]
    public void ValidationMessages_ClearField_MutatorLogsAccordingly()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext1);
        validationMessageStore.Clear(fieldIdentifier);

        mutator.AccessLog.ShouldBe(
        [
            // _fieldStates.TryGetValue
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.EditContext),
            // _validationMessageStores.Remove
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState)
        ]);
    }

    [Fact]
    public void ValidationMessage_AddAndClearSameField_MutatorLogsAccordingly()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext1);
        validationMessageStore.Add(fieldIdentifier, "FAILURE");
        mutator.AccessLog.Clear();
        validationMessageStore.Clear(fieldIdentifier);

        mutator.AccessLog.ShouldBe(
        [
            // _fieldStates.TryGetValue
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.EditContext),
            // _validationMessageStores.Remove
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState),
            // _messages.Remove
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.ValidationMessageStore),
        ]);
    }

    [Fact]
    public void ValidationMessage_AddMultipleFieldsAndClearThemSeparately_MutatorLogsAccordingly()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var field1Identifier = FieldIdentifier.Create(() => model.Field1);
        var field2Identifier = FieldIdentifier.Create(() => model.Field2);
        var validationMessageStore = new ValidationMessageStore(editContext1);
        validationMessageStore.Add(field1Identifier, "FAILURE");
        validationMessageStore.Add(field2Identifier, "FAILURE");
        mutator.AccessLog.Clear();

        validationMessageStore.Clear(field1Identifier);
        mutator.AccessLog.ShouldBe(
        [
            // _fieldStates.TryGetValue
            AccessLogEntry.Of(field1Identifier, AccessLogSubject.EditContext),
            // _validationMessageStores.Remove
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState),
            // _messages.Remove
            AccessLogEntry.Of(field1Identifier, AccessLogSubject.ValidationMessageStore),
        ]);
        mutator.AccessLog.Clear();

        validationMessageStore.Clear(field2Identifier);
        mutator.AccessLog.ShouldBe(
        [
            // _fieldStates.TryGetValue
            AccessLogEntry.Of(field2Identifier, AccessLogSubject.EditContext),
            // _validationMessageStores.Remove
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState),
            // _messages.Remove
            AccessLogEntry.Of(field2Identifier, AccessLogSubject.ValidationMessageStore),
        ]);
    }

    [Fact]
    public void ValidationMessage_AddThenClearStore_MutatorLogsAccordingly()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext1);
        validationMessageStore.Add(fieldIdentifier, "FAILURE");
        mutator.AccessLog.Clear();
        validationMessageStore.Clear();

        mutator.AccessLog.ShouldBe(
        [
            // _fieldStates.TryGetValue
            // AccessLogEntry.Of(Warmup.s_fieldIdentifier, AccessLogSource.EditContext),
            // _fieldStates.TryGetValue
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.EditContext),
            // _validationMessageStores.Remove
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState),
        ]);
    }

    [Fact]
    public void ValidationMessage_AddMultipleFieldsThenClearStore_MutatorLogsAccordingly()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var field1Identifier = FieldIdentifier.Create(() => model.Field1);
        var field2Identifier = FieldIdentifier.Create(() => model.Field2);
        var validationMessageStore = new ValidationMessageStore(editContext1);
        validationMessageStore.Add(field1Identifier, "FAILURE");
        validationMessageStore.Add(field2Identifier, "FAILURE");
        mutator.AccessLog.Clear();
        validationMessageStore.Clear();

        mutator.AccessLog.ShouldBe(
        [
            AccessLogEntry.Of(field1Identifier, AccessLogSubject.EditContext),
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState),

            AccessLogEntry.Of(field2Identifier, AccessLogSubject.EditContext),
            AccessLogEntry.Of(validationMessageStore, AccessLogSubject.FieldState),
        ]);
    }
}


