﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class PipelineConfigurationChangeTokenSource<TResult> : IOptionsChangeTokenSource<ResiliencePipelineFactoryOptions<TResult>>
{
    private static readonly NoopChangeToken _noopChangeToken = new();
    private readonly Func<IChangeToken> _changeTokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineConfigurationChangeTokenSource{TResult}"/> class.
    /// which provides the <see cref="IChangeToken"/>s so that the <see cref="IOptionsMonitor{ResiliencePipelineFactoryOptions}"/> gets
    /// notified when the options of a pipeline changed.
    /// A pipeline's options change when any component policy has its options changed.
    /// </summary>
    /// <param name="name">The name of the pipeline for which the token is monitored.</param>
    /// <param name="optionsMonitor">The instance handling named options with the sources for
    /// changed tokens triggered by the modified configurations of a policy.</param>
    public PipelineConfigurationChangeTokenSource(
        string name,
        IOptionsMonitor<ResiliencePipelineFactoryTokenSourceOptions<TResult>> optionsMonitor)
    {
        Name = name;

        var sources = optionsMonitor.Get(name).ChangeTokenSources;
        var sourcesCount = sources.Count;
        _changeTokenProvider = sourcesCount switch
        {
            0 => () => _noopChangeToken,
            1 => sources[0],
            _ => () => new CompositeChangeToken(sources.Select(sourceFunction => sourceFunction()).ToList())
        };
    }

    /// <summary>
    /// Gets the name of the option instance being changed.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Get a the reloaded token generated by a change in the underlying configuration.
    /// </summary>
    /// <returns>The reloadToken from the <see cref="IOptionsChangeTokenSource{TOptions}"/> of a policy.</returns>
    public IChangeToken GetChangeToken()
    {
        return _changeTokenProvider();
    }
}
