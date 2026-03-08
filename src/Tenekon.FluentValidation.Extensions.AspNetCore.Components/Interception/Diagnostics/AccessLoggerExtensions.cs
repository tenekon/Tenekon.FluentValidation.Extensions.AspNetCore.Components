using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal static class AccessLoggerExtensions
{
    public static void LogEditContextAccess(this IAccessLogger accessLogger, FieldIdentifier fieldIdentifier) =>
        accessLogger.LogAccess(new AccessLogEntry<FieldIdentifier>(fieldIdentifier, AccessLogSubject.EditContext));

    public static void LogFieldStateAccess(this IAccessLogger accessLogger, ValidationMessageStore validationMessageStore) =>
        accessLogger.LogAccess(new AccessLogEntry<ValidationMessageStore>(validationMessageStore, AccessLogSubject.FieldState));

    public static void LogValidationMessageStoreAccess(this IAccessLogger accessLogger, FieldIdentifier fieldIdentifier) =>
        accessLogger.LogAccess(new AccessLogEntry<FieldIdentifier>(fieldIdentifier, AccessLogSubject.ValidationMessageStore));
}
