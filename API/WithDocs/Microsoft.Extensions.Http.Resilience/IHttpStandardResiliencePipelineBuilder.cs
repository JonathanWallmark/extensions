// Assembly 'Microsoft.Extensions.Http.Resilience'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The builder for the standard HTTP pipeline.
/// </summary>
public interface IHttpStandardResiliencePipelineBuilder
{
    /// <summary>
    /// Gets the name of the pipeline configured by this builder.
    /// </summary>
    string PipelineName { get; }

    /// <summary>
    /// Gets the application service collection.
    /// </summary>
    IServiceCollection Services { get; }
}
