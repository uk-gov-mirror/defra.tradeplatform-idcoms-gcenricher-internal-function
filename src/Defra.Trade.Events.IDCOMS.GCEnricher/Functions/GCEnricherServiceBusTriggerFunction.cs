// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Azure.Messaging.ServiceBus;
using Defra.Trade.Common.Functions;
using Defra.Trade.Common.Functions.Interfaces;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Functions;

public class GcEnricherServiceBusTriggerFunction
{
    private readonly IBaseMessageProcessorService<GcEnrichmentInbound> _baseMessageProcessorService;
    private readonly IMessageRetryService _retry;

    public GcEnricherServiceBusTriggerFunction(IBaseMessageProcessorService<GcEnrichmentInbound> baseMessageProcessorService, IMessageRetryService retry)
    {
        ArgumentNullException.ThrowIfNull(baseMessageProcessorService);
        ArgumentNullException.ThrowIfNull(retry);
        _baseMessageProcessorService = baseMessageProcessorService;
        _retry = retry;
    }

    [ServiceBusAccount(GcEnricherSettings.ConnectionStringConfigurationKey)]
    [FunctionName(nameof(GcEnricherServiceBusTriggerFunction))]
    public async Task RunAsync(
        [ServiceBusTrigger(queueName: GcEnricherSettings.DefaultQueueName, IsSessionsEnabled = false)] ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        ExecutionContext executionContext,
        [ServiceBus(GcEnricherSettings.TradeEventInfo)] IAsyncCollector<ServiceBusMessage> eventStoreCollector,
        [ServiceBus(GcEnricherSettings.DefaultQueueName)] IAsyncCollector<ServiceBusMessage> retryQueue,
        ILogger logger)
    {
        _retry.SetContext(message, retryQueue);
        await RunInternalAsync(message, messageActions, eventStoreCollector, executionContext, logger);
    }

    private static string GetGcId(BinaryData messageBody)
    {
        var gcInbound = JsonConvert.DeserializeObject<dynamic>(messageBody.ToString());
        return gcInbound?.exchangedDocument?.id ?? string.Empty;
    }

    private async Task RunInternalAsync(ServiceBusReceivedMessage message,
                           ServiceBusMessageActions messageReceiver,
                       IAsyncCollector<ServiceBusMessage> eventStoreCollector,
                       ExecutionContext executionContext,
                       ILogger logger)
    {
        try
        {
            string gcId = GetGcId(message.Body);

            logger.MessageReceived(message.MessageId, executionContext.FunctionName, gcId);

            await _baseMessageProcessorService.ProcessAsync(executionContext.InvocationId.ToString(),
                 GcEnricherSettings.DefaultQueueName,
                 GcEnricherSettings.PublisherId,
                 message,
                 messageReceiver,
                 eventStoreCollector,
                 originalCrmPublisherId: GcEnricherSettings.PublisherId,
                 originalSource: GcEnricherSettings.DefaultQueueName,
                 originalRequestName: "Create");

            logger.ProcessMessageSuccess(message.MessageId, executionContext.FunctionName, gcId);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, ex.Message);
        }
    }
}
