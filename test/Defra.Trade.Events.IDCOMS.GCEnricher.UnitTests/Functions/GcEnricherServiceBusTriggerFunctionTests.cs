// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using AutoFixture;
using AutoFixture.AutoMoq;
using Azure.Messaging.ServiceBus;
using Defra.Trade.Common.Functions.Isolated;
using Defra.Trade.Common.Functions.Isolated.Interfaces;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Functions;
using Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.FunctionTestExtensions;
using Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.Helpers;
using FakeItEasy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Shouldly;
using Times = Moq.Times;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.Functions;

public class GcEnricherServiceBusTriggerFunctionTests
{
    private readonly GcEnricherServiceBusTriggerFunction _sut;
    private readonly Mock<ILogger<GcEnricherServiceBusTriggerFunction>> _logger;
    private readonly Mock<IBaseMessageProcessorService<GcEnrichmentInbound>> _mockBaseMessageProcessorService;
    private readonly Mock<ServiceBusMessageActions> _mockServiceBusMessageActions;
    private readonly Mock<ServiceBusClient> _mockServiceBusClient;
    private readonly Mock<ServiceBusSender> _mockSender;
    private readonly IMessageRetryService _retry;

    public GcEnricherServiceBusTriggerFunctionTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        _retry = A.Fake<IMessageRetryService>(opt => opt.Strict());
        _mockBaseMessageProcessorService = fixture.Freeze<Mock<IBaseMessageProcessorService<GcEnrichmentInbound>>>();
        _logger = fixture.Freeze<Mock<ILogger<GcEnricherServiceBusTriggerFunction>>>();
        _mockServiceBusMessageActions = new Mock<ServiceBusMessageActions>();
        _mockSender = new Mock<ServiceBusSender>();
        _mockServiceBusClient = new Mock<ServiceBusClient>();
        _mockServiceBusClient.Setup(c => c.CreateSender(It.IsAny<string>())).Returns(_mockSender.Object);

        _sut = new GcEnricherServiceBusTriggerFunction(
            _mockBaseMessageProcessorService.Object,
            _retry,
            _mockServiceBusClient.Object,
            _logger.Object);
    }

    [Fact]
    public void RunAsync_HasServiceBusTrigger_WithCorrectProperties()
    {
        FunctionTriggerAssertionHelpers.ShouldHaveServiceBusTrigger<GcEnricherServiceBusTriggerFunction>(
            nameof(GcEnricherServiceBusTriggerFunction.RunAsync), GcEnricherSettings.DefaultQueueName, GcEnricherSettings.ConnectionStringConfigurationKey);
    }

    [Fact]
    public async Task RunAsync_WhenTrigger_ShouldCallMessageProcessor()
    {
        const string Json = "{}";

        var message = new ServiceBusReceivedMessageBuilder().WithBody(BinaryData.FromString(Json)).Build();
        var context = new Mock<FunctionContext>();
        context.SetupGet(c => c.InvocationId).Returns("invocation-id");
        var setRetryContext = A.CallTo(() => _retry.SetContext(message, A<ServiceBusSender>._));

        _mockBaseMessageProcessorService
            .Setup(x => x.ProcessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                message,
                _mockServiceBusMessageActions.Object,
                It.IsAny<ServiceBusSender>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(false)
            .Verifiable();
        setRetryContext.DoesNothing();

        await _sut.RunAsync(message, _mockServiceBusMessageActions.Object, context.Object);

        setRetryContext.MustHaveHappened();
        _mockBaseMessageProcessorService.Verify();
    }

    [Fact]
    public async Task RunAsync_WhenTriggeredWithInvalidMessage_ShouldThrowException()
    {
        const string Json = "invalid-json";

        var message = new ServiceBusReceivedMessageBuilder().WithBody(BinaryData.FromString(Json)).Build();
        var exception = new Exception();
        var context = new Mock<FunctionContext>();
        context.SetupGet(c => c.InvocationId).Returns("invocation-id");
        var setRetryContext = A.CallTo(() => _retry.SetContext(message, A<ServiceBusSender>._));

        _mockBaseMessageProcessorService.Setup(
            x => x.ProcessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<ServiceBusMessageActions>(),
                It.IsAny<ServiceBusSender>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())).Throws(exception);
        setRetryContext.DoesNothing();

        await _sut.RunAsync(message, _mockServiceBusMessageActions.Object, context.Object);

        _logger.Verify(
            l => l.Log(
                It.Is<LogLevel>(level => level == LogLevel.Critical),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((@object, type) => @object.ToString()!.Length != 0),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
