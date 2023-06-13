﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

internal static partial class Metric
{
    [Microsoft.Extensions.Telemetry.Metering.Counter(
        FaultInjectionEventMeterDimensions.FaultInjectionGroupName,
        FaultInjectionEventMeterDimensions.FaultType,
        FaultInjectionEventMeterDimensions.InjectedValue,
        FaultInjectionEventMeterDimensions.HttpContentKey,
        Name = @"R9\Resilience\FaultInjection\HttpClient\InjectedFaults")]
    public static partial HttpClientFaultInjectionMetricCounter CreateHttpClientFaultInjectionMetricCounter(Meter meter);
}