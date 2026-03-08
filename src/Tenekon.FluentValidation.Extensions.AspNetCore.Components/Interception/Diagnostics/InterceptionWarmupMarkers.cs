using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal static class InterceptionWarmupMarkers
{
    internal static readonly FieldIdentifier FieldIdentifier = new(new object(), "WARMUP");
    internal static readonly ValidationMessageStore ValidationMessageStore = new(new EditContext(new object()));
}
