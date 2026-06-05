// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;

namespace Defra.Trade.Common.Function.Health.HealthChecks;

/// <summary>
/// Health check for he Trade Api Azure Service Bus queue connection.
/// </summary>
[ExcludeFromCodeCoverage]
public class ServiceBusQueueHealthCheck : IHealthCheck
{
    private readonly string _queueName;
    private readonly string _serviceBusConConfig;

    public ServiceBusQueueHealthCheck(string serviceBusConConfig, string queueName)
    {
        _queueName = queueName;
        _serviceBusConConfig = serviceBusConConfig;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var client = new ServiceBusClient(_serviceBusConConfig);
            await using var sender = client.CreateSender(_queueName);
            _ = client.FullyQualifiedNamespace;
            return HealthCheckResult.Healthy($"{context.Registration.Name} Service bus connection successful.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("The health check operation timed out");
        }
        catch (Exception ex)
        {
            var data = new Dictionary<string, object> { { "url", _queueName + "/health" } };
            return HealthCheckResult.Unhealthy($"Exception during check: {ex.GetType().FullName}", ex, data);
        }
    }
}
