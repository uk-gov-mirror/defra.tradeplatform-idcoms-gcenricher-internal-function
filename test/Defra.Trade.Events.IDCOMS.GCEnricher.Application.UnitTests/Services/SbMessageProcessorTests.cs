// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using AutoFixture.AutoMoq;
using AutoFixture.Idioms;
using AutoFixture.Xunit2;
using Azure.Messaging.ServiceBus;
using Defra.Trade.Common.Functions.Isolated.Interfaces;
using Defra.Trade.Common.Functions.Isolated.Models;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services.Contracts;
using Defra.Trade.Events.IDCOMS.GCEnricher.Tests.Common;
using Microsoft.Extensions.Logging;
using CertificateStoreClient = Defra.Trade.API.CertificatesStore.V1.ApiClient.Client;
using CrmAdapterClient = Defra.Trade.CrmAdapter.Api.V1.ApiClient.Client;
using Times = Moq.Times;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.UnitTests.Services;

public class SbMessageProcessorTests
{
    private readonly IFixture _fixture;
    private readonly TradeEventMessageHeader _messageHeader;
    private readonly Mock<ILogger<SbMessageProcessor>> _mockLogger;
    private readonly Mock<IGcEnrichmentMessageProcessor> _mockMessageProcessor;
    private readonly Mock<IMessageRetryContextAccessor> _mockRetryAccessor;
    private readonly Mock<ServiceBusReceivedMessage> _mockMessage;
    private readonly SbMessageProcessor _sut;

    public SbMessageProcessorTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _mockMessageProcessor = new Mock<IGcEnrichmentMessageProcessor>();
        _mockLogger = new Mock<ILogger<SbMessageProcessor>>();
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);
        _mockRetryAccessor = new Mock<IMessageRetryContextAccessor>();
        _mockMessage = _fixture.Freeze<Mock<ServiceBusReceivedMessage>>();

        _sut = new SbMessageProcessor(_mockLogger.Object, _mockMessageProcessor.Object, _mockRetryAccessor.Object);
        _messageHeader = new TradeEventMessageHeader { MessageId = "messageId" };
    }

    [Fact]
    public void Ctors_EnsureNotNullAndCorrectExceptionParameterName()
    {
        var assertion = new GuardClauseAssertion(_fixture);
        assertion.Verify(typeof(SbMessageProcessor).GetConstructors());
    }

    [Fact]
    public async Task Process_BuildCustomMessageHeaderAsync_Should_Not_Be_Null()
    {
        var result = await _sut.BuildCustomMessageHeaderAsync();
        result.ShouldNotBeNull();
    }

    [Theory, AutoData]
    public async Task Process_CustomerPublisherMessageProcessor_ValidateMessageLabelAsync(TradeEventMessageHeader messageHeader)
    {
        messageHeader.Label = GcMessageConstants.BrokerLabel;
        bool result = await _sut.ValidateMessageLabelAsync(messageHeader);
        result.ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task Process_CustomerPublisherMessageProcessor_ValidateMessageLabelAsync_Not_Relevant_Label(TradeEventMessageHeader messageHeader)
    {
        messageHeader.Label = "invalid-label";
        bool result = await _sut.ValidateMessageLabelAsync(messageHeader);
        result.ShouldBeFalse();
    }

    [Theory, AutoData]
    public async Task Process_GetSchemaAsync_Should_Not_Be_Null(TradeEventMessageHeader messageHeader)
    {
        string result = await _sut.GetSchemaAsync(messageHeader);
        result.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(599)]
    public async Task ProcessMessage_WhenCrmAdapterApiException_ShouldThrowActualAndRetry(int errorCode)
    {
        var mockedGcCommand = new GcEnrichmentRequest();
        var mockException = new CrmAdapterClient.ApiException(errorCode, "mocked error");

        _mockMessageProcessor
            .Setup(x => x.ProcessMessage(mockedGcCommand, _messageHeader))
            .Throws(mockException);

        await Assert.ThrowsAsync<CrmAdapterClient.ApiException>(
            async () => await _sut.ProcessAsync(mockedGcCommand, _messageHeader));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(599)]
    public async Task ProcessMessage_WhenCertificateStoreApiException_ShouldThrowActualAndRetry(int errorCode)
    {
        var mockedGcCommand = new GcEnrichmentRequest();
        var mockException = new CertificateStoreClient.ApiException(errorCode, "mocked error");

        _mockMessageProcessor
            .Setup(x => x.ProcessMessage(mockedGcCommand, _messageHeader))
            .Throws(mockException);

        await Assert.ThrowsAsync<CertificateStoreClient.ApiException>(
            async () => await _sut.ProcessAsync(mockedGcCommand, _messageHeader));
    }

    [Fact]
    public async Task ProcessMessage_WhenValidJson_ShouldParse()
    {
        var mockedGcCommand = new GcEnrichmentRequest();
        _mockMessageProcessor.Setup(x => x.ProcessMessage(mockedGcCommand, _messageHeader))
            .Returns(Task.CompletedTask);

        await _sut.ProcessAsync(mockedGcCommand, _messageHeader);

        _mockMessageProcessor.Verify(x => x.ProcessMessage(mockedGcCommand, _messageHeader), Times.Once());
    }
}
