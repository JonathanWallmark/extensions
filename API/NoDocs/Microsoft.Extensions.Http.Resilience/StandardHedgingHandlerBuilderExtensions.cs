// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Resilience;

public static class StandardHedgingHandlerBuilderExtensions
{
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, IConfigurationSection section);
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions> configure);
    [System.Diagnostics.CodeAnalysis.Experimental]
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions, IServiceProvider> configure);
    public static IStandardHedgingHandlerBuilder SelectPipelineByAuthority(this IStandardHedgingHandlerBuilder builder, DataClassification classification);
    public static IStandardHedgingHandlerBuilder SelectPipelineBy(this IStandardHedgingHandlerBuilder builder, Func<IServiceProvider, PipelineKeySelector> selectorFactory);
}
