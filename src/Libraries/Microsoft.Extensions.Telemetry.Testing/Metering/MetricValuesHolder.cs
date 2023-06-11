// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.Extensions.Telemetry.Testing.Metering.Internal;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

/// <summary>
/// The metric measurements holder that contains information belonging to one named metric.
/// </summary>
/// <typeparam name="T">The type of metric measurement value.</typeparam>
[Experimental]
[DebuggerDisplay("Count = {Count}")]
public sealed class MetricValuesHolder<T> : IReadOnlyCollection<MetricValue<T>>
    where T : struct
{
    private static readonly HashSet<Type> _supportedValueTypesAsDimensionValue = new()
    {
        typeof(int),
        typeof(byte),
        typeof(short),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(char),
    };

    private readonly TimeProvider _timeProvider;
    private readonly AggregationType _aggregationType;
    private readonly ConcurrentDictionary<string, MetricValue<T>> _valuesTable = new();
#if NETCOREAPP3_1_OR_GREATER
    private readonly ConcurrentBag<MetricValue<T>> _values = new();
    private readonly ConcurrentQueue<Measurement<T>> _measurements = new();
#else
    private ConcurrentBag<MetricValue<T>> _values = new();
    private ConcurrentQueue<Measurement<T>> _measurements = new();
#endif

    internal MetricValuesHolder(TimeProvider timeProvider, AggregationType aggregationType, Instrument instrument)
    {
        _timeProvider = timeProvider;
        _aggregationType = aggregationType;
        Instrument = instrument;
    }

    /// <summary>
    /// Gets the instrument that produces the values maintained by this holder.
    /// </summary>
    public Instrument Instrument { get; }

    /// <summary>
    /// Gets the total number of values held by this instance.
    /// </summary>
    public int Count => _values.Count;

    /// <summary>
    /// Gets a recorded metric measurement value by given dimensions.
    /// </summary>
    /// <param name="tags">The dimensions of a metric measurement.</param>
    /// <returns>The metric measurement value or <see langword="null"/> if it does not exist.</returns>
    public T? GetValue(params KeyValuePair<string, object?>[]? tags) => GetMetricValue(tags.AsSpan())?.Value;

    /// <summary>
    /// Gets a recorded metric measurement value by given dimensions.
    /// </summary>
    /// <param name="tags">The dimensions of a metric measurement.</param>
    /// <returns>The metric measurement value or <see langword="null"/> if it does not exist.</returns>
    public T? GetValue(ReadOnlySpan<KeyValuePair<string, object?>> tags) => GetMetricValue(tags)?.Value;

    /// <summary>oes
    /// Gets a recorded metric measurement value by given dimensions.
    /// </summary>
    /// <param name="tags">The dimensions of a metric measurement.</param>
    /// <returns>The metric measurement value or <see langword="null"/> if it does not exist.</returns>
    public MetricValue<T>? GetMetricValue(params KeyValuePair<string, object?>[]? tags) => GetMetricValue(tags.AsSpan());

    /// <summary>oes
    /// Gets a recorded metric measurement value by given dimensions.
    /// </summary>
    /// <param name="tags">The dimensions of a metric measurement.</param>
    /// <returns>The metric measurement value or <see langword="null"/> if it does not exist.</returns>
    public MetricValue<T>? GetMetricValue(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        _ = _valuesTable.TryGetValue(CreateKey(tags, out _), out var value);
        return value;
    }

    /// <summary>
    /// Gets the measurements tracked by this instance.
    /// </summary>
    public IReadOnlyList<Measurement<T>> Measurements => (IReadOnlyList<Measurement<T>>)_measurements;

    /// <summary>
    /// Clears all metric measurements information.
    /// </summary>
    public void Clear()
    {
#if NETCOREAPP3_1_OR_GREATER
        _values.Clear();
        _measurements.Clear();
#else
        _values = new();
        _measurements = new();
#endif
        _valuesTable.Clear();
    }

    /// <summary>
    /// Gets an enumerator of measurements in the holder.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator<MetricValue<T>> IEnumerable<MetricValue<T>>.GetEnumerator() => _values.GetEnumerator();

    /// <summary>
    /// Gets an enumerator of measurements in the holder.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    internal void ReceiveValue(T value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var key = CreateKey(tags, out var sortedTags);

        _measurements.Enqueue(new Measurement<T>(value, sortedTags, _timeProvider.GetUtcNow()));

        switch (_aggregationType)
        {
            case AggregationType.Save:
                var metricValue = new MetricValue<T>(value, sortedTags);
                _values.Add(metricValue);
                _ = _valuesTable.AddOrUpdate(key, metricValue, (_, _) => metricValue);
                break;

            case AggregationType.Aggregate:
                GetOrAdd(key, sortedTags).Add(value);
                break;

            case AggregationType.SaveOrUpdate:
                GetOrAdd(key, sortedTags).Update(value);
                break;

            default:
                throw new InvalidOperationException($"Aggregation type {_aggregationType} is not supported.");
        }
    }

    private static string CreateKey(ReadOnlySpan<KeyValuePair<string, object?>> tags, out KeyValuePair<string, object?>[] sortedTags)
    {
        if (tags.Length == 0)
        {
            sortedTags = Array.Empty<KeyValuePair<string, object?>>();
            return string.Empty;
        }

        sortedTags = tags.ToArray();
        Array.Sort(sortedTags, (x, y) => StringComparer.Ordinal.Compare(x.Key, y.Key));

        const char TagSeparator = ';';
        const char KeyValueSeparator = ':';
        const char ArrayMemberSeparator = ',';

        var keyBuilder = new StringBuilder();

        foreach (var kvp in sortedTags)
        {
            _ = keyBuilder
                .Append(kvp.Key)
                .Append(KeyValueSeparator);

            if (kvp.Value is null)
            {
                _ = keyBuilder.Append(string.Empty);
            }
            else
            {
                var valueType = kvp.Value.GetType();

                if (valueType == typeof(string) || (!valueType.IsArray && _supportedValueTypesAsDimensionValue.Contains(valueType)))
                {
                    _ = keyBuilder.Append(kvp.Value);
                }
                else if (valueType.IsArray && _supportedValueTypesAsDimensionValue.Contains(valueType.GetElementType()!))
                {
                    var array = (Array)kvp.Value;

                    _ = keyBuilder.Append('[');

                    foreach (var item in array)
                    {
                        _ = keyBuilder
                            .Append(item)
                            .Append(ArrayMemberSeparator);
                    }

                    _ = keyBuilder.Append(']');
                }
                else
                {
                    throw new InvalidOperationException($"The type {valueType.FullName} is not supported as a dimension value type.");
                }
            }

            _ = keyBuilder.Append(TagSeparator);
        }

        return keyBuilder.ToString();
    }

    private MetricValue<T> GetOrAdd(string key, KeyValuePair<string, object?>[] sortedTags)
    {
        return _valuesTable.GetOrAdd(key,
            (_) =>
            {
                var metricValue = new MetricValue<T>(default, sortedTags);
                _values.Add(metricValue);
                return metricValue;
            });
    }
}
