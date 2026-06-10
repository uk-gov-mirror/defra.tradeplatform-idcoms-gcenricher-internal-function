// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models.Settings;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services;

public class QueueClientFactory(IOptions<ServiceBusQueuesSettings> serviceBusQueuesSettings, ServiceBusClient client) : IQueueClientFactory, IAsyncDisposable
{
    private readonly IOptions<ServiceBusQueuesSettings> _serviceBusQueuesSettings = serviceBusQueuesSettings ?? throw new ArgumentNullException(nameof(serviceBusQueuesSettings));
    private readonly ServiceBusClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public ServiceBusSender CreateNotifierQueueClient()
    {
        var settings = _serviceBusQueuesSettings.Value;
        return _senders.GetOrAdd(settings.QueueNameEhcoRemosNotification, key => _client.CreateSender(key));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
