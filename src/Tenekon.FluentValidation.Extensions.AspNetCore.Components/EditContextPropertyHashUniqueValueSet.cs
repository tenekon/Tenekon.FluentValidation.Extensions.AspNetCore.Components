using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal readonly struct EditContextPropertyHashUniqueValueSet<TValue>(object key)
{
    private static HashSet<TValue> GetPropertyValue(object originalPropertyValue)
    {
        if (originalPropertyValue is not HashSet<TValue> propertyValue) {
            throw new InvalidOperationException(
                $"A property with the same key exists, but its value is not of type {typeof(HashSet<TValue>)}.");
        }

        return propertyValue;
    }

    public bool TryGetPropertyValue(EditContext owner, [NotNullWhen(returnValue: true)] out IReadOnlySet<TValue>? value)
    {
        if (!owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            value = null;
            return false;
        }

        var propertyValue = GetPropertyValue(originalPropertyValue);
        value = propertyValue;
        return true;
    }

    public void AttachValue(EditContext owner, TValue value)
    {
        if (owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            var propertyValue = GetPropertyValue(originalPropertyValue);

            if (!propertyValue.Add(value)) {
                throw new InvalidOperationException("You cannot add the same value twice.");
            }

            return;
        }

        owner.Properties[key] = new HashSet<TValue> { value };
    }

    public void DetachValue(EditContext owner, TValue value)
    {
        if (!owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            throw new InvalidOperationException($"A property with the key {key} does not exist");
        }

        var propertyValue = GetPropertyValue(originalPropertyValue);

        if (!propertyValue.Remove(value)) {
            throw new InvalidOperationException("You cannot remove a value that was not added before.");
        }

        if (propertyValue.Count > 0) {
            return;
        }

        owner.Properties.Remove(key);
    }
}
