namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal static class ParameterSetTransitionHandlerRegistryAccessor<T> where T : IParameterSetTransitionHandlerRegistryProvider
{
    public static ParameterSetTransitionHandlerRegistry ParameterSetTransitionHandlerRegistry => T.ParameterSetTransitionHandlerRegistry;
}
