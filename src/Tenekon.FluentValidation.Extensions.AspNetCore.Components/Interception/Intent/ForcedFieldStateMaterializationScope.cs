namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Intent;

internal static class ForcedFieldStateMaterializationScope
{
    [ThreadStatic]
    private static int s_depth;

    public static bool IsActive => s_depth > 0;

    public static IDisposable Enter()
    {
        s_depth++;
        return new ScopeLease();
    }

    private sealed class ScopeLease : IDisposable
    {
        public void Dispose() => s_depth--;
    }
}
