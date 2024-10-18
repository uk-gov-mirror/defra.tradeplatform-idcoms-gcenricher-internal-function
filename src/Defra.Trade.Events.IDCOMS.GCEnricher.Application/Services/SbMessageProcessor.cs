// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Defra.Trade.Common.Functions.Extensions;
using Defra.Trade.Common.Functions.Interfaces;
using Defra.Trade.Common.Functions.Models;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Extensions;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services.Contracts;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using CertificateStoreClient = Defra.Trade.API.CertificatesStore.V1.ApiClient.Client;
using CrmAdapterClient = Defra.Trade.CrmAdapter.Api.V1.ApiClient.Client;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services;

/// <summary>
/// Object initializer.
/// </summary>
/// <param name="gcEnrichmentMessageProcessor"></param>
/// <param name="logger"></param>
/// <exception cref="ArgumentNullException"></exception>
public class SbMessageProcessor(
    ILogger<SbMessageProcessor> logger,
    IGcEnrichmentMessageProcessor
    gcEnrichmentMessageProcessor,
    IMessageRetryContextAccessor retry) : IMessageProcessor<GcEnrichmentRequest, TradeEventMessageHeader>
{
    private readonly IGcEnrichmentMessageProcessor _gcEnrichmentMessageProcessor = gcEnrichmentMessageProcessor ?? throw new ArgumentNullException(nameof(gcEnrichmentMessageProcessor));
    private readonly ILogger<SbMessageProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TimeSpan _messageRetryEnqueueTime = new(0, 0, 0, GcEnricherSettings.MessageRetry.EnqueueTimeInSeconds);
    private readonly TimeSpan _messageRetryWindow = new(0, 0, 0, GcEnricherSettings.MessageRetry.RetryWindowInSeconds);
    private readonly IMessageRetryContextAccessor _retry = retry ?? throw new ArgumentNullException(nameof(retry));

    public Task<CustomMessageHeader> BuildCustomMessageHeaderAsync()
    {
        return Task.FromResult(new CustomMessageHeader());
    }

    public Task<string> GetSchemaAsync(TradeEventMessageHeader messageHeader)
    {
        return Task.FromResult(string.Empty);
    }

    public async Task<StatusResponse<GcEnrichmentRequest>> ProcessAsync(GcEnrichmentRequest messageRequest, TradeEventMessageHeader messageHeader)
    {
        try
        {
            _logger.ProcessingEnrichment(messageRequest.GcId);
            await _gcEnrichmentMessageProcessor.ProcessMessage(messageRequest, messageHeader);
            _logger.ProcessingEnrichmentSuccess(messageRequest.GcId);
        }
        catch (Exception ex) when (IsInternalServerError(ex) && _retry.Context is { } context)
        {
            _logger.ProcessingEnrichmentFailed(
                ex,
                messageRequest.GcId,
                context.Message.RetryCount());

            await context.RetryMessage(_messageRetryWindow, _messageRetryEnqueueTime, ex);
        }
        catch (ServiceBusCommunicationException ex) when (_retry.Context is { } context)
        {
            _logger.SendingMessageToNotifierFailure(
                ex,
                messageRequest.GcId,
                context.Message.RetryCount());
        }

        return new StatusResponse<GcEnrichmentRequest> { ForwardMessage = false, Response = messageRequest };
    }

    private static bool IsInternalServerError(Exception ex)
    {
        bool result = false;

        if (ex is CertificateStoreClient.ApiException certificateEx)
        {
            result = (certificateEx.ErrorCode >= 500 && certificateEx.ErrorCode <= 599) || certificateEx.ErrorCode == 0;
        }
        else if (ex is CrmAdapterClient.ApiException crmEx)
        {
            result = (crmEx.ErrorCode >= 500 && crmEx.ErrorCode <= 599) || crmEx.ErrorCode == 0;
        }

        return result;
    }

    public Task<bool> ValidateMessageLabelAsync(TradeEventMessageHeader messageHeader)
        => Task.FromResult(messageHeader.Label != null && messageHeader.Label.Equals(GcMessageConstants.BrokerLabel, StringComparison.OrdinalIgnoreCase));
}
