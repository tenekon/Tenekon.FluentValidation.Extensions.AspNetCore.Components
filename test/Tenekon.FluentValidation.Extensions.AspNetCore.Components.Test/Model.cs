using System.Diagnostics.CodeAnalysis;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public record Model(string? Field1 = null, string? Field2 = null)
{
    public string? Field1 { get; set; } = Field1;
    public string? Field2 { get; set; } = Field2;

    [field: AllowNull]
    [field: MaybeNull]
    public ChildModel Child => field ??= new ChildModel();

    public record ChildModel(string? Field1 = null)
    {
        public string? Field1 { get; set; } = Field1;
    }
}
