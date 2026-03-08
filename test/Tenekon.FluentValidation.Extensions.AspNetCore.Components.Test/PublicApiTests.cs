using PublicApiGenerator;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class PublicApiTests
{
    [Fact]
    public Task FluentValidationComponentsPublicApi_HasNoChanges()
    {
        var publicApi = typeof(EditContextualComponentBase<>).Assembly.GeneratePublicApi(
            new ApiGeneratorOptions {
                ExcludeAttributes = [
                    "System.Runtime.CompilerServices.InternalsVisibleToAttribute"
                ]
            });

        return Verify(publicApi);

        // Or, if the public api is different based on the target frameworks:
        // return Verifier.Verify(publicApi).UniqueForTargetFrameworkAndVersion();
    }
}
