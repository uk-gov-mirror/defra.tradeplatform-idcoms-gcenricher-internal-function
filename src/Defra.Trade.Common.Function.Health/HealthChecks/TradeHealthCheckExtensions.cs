// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Diagnostics.CodeAnalysis;
using Defra.Trade.Common.Function.Health.HealthChecks.ApiCheck;
using Defra.Trade.Common.Infra.Infrastructure;

namespace Defra.Trade.Common.Function.Health.HealthChecks;

[ExcludeFromCodeCoverage(Justification = "Unable to mock service provider. This can be looked at later stage")]
public static class TradeHealthCheckExtensions
{
    /// <summary>
    /// Health check for trade internal APIs.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    /// <param name="builder"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="apiPath"></param>
    /// <param name="sampleUserId">User in IDCOMS</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IHealthChecksBuilder AddCrmAdapterHealthCheck<TOptions>(
        this IHealthChecksBuilder builder,
        ServiceProvider serviceProvider,
        string apiPath,
        string sampleUserId)
        where TOptions : class, ITradeApiOptions
    {
        var apiOptions = serviceProvider.GetRequiredService<IOptions<TOptions>>();

        var options = apiOptions.Value;
        _ = options.BaseAddress ?? throw new InvalidOperationException($"Null {typeof(TOptions).Name} Base Address");

        string healthEndpoint = $"{options.BaseAddress}{apiPath}";
        builder.Add(new HealthCheckRegistration(
           "CrmAdapterAPI",
            sp => new CrmAdapterApiHealthCheck(serviceProvider, healthEndpoint, sampleUserId),
            failureStatus: default,
            tags: default,
            timeout: default));

        return builder;
    }

    /// <summary>
    /// Health check for trade internal APIs.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    /// <param name="builder"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="apiPath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IHealthChecksBuilder AddTradeInternalApiCheck<TOptions>(
        this IHealthChecksBuilder builder,
        ServiceProvider serviceProvider,
        string apiPath)
        where TOptions : class, ITradeApiOptions
    {
        var apiOptions = serviceProvider.GetRequiredService<IOptions<TOptions>>();

        var options = apiOptions.Value;
        _ = options.BaseAddress ?? throw new InvalidOperationException($"Null {typeof(TOptions).Name} Base Address");

        string healthEndpoint = $"{options.BaseAddress}{apiPath}";
        builder.Add(new HealthCheckRegistration(
          "CertificateStoreApi",
            sp => new TradeCertificateStoreApiHealthCheck(serviceProvider, healthEndpoint),
            failureStatus: default,
            tags: default,
            timeout: default));

        return builder;
    }


    public static IHealthChecksBuilder AddAzureServiceBusCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string serviceBusConnectionConfigPath,
        string queueName)
    {
        string servicesBusConnectionString = configuration.GetValue<string>(serviceBusConnectionConfigPath);
        string servicesBusQueueName = queueName;

        builder.Add(new HealthCheckRegistration(
           $"ServiceBus:{queueName}",
            sp => new ServiceBusQueueHealthCheck(servicesBusConnectionString, servicesBusQueueName),
            failureStatus: default,
            tags: default,
            timeout: default));
        return builder;
    }
}
