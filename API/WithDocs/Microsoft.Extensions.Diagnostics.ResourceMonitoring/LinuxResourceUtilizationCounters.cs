// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// The names of instruments published by this package.
/// </summary>
/// <remarks>
/// <seealso cref="T:System.Diagnostics.Metrics.Instrument" />.
/// </remarks>
public static class LinuxResourceUtilizationCounters
{
    /// <summary>
    /// Gets CPU consumption by running application in percentages.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="T:System.Diagnostics.Metrics.ObservableGauge`1" /> (long).
    /// </remarks>
    public static string CpuConsumptionPercentage { get; }

    /// <summary>
    /// Gets memory consumption by running application in percentages.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="T:System.Diagnostics.Metrics.ObservableGauge`1" /> (long).
    /// </remarks>
    public static string MemoryConsumptionPercentage { get; }
}
