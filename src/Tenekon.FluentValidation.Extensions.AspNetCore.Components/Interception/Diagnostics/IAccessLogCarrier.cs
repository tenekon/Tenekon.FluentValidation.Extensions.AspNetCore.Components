namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal interface IAccessLogCarrier
{
    IAccessLogger? AccessLogger { get; }
}
