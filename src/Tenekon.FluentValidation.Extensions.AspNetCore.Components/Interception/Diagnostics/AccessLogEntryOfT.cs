using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components.Interception.Diagnostics;

internal record AccessLogEntry<TValue>(TValue Value, AccessLogSubject Subject, int IndexShift = 0) : IAccessLogEntry
{
    object? IAccessLogEntry.UntypedValue => Value;

    public override string ToString()
    {
        var text = new System.Text.StringBuilder();
        text.Append(Environment.NewLine);
        text.AppendLine($"Subject: {Subject}");
        text.AppendLine($"Value: <{typeof(TValue).Name}>");

        if (IndexShift != 0) {
            text.AppendLine($"IndexShift: {IndexShift}");
        }

        if (Value is FieldIdentifier fieldIdentifier) {
            text.AppendLine($"  ModelType: {fieldIdentifier.Model.GetType().Name}");
            text.AppendLine($"  FieldName: {fieldIdentifier.FieldName}");
        } else if (Value is ValidationMessageStore validationMessageStore) {
            text.AppendLine($"  HashCode: {validationMessageStore.GetHashCode()}");
        }

        return text.ToString();
    }
}
