// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Extensions for adding the Windows resource utilization provider.
/// </summary>
public static class WindowsUtilizationExtensions
{
    /// <summary>
    /// An extension method to configure and add the default windows utilization provider to services collection.
    /// </summary>
    /// <param name="builder">The tracker builder instance used to add the provider.</param>
    /// <returns>Returns the input tracker builder for call chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddWindowsProvider(this IResourceMonitorBuilder builder);

    /// <summary>
    /// An extension method to configure and add the default windows performance counters publisher to services collection.
    /// </summary>
    /// <param name="builder">The tracker builder instance used to add the publisher.</param>
    /// <returns>Returns the input tracker builder for call chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddWindowsPerfCounterPublisher(this IResourceMonitorBuilder builder);

    /// <summary>
    /// An extension method that creates a few OpenTelemetry instruments for system counters.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>Returns the builder.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <seealso cref="T:System.Diagnostics.Metrics.Instrument" />.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.Experimental]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder);

    /// <summary>
    /// An extension method that creates a few OpenTelemetry instruments for system counters.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring of <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.WindowsCountersOptions" />.</param>
    /// <returns>Returns the builder.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <seealso cref="T:System.Diagnostics.Metrics.Instrument" />.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.Experimental]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder, IConfigurationSection section);

    /// <summary>
    /// An extension method that creates a few OpenTelemetry instruments for system counters.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">The delegate for configuring of <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.WindowsCountersOptions" />.</param>
    /// <returns>Returns the builder.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <seealso cref="T:System.Diagnostics.Metrics.Instrument" />.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.Experimental]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder, Action<WindowsCountersOptions> configure);
}
