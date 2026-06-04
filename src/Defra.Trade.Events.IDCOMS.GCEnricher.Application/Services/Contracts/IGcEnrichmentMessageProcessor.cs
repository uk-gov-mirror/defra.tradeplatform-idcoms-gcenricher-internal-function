// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Defra.Trade.Common.Functions.Isolated.Models;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services.Contracts;

/// <summary>
/// GC Enricher - GC payload processor
/// </summary>
public interface IGcEnrichmentMessageProcessor
{
    /// <summary>
    /// Process GC message from SB queue.
    /// </summary>
    /// <param name="gcMessageRequest"></param>
    /// <param name="messageHeader"></param>
    /// <returns></returns>
    Task ProcessMessage(GcEnrichmentRequest gcMessageRequest, TradeEventMessageHeader messageHeader);
}