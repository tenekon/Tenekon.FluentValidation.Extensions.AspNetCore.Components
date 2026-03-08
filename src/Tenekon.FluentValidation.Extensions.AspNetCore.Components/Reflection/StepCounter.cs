namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Reflection;

internal static class StepCounter
{
    [ThreadStatic]
    private static uint s_stepCount;

    public static uint NextStepCount() => ++s_stepCount;
}
