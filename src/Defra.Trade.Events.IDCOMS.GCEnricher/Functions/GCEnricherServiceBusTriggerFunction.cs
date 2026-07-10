// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Azure.Messaging.ServiceBus;
using Defra.Trade.Common.Functions.Isolated;
using Defra.Trade.Common.Functions.Isolated.Interfaces;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Functions;

public class GcEnricherServiceBusTriggerFunction
{
    private const string FunctionName = nameof(GcEnricherServiceBusTriggerFunction);

    private readonly IBaseMessageProcessorService<GcEnrichmentInbound> _baseMessageProcessorService;
    private readonly IMessageRetryService _retry;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<GcEnricherServiceBusTriggerFunction> _logger;

    public GcEnricherServiceBusTriggerFunction(
        IBaseMessageProcessorService<GcEnrichmentInbound> baseMessageProcessorService,
        IMessageRetryService retry,
        ServiceBusClient serviceBusClient,
        ILogger<GcEnricherServiceBusTriggerFunction> logger)
    {
        ArgumentNullException.ThrowIfNull(baseMessageProcessorService);
        ArgumentNullException.ThrowIfNull(retry);
        ArgumentNullException.ThrowIfNull(serviceBusClient);
        ArgumentNullException.ThrowIfNull(logger);
        _baseMessageProcessorService = baseMessageProcessorService;
        _retry = retry;
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    [Function(FunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger(GcEnricherSettings.DefaultQueueName, Connection = GcEnricherSettings.ConnectionStringConfigurationKey, IsSessionsEnabled = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        FunctionContext context)
    {
        await using var retrySender = _serviceBusClient.CreateSender(GcEnricherSettings.DefaultQueueName);
        await using var outboundSender = _serviceBusClient.CreateSender(GcEnricherSettings.TradeEventInfo);

        _retry.SetContext(message, retrySender);

        try
        {
            string gcId = GetGcId(message.Body);

            _logger.MessageReceived(message.MessageId, FunctionName, gcId);

            await _baseMessageProcessorService.ProcessAsync(
                context.InvocationId,
                GcEnricherSettings.DefaultQueueName,
                GcEnricherSettings.PublisherId,
                message,
                messageActions,
                outboundSender,
                GcEnricherSettings.TradeEventInfo,
                originalCrmPublisherId: GcEnricherSettings.PublisherId,
                originalSource: GcEnricherSettings.DefaultQueueName,
                originalRequestName: "Create");

            _logger.ProcessMessageSuccess(message.MessageId, FunctionName, gcId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }
    }

    private static string GetGcId(BinaryData messageBody)
    {
        var gcInbound = JsonConvert.DeserializeObject<dynamic>(messageBody.ToString());
        return gcInbound?.exchangedDocument?.id ?? string.Empty;
    }
}
