using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public partial class DescendantEditContextMutationTests
{
    [Fact]
    public void EditContext_AddThenGetValidationMessages_MutatorLogsAccordingly()
    {
        var model = new Model { Field1 = null! };
        var editContext = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext);
        validationMessageStore.Add(fieldIdentifier, "FAILURE");
        mutator.AccessLog.Clear();

        _ = editContext.GetValidationMessages().ToList();
        
        mutator.AccessLog.ShouldBe([
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.ValidationMessageStore)
        ]);
    }
    
    [Fact]
    public void EditContext_AddThenGetValidationMessagesByField_MutatorLogsAccordingly()
    {
        var model = new Model { Field1 = null! };
        var editContext = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext);
        validationMessageStore.Add(fieldIdentifier, "FAILURE");
        mutator.AccessLog.Clear();

        _ = editContext.GetValidationMessages(fieldIdentifier).ToList();
        
        mutator.AccessLog.ShouldBe([
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.EditContext),
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.ValidationMessageStore)
        ]);
    }
    
    [Fact]
    public void ValidationMessageStore_AddThenGetValidationMessagesByField_MutatorLogsAccordingly()
    {
        var model = new Model { Field1 = null! };
        var editContext = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext);
        mutator.DoMutation();

        var fieldIdentifier = FieldIdentifier.Create(() => model.Field1);
        var validationMessageStore = new ValidationMessageStore(editContext);
        validationMessageStore.Add(fieldIdentifier, "FAILURE");
        mutator.AccessLog.Clear();

        _ = validationMessageStore[fieldIdentifier].ToList();
        
        mutator.AccessLog.ShouldBe([
            AccessLogEntry.Of(fieldIdentifier, AccessLogSubject.ValidationMessageStore)
        ]);
    }
}


