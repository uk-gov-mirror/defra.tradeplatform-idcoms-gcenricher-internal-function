// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Azure.Messaging.ServiceBus;
using Defra.Trade.API.CertificatesStore.V1.ApiClient.Api;
using Defra.Trade.API.CertificatesStore.V1.ApiClient.Model;
using Defra.Trade.Common.Functions.Isolated.Models;
using Defra.Trade.CrmAdapter.Api.V1.ApiClient.Api;
using Defra.Trade.CrmAdapter.Api.V1.ApiClient.Model;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Extensions;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Helpers;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services.Contracts;
using Microsoft.Extensions.Logging;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services;

/// <summary>
/// Object initializer.
/// </summary>
/// <param name="logger"></param>
/// <param name="establishmentApi"></param>
/// <param name="idcomsGeneralCertificateEnrichmentApi"></param>
/// <param name="mapper"></param>
/// <param name="serviceBusManagerClient"></param>
/// <exception cref="ArgumentNullException"></exception>
public class GcEnrichmentMessageProcessor(
    ILogger<GcEnrichmentMessageProcessor> logger,
    IEnrichmentApi establishmentApi,
    IIdcomsGeneralCertificateEnrichmentApi idcomsGeneralCertificateEnrichmentApi,
    IMapper mapper,
    IServiceBusManagerClient serviceBusManagerClient) : IGcEnrichmentMessageProcessor
{
    private readonly IEnrichmentApi _enrichmentApi = establishmentApi ?? throw new ArgumentNullException(nameof(establishmentApi));

    private readonly IIdcomsGeneralCertificateEnrichmentApi _idcomsGeneralCertificateEnrichmentApi = idcomsGeneralCertificateEnrichmentApi ??
                                                     throw new ArgumentNullException(nameof(idcomsGeneralCertificateEnrichmentApi));

    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly ILogger<GcEnrichmentMessageProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceBusManagerClient _serviceBusManagerClient = serviceBusManagerClient ?? throw new ArgumentNullException(nameof(serviceBusManagerClient));
    private string _gcId;

    private const string Label = "trade.remos.notification";
    private const string PublisherId = "GCEnricher";
    private const string SchemaVersion = "1";
    private const string Status = "Created";
    private const string Type = "Internal";
    private const string ContentType = "application/json";

    public async Task ProcessMessage(GcEnrichmentRequest gcMessageRequest, TradeEventMessageHeader messageHeader)
    {
        _gcId = gcMessageRequest.GcId;
        _logger.RequestingEnrichmentData(_gcId);

        var organisationIds = new List<Guid>
        {
            gcMessageRequest.Applicant.DefraCustomer.OrgId,
            gcMessageRequest.Consignee.DefraCustomer.OrgId,
            gcMessageRequest.Consignor.DefraCustomer.OrgId
        };

        var establishmentIds = new List<Guid>
        {
            gcMessageRequest.DestinationLocation.Idcoms.EstablishmentId,
            gcMessageRequest.DispatchLocation.Idcoms.EstablishmentId
        };

        var gcEnriched = new GcEnrichedData
        {
            GcId = gcMessageRequest.GcId,
            Applicant = await GetEnrichedContact(gcMessageRequest.Applicant.DefraCustomer.UserId)
        };

        foreach (var orgId in organisationIds.Distinct())
        {
            gcEnriched.Organisations.Add(await GetEnrichedOrganisation(orgId));
        }

        foreach (var establishmentId in establishmentIds.Distinct())
        {
            gcEnriched.Establishments.Add(await GetEnrichedEstablishment(establishmentId));
        }

        _logger.RequestingEnrichmentDataSuccess(_gcId);

        var gcStoreGcEnrichment = _mapper.Map<IdcomsGeneralCertificateEnrichment>(gcEnriched);

        _logger.SendingMessageToCertificateStore(_gcId);
        await _idcomsGeneralCertificateEnrichmentApi.SaveIDCOMSGeneralCertificateEnrichmentAsync(SchemaVersion, gcStoreGcEnrichment);
        _logger.SendingMessageToCertificateStoreSuccess(_gcId);

        var message = BuildMessage(gcMessageRequest, messageHeader);

        _logger.SendingMessageToNotifier(_gcId);
        await _serviceBusManagerClient.SendMessageAsync(message);
        _logger.SendingMessageToNotifierSuccess(_gcId);
    }

    private static ServiceBusMessage BuildMessage(GcEnrichmentRequest messageRequest, TradeEventMessageHeader messageHeader)
    {
        var toSend = new GCNotifierPayload { GcId = messageRequest.GcId };
        var messageToSend = new ServiceBusMessage(BinaryData.FromBytes(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(toSend, SerializerOptions.GetSerializerOptions()))))
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = messageHeader.CorrelationId,
            ContentType = ContentType,
            Subject = Label,
        };
        messageToSend.ApplicationProperties.Add("EntityKey", messageRequest.GcId);
        messageToSend.ApplicationProperties.Add("CausationId", messageHeader.MessageId);
        messageToSend.ApplicationProperties.Add("PublisherId", PublisherId);
        messageToSend.ApplicationProperties.Add("SchemaVersion", SchemaVersion);
        messageToSend.ApplicationProperties.Add("Status", Status);
        messageToSend.ApplicationProperties.Add("Type", Type);
        messageToSend.ApplicationProperties.Add("TimestampUtc", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString());
        return messageToSend;
    }

    private async Task<CustomerContact> GetEnrichedContact(Guid userId)
    {
        EnrichedContact customerContact;
        try
        {
            customerContact = await _enrichmentApi.EnrichContactDetailsAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.RequestingEnrichmentDataContactFailure(
                ex,
                nameof(userId),
                _gcId,
                userId.ToString());

            throw;
        }

        var enrichedCustomerContact = _mapper.Map<CustomerContact>(customerContact);
        return enrichedCustomerContact;
    }

    private async Task<Organisation> GetEnrichedOrganisation(Guid organisationId)
    {
        EnrichedTrader enrichedTrader;
        try
        {
            enrichedTrader = await _enrichmentApi.EnrichTraderDetailsAsync(organisationId);
        }
        catch (Exception ex)
        {
            _logger.RequestingEnrichmentDataContactFailure(
                ex,
                nameof(organisationId),
                _gcId,
                organisationId.ToString());

            throw;
        }
        var enrichedOrganisation = _mapper.Map<Organisation>(enrichedTrader);
        return enrichedOrganisation;
    }

    private async Task<Establishment> GetEnrichedEstablishment(Guid establishmentId)
    {
        EnrichedEstablishment enrichedEstablishment;
        try
        {
            enrichedEstablishment = await _enrichmentApi.EnrichEstablishmentDetailsAsync(establishmentId);
        }
        catch (Exception ex)
        {
            _logger.RequestingEnrichmentDataContactFailure(
                ex,
                nameof(establishmentId),
                _gcId,
                establishmentId.ToString());

            throw;
        }

        var establishmentDetails = _mapper.Map<Establishment>(enrichedEstablishment);
        return establishmentDetails;
    }
}
