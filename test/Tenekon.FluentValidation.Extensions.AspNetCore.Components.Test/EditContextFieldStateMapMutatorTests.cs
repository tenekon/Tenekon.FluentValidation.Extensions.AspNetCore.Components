using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class EditContextFieldStateMapMutatorTests
{
    [Fact]
    public void Constructor_IsConstructible()
    {
        var model = new Model();
        var editContext = new EditContext(model);
        var mutator = RootFieldStateMapMutatorFactory.Create(editContext);
        mutator.ShouldNotBeNull();
    }

    [Fact]
    public void DoMutation_Mutates()
    {
        var model = new Model();
        var editContext = new EditContext(model);
        var mutator = RootFieldStateMapMutatorFactory.Create(editContext);
        mutator.DoMutation();

        var dictionary = EditContextAccessor.EditContextFieldStateMapMember.GetValue(editContext).ShouldNotBeNull();
        dictionary.GetType().GetGenericTypeDefinition().ShouldBe(typeof(EditContextFieldStateMap<>));
    }

    // [Fact]
    // public void RecognizeOtherValidationMessageStore()
    // {
    //     var model = new Model { Hello = null! };
    //     var editContext1 = new EditContext(model);
    //     var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
    //     mutator.DoMutation();
    //
    //     var editContext2 = new EditContext(model);
    //     var validationMessageStore = new ValidationMessageStore(editContext2);
    //     validationMessageStore.Add(() => model.Hello, "FAILURE");
    //
    //     EditContextPropertyAccessor.s_descendantEditContextSetProperty.AttachValue(editContext1, editContext2);
    //
    //     // editContext1.GetValidationMessages(() => model.Hello).ToList().ShouldBe(["FAILURE"]);
    //
    //
    //     editContext1.GetValidationMessages().ToList().ShouldBe(["FAILURE"]);
    // }
    
    [Fact]
    public void RecognizeOtherValidationMessageStore()
    {
        var model = new Model { Field1 = null! };
        var editContext1 = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext1);
        mutator.DoMutation();

        var validationMessageStore = new ValidationMessageStore(editContext1);
        validationMessageStore.Add(() => model.Field1, "FAILURE");

        // EditContextPropertyAccessor.s_descendantEditContextSetProperty.AttachValue(editContext1, editContext2);

        // editContext1.GetValidationMessages(() => model.Hello).ToList().ShouldBe(["FAILURE"]);


        ;
    }
}

