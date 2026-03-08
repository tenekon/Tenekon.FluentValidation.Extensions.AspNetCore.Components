using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

/// <summary>
/// 
/// </summary>
/// <param name="key">Must be unique across</param>
/// <typeparam name="TValue"></typeparam>
internal readonly struct EditContextPropertyRefCountedValue<TValue>(object key) where TValue : class
{
    private static PropertyValue GetPropertyValue(object originalPropertyValue)
    {
        if (originalPropertyValue is not PropertyValue propertyValue) {
            throw new InvalidOperationException(
                $"A property with the same key exists, but its value is not of type {typeof(PropertyValue)}.");
        }

        if (propertyValue.Counter <= 0) {
            throw new InvalidOperationException(
                "A property with the same key and type exists, but its counter is zero or smaller and it should no longer exist.");
        }

        return propertyValue;
    }

    public bool TryGetPropertyValue(EditContext owner, [NotNullWhen(returnValue: true)] out TValue? value)
    {
        if (!owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            value = null;
            return false;
        }

        var propertyValue = GetPropertyValue(originalPropertyValue);
        value = propertyValue.Value;
        return true;
    }

    internal bool TryGetPropertyValue(EditContext owner, [NotNullWhen(returnValue: true)] out TValue? value, out int counter)
    {
        if (!owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            value = null;
            counter = 0;
            return false;
        }

        var propertyValue = GetPropertyValue(originalPropertyValue);
        value = propertyValue.Value;
        counter = propertyValue.Counter;
        return true;
    }

    public int OccupyProperty(EditContext owner, TValue value)
    {
        if (owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            var propertyValue = GetPropertyValue(originalPropertyValue);

            if (!ReferenceEquals(value, propertyValue.Value)) {
                throw new InvalidOperationException(
                    "A property with the same key and type exists, but its inner value is a different reference than the given value.");
            }

            return ++propertyValue.Counter;
        }

        owner.Properties[key] = new PropertyValue(value, counter: 1);
        return 1;
    }

    public int DisoccupyProperty(EditContext owner)
    {
        if (!owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            throw new InvalidOperationException($"A property with the key {key} does not exist");
        }

        var propertyValue = GetPropertyValue(originalPropertyValue);

        if (--propertyValue.Counter > 0) {
            return propertyValue.Counter;
        }

        owner.Properties.Remove(key);
        return 0;
    }

    private class PropertyValue(TValue value, int counter)
    {
        public TValue Value { get; set; } = value;
        internal int Counter { get; set; } = counter;
    }
}
