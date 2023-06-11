﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

/// <summary>
/// The helper class to automatically capture metering data that has been recorded
/// by instruments created by <see cref="Meter"/>.
/// </summary>
/// <remarks>
/// This type has been designed to be used only for testing purposes.
/// </remarks>
[Experimental]
[DebuggerDisplay("Count = {Count}")]
public partial class MetricCollector : IDisposable
{
    private readonly MeterListener _listener;
    private readonly string[]? _meterNames;
#pragma warning disable CA2213 // Disposable fields should be disposed
    private readonly Meter? _meter;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly TimeProvider _timeProvider;

    private readonly Dictionary<Type, object> _allCounters;
    private readonly Dictionary<Type, object> _allHistograms;
    private readonly Dictionary<Type, object> _allUpDownCounters;
    private readonly Dictionary<Type, object> _allObservableCounters;
    private readonly Dictionary<Type, object> _allObservableGauges;
    private readonly Dictionary<Type, object> _allObservableUpDownCounters;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricCollector"/> class.
    /// </summary>
    /// <param name="meterNames">The names of .NET <see cref="Meter"/> to capture measurements from.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> instance.</param>
    /// <remarks>
    /// This constructor is applicable for the scenario when metering data generated
    /// by active <see cref="Meter"/> instances which <see cref="Meter.Name"/> matches
    /// the one from the list <paramref name="meterNames"/> is to be captured.
    /// </remarks>
    public MetricCollector(IEnumerable<string> meterNames, TimeProvider? timeProvider = null)
        : this(meterNames, null, timeProvider, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricCollector"/> class.
    /// </summary>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> instance.</param>
    /// <remarks>
    /// This constructor is applicable for the scenario when metering data
    /// generated by all the active <see cref="Meter"/> instances
    /// in the application is to be captured.
    /// </remarks>
    public MetricCollector(TimeProvider? timeProvider = null)
        : this(Array.Empty<string>(), null, timeProvider, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricCollector"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="Meter"/> instance to capture metering data.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> instance.</param>
    /// <remarks>
    /// This constructor is applicable for the scenario when metering data
    /// generated by a specific <see cref="Meter"/> instance is to be captured.
    /// </remarks>
    public MetricCollector(Meter meter, TimeProvider? timeProvider = null)
        : this(null, meter, timeProvider, true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricCollector"/> class.
    /// </summary>
    /// <param name="instrument">The <see cref="Instrument"/> instance from which to capture metering data.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> instance.</param>
    /// <remarks>
    /// This constructor is applicable to capture the metering data
    /// generated by a specific <see cref="Instrument"/> instance.
    /// </remarks>
    public MetricCollector(Instrument instrument, TimeProvider? timeProvider = null)
        : this(new[] { Throw.IfNull(instrument).Name }, instrument.Meter, timeProvider, true)
    {
    }

    private MetricCollector(IEnumerable<string>? meterNames, Meter? meter, TimeProvider? timeProvider, bool applyMeterFiltering)
    {
        if (applyMeterFiltering)
        {
            _meter = Throw.IfNull(meter);
        }
        else
        {
            _meterNames = Throw.IfNull(meterNames).ToArray();
        }

        _timeProvider = timeProvider ?? TimeProvider.System;

        _listener = new MeterListener
        {
            InstrumentPublished = OnInstrumentPublished
        };

        _listener.SetMeasurementEventCallback<byte>(CollectMeasurement);
        _listener.SetMeasurementEventCallback<short>(CollectMeasurement);
        _listener.SetMeasurementEventCallback<int>(CollectMeasurement);
        _listener.SetMeasurementEventCallback<long>(CollectMeasurement);
        _listener.SetMeasurementEventCallback<float>(CollectMeasurement);
        _listener.SetMeasurementEventCallback<double>(CollectMeasurement);
        _listener.SetMeasurementEventCallback<decimal>(CollectMeasurement);

        _allCounters = CreateMetricsValuesDictionary();
        _allHistograms = CreateMetricsValuesDictionary();
        _allUpDownCounters = CreateMetricsValuesDictionary();
        _allObservableGauges = CreateMetricsValuesDictionary();
        _allObservableCounters = CreateMetricsValuesDictionary();
        _allObservableUpDownCounters = CreateMetricsValuesDictionary();

        _listener.Start();
    }

    /// <summary>
    /// Clears all the captured metering data.
    /// </summary>
    public void Clear()
    {
        ClearValues(_allCounters);
        ClearValues(_allHistograms);
        ClearValues(_allUpDownCounters);
        ClearValues(_allObservableCounters);
        ClearValues(_allObservableGauges);
        ClearValues(_allObservableUpDownCounters);
    }

    /// <summary>
    /// Gets the total number of measurements held by the collector.
    /// </summary>
    public int Count => CountValues(_allCounters)
                + CountValues(_allHistograms)
                + CountValues(_allUpDownCounters)
                + CountValues(_allObservableCounters)
                + CountValues(_allObservableGauges)
                + CountValues(_allObservableUpDownCounters);

    /// <summary>
    /// Gets the object containing all the captured metering data that has been recorded by a counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="counterName">The metric name.</param>
    /// <returns>
    /// The <see cref="MetricValuesHolder{T}"/> instance or <see langword="null"/> if a counter with given <paramref name="counterName"/> was not found.
    /// </returns>
    public MetricValuesHolder<T>? GetCounterValues<T>(string counterName)
        where T : struct => GetInstrumentCapturedData<T>(_allCounters, counterName);

    /// <summary>
    /// Gets the object containing all the captured metering data that has been recorded by a histogram instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="histogramName">The metric name.</param>
    /// <returns>
    /// The <see cref="MetricValuesHolder{T}"/> instance or <see langword="null"/> if a histogram with given <paramref name="histogramName"/> was not found.
    /// </returns>
    public MetricValuesHolder<T>? GetHistogramValues<T>(string histogramName)
        where T : struct => GetInstrumentCapturedData<T>(_allHistograms, histogramName);

    /// <summary>
    /// Gets the object containing all the captured metering data that has been recorded by an updown counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="updownCounterName">The metric name.</param>
    /// <returns>
    /// The <see cref="MetricValuesHolder{T}"/> instance or <see langword="null"/> if an updown counter with given <paramref name="updownCounterName"/> was not found.
    /// </returns>
    public MetricValuesHolder<T>? GetUpDownCounterValues<T>(string updownCounterName)
        where T : struct => GetInstrumentCapturedData<T>(_allUpDownCounters, updownCounterName);

    /// <summary>
    /// Gets the object containing all the captured metering data that has been recorded by an observable gauge instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableGaugeName">The metric name.</param>
    /// <returns>
    /// The <see cref="MetricValuesHolder{T}"/> instance or <see langword="null"/> if an observable gauge with given <paramref name="observableGaugeName"/> was not found.
    /// </returns>
    public MetricValuesHolder<T>? GetObservableGaugeValues<T>(string observableGaugeName)
        where T : struct => GetInstrumentCapturedData<T>(_allObservableGauges, observableGaugeName);

    /// <summary>
    /// Gets the object containing all the captured metering data that has been recorded by an observable counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableCounterName">The metric name.</param>
    /// <returns>
    /// The <see cref="MetricValuesHolder{T}"/> instance or <see langword="null"/> if an observable counter with given <paramref name="observableCounterName"/> was not found.
    /// </returns>
    public MetricValuesHolder<T>? GetObservableCounterValues<T>(string observableCounterName)
        where T : struct => GetInstrumentCapturedData<T>(_allObservableCounters, observableCounterName);

    /// <summary>
    /// Gets the object containing all the captured metering data that has been recorded by an observable updown counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableUpDownCounterName">The metric name.</param>
    /// <returns>
    /// The <see cref="MetricValuesHolder{T}"/> instance or <see langword="null"/> if an observable updown counter with given <paramref name="observableUpDownCounterName"/> was not found.
    /// </returns>
    public MetricValuesHolder<T>? GetObservableUpDownCounterValues<T>(string observableUpDownCounterName)
        where T : struct => GetInstrumentCapturedData<T>(_allObservableUpDownCounters, observableUpDownCounterName);

    /// <summary>
    /// Gets the object containing all the captured metering data that has been recorded by an instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="instrument">The instrument of interest.</param>
    /// <returns>
    /// The <see cref="MetricValuesHolder{T}"/> instance or <see langword="null"/> if an observable updown counter with given <paramref name="observableUpDownCounterName"/> was not found.
    /// </returns>
    public MetricValuesHolder<T>? GetInstrumentValues<T>(Instrument instrument)
        where T : struct => GetInstrumentCapturedData<T>(instrument);

    /// <summary>
    /// Gets a measurement value recorded by a counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="counterName">The metric name.</param>
    /// <param name="tags">The dimensions collection describing the measurement value.</param>
    /// <returns>The measurement value or <see langword="null"/> if the value was not recorded.</returns>
    public T? GetCounterValue<T>(string counterName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetCounterValues<T>(counterName)?.GetValue(tags);

    /// <summary>
    /// Gets a measurement value recorded by a histogram instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="histogramName">The metric name.</param>
    /// <param name="tags">The dimensions collection describing the measurement value.</param>
    /// <returns>The measurement value or <see langword="null"/> if the value was not recorded.</returns>
    public T? GetHistogramValue<T>(string histogramName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetHistogramValues<T>(histogramName)?.GetValue(tags);

    /// <summary>
    /// Gets a measurement value recorded by an updown counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="updownCounterName">The metric name.</param>
    /// <param name="tags">The dimensions collection describing the measurement value.</param>
    /// <returns>The measurement value or <see langword="null"/> if the value was not recorded.</returns>
    public T? GetUpDownCounterValue<T>(string updownCounterName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetUpDownCounterValues<T>(updownCounterName)?.GetValue(tags);

    /// <summary>
    /// Gets a measurement value recorded by an observable counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableCounterName">The metric name.</param>
    /// <param name="tags">The dimensions collection describing the measurement value.</param>
    /// <returns>The measurement value or <see langword="null"/> if the value was not recorded.</returns>
    public T? GetObservableCounterValue<T>(string observableCounterName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetObservableCounterValues<T>(observableCounterName)?.GetValue(tags);

    /// <summary>
    /// Gets a measurement value recorded by an observable gauge instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableGaugeName">The metric name.</param>
    /// <param name="tags">The dimensions collection describing the measurement value.</param>
    /// <returns>The measurement value or <see langword="null"/> if the value was not recorded.</returns>
    public T? GetObservableGaugeValue<T>(string observableGaugeName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetObservableGaugeValues<T>(observableGaugeName)?.GetValue(tags);

    /// <summary>
    /// Gets a measurement value recorded by an observable updown counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableUpDownCounterName">The metric name.</param>
    /// <param name="tags">The dimensions collection describing the measurement value.</param>
    /// <returns>The measurement value or <see langword="null"/> if the value was not recorded.</returns>
    public T? GetObservableUpDownCounterValue<T>(string observableUpDownCounterName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetObservableUpDownCounterValues<T>(observableUpDownCounterName)?.GetValue(tags);

    /// <summary>
    /// Gets a measurement value recorded by an instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="instrument">The instrument of interest.</param>
    /// <param name="tags">The dimensions collection describing the measurement value.</param>
    /// <returns>The measurement value or <see langword="null"/> if the value was not recorded.</returns>
    public T? GetInstrumentValue<T>(Instrument instrument, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetInstrumentValues<T>(instrument)?.GetValue(tags);

    /// <summary>
    /// Gets the measurements recorded by a counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="counterName">The metric name.</param>
    /// <returns>The measurement values or <see langword="null"/> no measurements were recorded.</returns>
    public IReadOnlyList<Measurement<T>>? GetCounterMeasurements<T>(string counterName)
        where T : struct => GetCounterValues<T>(counterName)?.Measurements;

    /// <summary>
    /// Gets the measurements recorded by a histogram instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="histogramName">The metric name.</param>
    /// <returns>The measurement values or <see langword="null"/> no measurements were recorded.</returns>
    public IReadOnlyList<Measurement<T>>? GetHistogramMeasurements<T>(string histogramName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetHistogramValues<T>(histogramName)?.Measurements;

    /// <summary>
    /// Gets the measurements recorded by an updown counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="updownCounterName">The metric name.</param>
    /// <returns>The measurement values or <see langword="null"/> no measurements were recorded.</returns>
    public IReadOnlyList<Measurement<T>>? GetUpDownCounterMeasurements<T>(string updownCounterName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetUpDownCounterValues<T>(updownCounterName)?.Measurements;

    /// <summary>
    /// Gets the measurements recorded by an observable counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableCounterName">The metric name.</param>
    /// <returns>The measurement values or <see langword="null"/> no measurements were recorded.</returns>
    public IReadOnlyList<Measurement<T>>? GetObservableCounterMeasurement<T>(string observableCounterName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetObservableCounterValues<T>(observableCounterName)?.Measurements;

    /// <summary>
    /// Gets the measurements recorded by an observable gauge instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableGaugeName">The metric name.</param>
    /// <returns>The measurement values or <see langword="null"/> no measurements were recorded.</returns>
    public IReadOnlyList<Measurement<T>>? GetObservableGaugeMeasurement<T>(string observableGaugeName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetObservableGaugeValues<T>(observableGaugeName)?.Measurements;

    /// <summary>
    /// Gets the measurements recorded by an observable updown counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="observableUpDownCounterName">The metric name.</param>
    /// <returns>The measurement values or <see langword="null"/> no measurements were recorded.</returns>
    public IReadOnlyList<Measurement<T>>? GetObservableUpDownCounterMeasurements<T>(string observableUpDownCounterName, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetObservableUpDownCounterValues<T>(observableUpDownCounterName)?.Measurements;

    /// <summary>
    /// Gets the measurements recorded by an instrument.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <param name="instrument">The instrument of interest.</param>
    /// <returns>The measurement values or <see langword="null"/> no measurements were recorded.</returns>
    public IReadOnlyList<Measurement<T>>? GetInstrumentMeasurements<T>(Instrument instrument, params KeyValuePair<string, object?>[] tags)
        where T : struct => GetInstrumentValues<T>(instrument)?.Measurements;

#pragma warning disable S4049 // Properties should be preferred
    /// <summary>
    /// Gets a list of all counters registered with this metrics collector.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <returns>Read only dictionary of <see cref="MetricValuesHolder{T}"/>.</returns>
    public IReadOnlyDictionary<string, MetricValuesHolder<T>>? GetAllCounters<T>()
        where T : struct => GetAllInstruments<T>(_allCounters);

    /// <summary>
    /// Gets a list of all UpDown counters registered with this metrics collector.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <returns>Read only dictionary of <see cref="MetricValuesHolder{T}"/>.</returns>
    public IReadOnlyDictionary<string, MetricValuesHolder<T>>? GetAllUpDownCounters<T>()
        where T : struct => GetAllInstruments<T>(_allUpDownCounters);

    /// <summary>
    /// Gets a list of all histograms registered with this metrics collector.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <returns>Read only dictionary of <see cref="MetricValuesHolder{T}"/>.</returns>
    public IReadOnlyDictionary<string, MetricValuesHolder<T>>? GetAllHistograms<T>()
        where T : struct => GetAllInstruments<T>(_allHistograms);

    /// <summary>
    /// Gets a list of all observable counters registered with this metrics collector.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <returns>Read only dictionary of <see cref="MetricValuesHolder{T}"/>.</returns>
    public IReadOnlyDictionary<string, MetricValuesHolder<T>>? GetAllObservableCounters<T>()
        where T : struct => GetAllInstruments<T>(_allObservableCounters);

    /// <summary>
    /// Gets a list of all observable UpDown counters registered with this metrics collector.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <returns>Read only dictionary of <see cref="MetricValuesHolder{T}"/>.</returns>
    public IReadOnlyDictionary<string, MetricValuesHolder<T>>? GetAllObservableUpDownCounters<T>()
        where T : struct => GetAllInstruments<T>(_allObservableUpDownCounters);

    /// <summary>
    /// Gets a list of all observable Gauges registered with this metrics collector.
    /// </summary>
    /// <typeparam name="T">The type of metric measurement value.</typeparam>
    /// <returns>Read only dictionary of <see cref="MetricValuesHolder{T}"/>.</returns>
    public IReadOnlyDictionary<string, MetricValuesHolder<T>>? GetAllObservableGauges<T>()
        where T : struct => GetAllInstruments<T>(_allObservableGauges);
#pragma warning restore S4049 // Properties should be preferred

    /// <summary>
    /// Collects all observable instruments and records their measurements.
    /// </summary>
    public void CollectObservableInstruments()
    {
        _listener.RecordObservableInstruments();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose the el.
    /// </summary>
    /// <param name="disposing">Disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _listener.Dispose();
        }
    }

    private static void ClearValues(Dictionary<Type, object> instrumentValues)
    {
        ClearValuesOf<byte>(instrumentValues);
        ClearValuesOf<short>(instrumentValues);
        ClearValuesOf<int>(instrumentValues);
        ClearValuesOf<long>(instrumentValues);
        ClearValuesOf<float>(instrumentValues);
        ClearValuesOf<double>(instrumentValues);
        ClearValuesOf<decimal>(instrumentValues);
    }

    private static void ClearValuesOf<T>(Dictionary<Type, object> instrumentValues)
        where T : struct
    {
        var valuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<T>>)instrumentValues[typeof(T)];

        foreach (var kvp in valuesDictionary)
        {
            kvp.Value.Clear();
        }
    }

    private static int CountValues(Dictionary<Type, object> instrumentValues)
    {
        return
            CountValuesOf<byte>(instrumentValues)
            + CountValuesOf<short>(instrumentValues)
            + CountValuesOf<int>(instrumentValues)
            + CountValuesOf<long>(instrumentValues)
            + CountValuesOf<float>(instrumentValues)
            + CountValuesOf<double>(instrumentValues)
            + CountValuesOf<decimal>(instrumentValues);
    }

    private static int CountValuesOf<T>(Dictionary<Type, object> instrumentValues)
        where T : struct
    {
        var valuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<T>>)instrumentValues[typeof(T)];

        int count = 0;
        foreach (var kvp in valuesDictionary)
        {
            count += kvp.Value.Count;
        }

        return count;
    }

    private static MetricValuesHolder<T>? GetInstrumentCapturedData<T>(Dictionary<Type, object> allInstrumentsValues, string instrumentName)
        where T : struct
    {
        if (!allInstrumentsValues.TryGetValue(typeof(T), out object? value))
        {
            throw new InvalidOperationException($"The type {typeof(T)} is not supported as a type for a metric measurement value");
        }

        var instrumentsDictionary = (IReadOnlyDictionary<string, MetricValuesHolder<T>>)value;
        _ = instrumentsDictionary.TryGetValue(instrumentName, out var metricValuesHolder);

        return metricValuesHolder;
    }

    private MetricValuesHolder<T>? GetInstrumentCapturedData<T>(Instrument instrument)
        where T : struct
    {
        return GetInstrumentCapturedData<T>(_allCounters, instrument.Name)
            ?? GetInstrumentCapturedData<T>(_allHistograms, instrument.Name)
            ?? GetInstrumentCapturedData<T>(_allObservableCounters, instrument.Name)
            ?? GetInstrumentCapturedData<T>(_allObservableGauges, instrument.Name)
            ?? GetInstrumentCapturedData<T>(_allObservableUpDownCounters, instrument.Name)
            ?? GetInstrumentCapturedData<T>(_allUpDownCounters, instrument.Name);
    }

    private static IReadOnlyDictionary<string, MetricValuesHolder<T>>? GetAllInstruments<T>(Dictionary<Type, object> allInstruments)
        where T : struct
    {
        if (!allInstruments.TryGetValue(typeof(T), out object? value))
        {
            throw new InvalidOperationException($"The type {typeof(T)} is not supported as a type for a metric measurement value");
        }

        return (IReadOnlyDictionary<string, MetricValuesHolder<T>>)value;
    }

    private static Dictionary<Type, object> CreateMetricsValuesDictionary()
    {
#pragma warning disable CPR121 // Specify 'concurrencyLevel' and 'capacity' in the ConcurrentDictionary ctor.
        return new Dictionary<Type, object>
        {
            [typeof(byte)] = new ConcurrentDictionary<string, MetricValuesHolder<byte>>(StringComparer.Ordinal),
            [typeof(short)] = new ConcurrentDictionary<string, MetricValuesHolder<short>>(StringComparer.Ordinal),
            [typeof(int)] = new ConcurrentDictionary<string, MetricValuesHolder<int>>(StringComparer.Ordinal),
            [typeof(long)] = new ConcurrentDictionary<string, MetricValuesHolder<long>>(StringComparer.Ordinal),
            [typeof(float)] = new ConcurrentDictionary<string, MetricValuesHolder<float>>(StringComparer.Ordinal),
            [typeof(double)] = new ConcurrentDictionary<string, MetricValuesHolder<double>>(StringComparer.Ordinal),
            [typeof(decimal)] = new ConcurrentDictionary<string, MetricValuesHolder<decimal>>(StringComparer.Ordinal),
        };
#pragma warning restore CPR121 // Specify 'concurrencyLevel' and 'capacity' in the ConcurrentDictionary ctor.
    }
}
