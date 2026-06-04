// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using AutoFixture.AutoMoq;
using AutoFixture.Idioms;
using Azure.Messaging.ServiceBus;
using Defra.Trade.API.CertificatesStore.V1.ApiClient.Api;
using Defra.Trade.Common.Functions.Isolated.Models;
using Defra.Trade.CrmAdapter.Api.V1.ApiClient.Api;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services.Contracts;
using Defra.Trade.Events.IDCOMS.GCEnricher.Tests.Common;
using Microsoft.Extensions.Logging;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.UnitTests.Services;

public class GcEnrichmentMessageProcessorTests
{
    private readonly Mock<ILogger<GcEnrichmentMessageProcessor>> _logger;
    private readonly Mock<IIdcomsGeneralCertificateEnrichmentApi> _idcomsGeneralCertificateEnrichmentApi;
    private readonly Mock<IEnrichmentApi> _enrichmentApi;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<IServiceBusManagerClient> _serviceBusMessageClientMock;
    private readonly IFixture _fixture;
    private readonly GcEnrichmentMessageProcessor _sut;

    public GcEnrichmentMessageProcessorTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _logger = new Mock<ILogger<GcEnrichmentMessageProcessor>>();
        _logger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);
        _logger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        _enrichmentApi = new Mock<IEnrichmentApi>();
        _serviceBusMessageClientMock = new Mock<IServiceBusManagerClient>();
        _mapper = new Mock<IMapper>();
        _idcomsGeneralCertificateEnrichmentApi = new Mock<IIdcomsGeneralCertificateEnrichmentApi>();
        _sut = new GcEnrichmentMessageProcessor(
            _logger.Object, _enrichmentApi.Object,
            _idcomsGeneralCertificateEnrichmentApi.Object,
            _mapper.Object,
            _serviceBusMessageClientMock.Object);
    }

    [Fact]
    public void Ctors_EnsureNotNullAndCorrectExceptionParameterName()
    {
        var assertion = new GuardClauseAssertion(_fixture);
        assertion.Verify(typeof(SbMessageProcessor).GetConstructors());
    }

    [Fact]
    public async Task ProcessMessage_WhenCalled_ShouldProcessToStore()
    {
        // Arrange
        var requestMessage = _fixture.Create<GcEnrichmentRequest>();
        var requestHeader = _fixture.Create<TradeEventMessageHeader>();

        // Act
        await _sut.ProcessMessage(requestMessage, requestHeader);

        // Assert
        _logger.VerifyLogged(
             $"Sending notification for GC with ID : {requestMessage.GcId} to GC Notifier",
            LogLevel.Information);

        _serviceBusMessageClientMock.Verify(x =>
            x.SendMessageAsync(It.IsAny<ServiceBusMessage>()), Times.Once());
    }

    [Fact]
    public async Task ProcessMessage_WhenCalledWithInvalidUserId_ShouldNotProcessToStore()
    {
        // Arrange
        var mockedException = new Exception();
        var requestMessage = _fixture.Create<GcEnrichmentRequest>();
        var requestHeader = _fixture.Create<TradeEventMessageHeader>();
        _enrichmentApi.Setup(x =>
                x.EnrichContactDetailsAsync(requestMessage.Applicant.DefraCustomer.UserId, null, It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(mockedException);

        // Act
        var act = async () => await _sut.ProcessMessage(requestMessage, requestHeader);

        // Assert
        var result = await act.ShouldThrowAsync<Exception>();
        result.ShouldBe(mockedException);
        _logger.VerifyLogged(
            $"Enriched userId data request failed for GC with ID : {requestMessage.GcId}. Contact ID : {requestMessage.Applicant.DefraCustomer.UserId}",
            LogLevel.Error);

        _enrichmentApi.Verify(x =>
            x.EnrichContactDetailsAsync(requestMessage.Applicant.DefraCustomer.UserId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

        _serviceBusMessageClientMock.Verify(x =>
            x.SendMessageAsync(It.IsAny<ServiceBusMessage>()), Times.Never());
    }

    [Fact]
    public async Task ProcessMessage_WhenCalledWithInvalidOrganisation_ShouldNotProcessToStore()
    {
        // Arrange
        var mockedException = new Exception();
        var requestMessage = _fixture.Create<GcEnrichmentRequest>();
        var requestHeader = _fixture.Create<TradeEventMessageHeader>();
        _enrichmentApi.Setup(x =>
                x.EnrichTraderDetailsAsync(It.IsAny<Guid>(), null, It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(mockedException);

        // Act
        var act = async () => await _sut.ProcessMessage(requestMessage, requestHeader);

        // Assert
        var result = await act.ShouldThrowAsync<Exception>();
        result.ShouldBe(mockedException);
        _logger.VerifyLogged(
            $"Enriched organisationId data request failed for GC with ID : {requestMessage.GcId}. Contact ID : {requestMessage.Applicant.DefraCustomer.OrgId}",
            LogLevel.Error);

        _enrichmentApi.Verify(x => x.EnrichContactDetailsAsync(requestMessage.Applicant.DefraCustomer.UserId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _enrichmentApi.Verify(x => x.EnrichTraderDetailsAsync(requestMessage.Applicant.DefraCustomer.OrgId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _serviceBusMessageClientMock.Verify(x =>
            x.SendMessageAsync(It.IsAny<ServiceBusMessage>()), Times.Never());
    }

    [Fact]
    public async Task ProcessMessage_WhenCalledWithInvalidEstablishment_ShouldNotProcessToStore()
    {
        // Arrange
        var mockedException = new Exception();
        var requestMessage = _fixture.Create<GcEnrichmentRequest>();
        var requestHeader = _fixture.Create<TradeEventMessageHeader>();
        _enrichmentApi.Setup(x =>
                x.EnrichEstablishmentDetailsAsync(It.IsAny<Guid>(), null, It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(mockedException);

        // Act
        var act = async () => await _sut.ProcessMessage(requestMessage, requestHeader);

        // Assert
        var result = await act.ShouldThrowAsync<Exception>();
        result.ShouldBe(mockedException);
        _logger.VerifyLogged(
             $"Enriched establishmentId data request failed for GC with ID : {requestMessage.GcId}. Contact ID : {requestMessage.DestinationLocation.Idcoms.EstablishmentId}",
            LogLevel.Error);

        _enrichmentApi.Verify(x => x.EnrichContactDetailsAsync(requestMessage.Applicant.DefraCustomer.UserId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _enrichmentApi.Verify(x => x.EnrichTraderDetailsAsync(requestMessage.Consignee.DefraCustomer.OrgId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _enrichmentApi.Verify(x => x.EnrichTraderDetailsAsync(requestMessage.Consignor.DefraCustomer.OrgId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
