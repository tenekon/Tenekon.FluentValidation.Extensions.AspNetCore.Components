using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public partial class DescendantEditContextMutationTests
{
    [Fact]
    public void Constructor_IsConstructible()
    {
        var model = new Model();
        var editContext = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext);
        mutator.ShouldNotBeNull();
    }

    [Fact]
    public void DoMutation_Mutates()
    {
        var model = new Model();
        var editContext = new EditContext(model);
        var mutator = DescendantFieldStateMapMutatorFactory.Create(editContext);
        mutator.DoMutation();

        var dictionary = EditContextAccessor.EditContextFieldStateMapMember.GetValue(editContext).ShouldNotBeNull();
        dictionary.GetType().GetGenericTypeDefinition().ShouldBe(typeof(EditContextFieldStateMap<>));
    }
}

