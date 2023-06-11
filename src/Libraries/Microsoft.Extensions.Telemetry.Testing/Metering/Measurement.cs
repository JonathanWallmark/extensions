// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

/// <summary>
/// Represents a single measurement performed with an instrument.
/// </summary>
/// <typeparam name="T">The type of metric measurement value.</typeparam>
[Experimental]
public sealed class Measurement<T>
    where T : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Measurement{T}"/> class.
    /// </summary>
    /// <param name="value">The measurement's value.</param>
    /// <param name="tags">The dimensions of this measurement.</param>
    /// <param name="timestamp">The time that the measurement occurred at.</param>
    public Measurement(T value, KeyValuePair<string, object?>[] tags, DateTimeOffset timestamp)
    {
        Tags = tags;
        Timestamp = timestamp;
        Value = value;
    }

    /// <summary>
    /// Gets a measurement's value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets a timestamp indicating when the measurement was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the measurement's dimensions.
    /// </summary>
    public IReadOnlyCollection<KeyValuePair<string, object?>> Tags { get; }

    /// <summary>
    /// Gets a dimension value by a dimension name.
    /// </summary>
    /// <param name="dimensionName">The dimension name.</param>
    /// <returns>The dimension value or <see langword="null"/> if the dimension value was not recorded.</returns>
    public object? GetDimension(string dimensionName)
    {
        foreach (var kvp in Tags)
        {
            if (kvp.Key == dimensionName)
            {
                return kvp.Value;
            }
        }

        return null;
    }
}
