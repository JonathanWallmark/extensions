// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Provides extension methods for <see cref="T:Polly.Context" />.
/// </summary>
[System.Diagnostics.CodeAnalysis.Experimental]
public static class PolicyContextExtensions
{
    /// <summary>
    /// Associates the given <see cref="T:Polly.Context" /> instance to the <paramref name="request" />.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <param name="request">The calling request.</param>
    /// <returns>
    /// The <see cref="T:Polly.Context" /> so that additional calls can be chained.
    /// </returns>
    public static Context WithCallingRequestMessage(this Context context, HttpRequestMessage request);
}
