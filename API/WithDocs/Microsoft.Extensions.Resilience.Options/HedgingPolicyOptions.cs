// Assembly 'Microsoft.Extensions.Resilience'

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Hedging policy options.
/// </summary>
public class HedgingPolicyOptions
{
    /// <summary>
    /// A <see cref="T:System.TimeSpan" /> that represents the infinite hedging delay.
    /// </summary>
    public static readonly TimeSpan InfiniteHedgingDelay;

    /// <summary>
    /// Gets or sets the minimal time of waiting before spawning a new hedged call.
    /// </summary>
    /// <remarks>
    /// Default is set to 2 seconds.
    ///
    /// You can also use <see cref="F:System.TimeSpan.Zero" /> to create all hedged tasks (value of <see cref="P:Microsoft.Extensions.Resilience.Options.HedgingPolicyOptions.MaxHedgedAttempts" />) at once
    /// or <see cref="F:Microsoft.Extensions.Resilience.Options.HedgingPolicyOptions.InfiniteHedgingDelay" /> to force the hedging policy to never create new task before the old one is finished.
    ///
    /// If you want a greater control over hedging delay customization use <see cref="P:Microsoft.Extensions.Resilience.Options.HedgingPolicyOptions.HedgingDelayGenerator" />.
    /// </remarks>
    [TimeSpan(-1, Exclusive = false)]
    public TimeSpan HedgingDelay { get; set; }

    /// <summary>
    /// Gets or sets the delegate that is used to customize the hedging delays after each hedging task is created.
    /// </summary>
    /// <remarks>
    /// The <see cref="P:Microsoft.Extensions.Resilience.Options.HedgingPolicyOptions.HedgingDelayGenerator" /> takes precedence over <see cref="P:Microsoft.Extensions.Resilience.Options.HedgingPolicyOptions.HedgingDelay" />. If specified, the <see cref="P:Microsoft.Extensions.Resilience.Options.HedgingPolicyOptions.HedgingDelay" /> is ignored.
    ///
    /// By default, this value is <c>null</c>.
    /// </remarks>
    public Func<HedgingDelayArguments, TimeSpan>? HedgingDelayGenerator { get; set; }

    /// <summary>
    /// Gets or sets the maximum hedged attempts to perform the desired task.
    /// </summary>
    /// <remarks>
    /// Default set to 2.
    /// The value defines how many concurrent hedged tasks will be triggered by the policy.
    /// This includes the primary hedged task that is initially performed, and the further tasks that will
    /// be fetched from the provider and spawned in parallel.
    /// The value must be bigger or equal to 2, and lower or equal to 10.
    /// </remarks>
    [Range(2, 10)]
    public int MaxHedgedAttempts { get; set; }

    /// <summary>
    /// Gets or sets the exception predicate to filter the type of exception the policy can handle.
    /// </summary>
    /// <remarks>
    /// By default any exception will be retried.
    /// </remarks>
    [Required]
    public Predicate<Exception> ShouldHandleException { get; set; }

    /// <summary>
    /// Gets or sets the action to call asynchronously before invoking the hedging delegate.
    /// </summary>
    [Required]
    public Func<HedgingTaskArguments, Task> OnHedgingAsync { get; set; }

    public HedgingPolicyOptions();
}
