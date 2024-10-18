// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using AutoFixture;
using AutoFixture.AutoMoq;
using Azure.Messaging.ServiceBus;
using Defra.Trade.Common.Functions;
using Defra.Trade.Common.Functions.Interfaces;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Functions;
using Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.FunctionTestExtensions;
using Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.Helpers;
using FakeItEasy;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Shouldly;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using Times = Moq.Times;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.Functions;

public class GcEnricherServiceBusTriggerFunctionTests
{
    private readonly GcEnricherServiceBusTriggerFunction _sut;
    private readonly Mock<ILogger> _logger;
    private readonly Mock<IBaseMessageProcessorService<GcEnrichmentInbound>> _mockBaseMessageProcessorService;
    private readonly Mock<ServiceBusMessageActions> _mockServiceBusMessageActions;
    private readonly IMessageRetryService _retry;

    public GcEnricherServiceBusTriggerFunctionTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        _retry = A.Fake<IMessageRetryService>(opt => opt.Strict());
        _mockBaseMessageProcessorService = fixture.Freeze<Mock<IBaseMessageProcessorService<GcEnrichmentInbound>>>();
        _logger = fixture.Freeze<Mock<ILogger>>();
        _mockServiceBusMessageActions = new Mock<ServiceBusMessageActions>();

        _sut = fixture.Create<GcEnricherServiceBusTriggerFunction>();
    }

    [Fact]
    public void RunAsync_HasServiceBusTrigger_WithCorrectProperties()
    {
        FunctionTriggerAssertionHelpers.ShouldHaveServiceBusTrigger<GcEnricherServiceBusTriggerFunction>(
            nameof(GcEnricherServiceBusTriggerFunction.RunAsync), GcEnricherSettings.DefaultQueueName, "ServiceBusConnectionString");
    }

    [Fact]
    public void RunAsync_WhenTrigger_ShouldCallMessageProcessor()
    {
        // Arrange
        const string Json = "{}";

        var message = new ServiceBusReceivedMessageBuilder().WithBody(BinaryData.FromString(Json)).Build();

        var executionContext = new Mock<ExecutionContext>();
        var retryQueue = A.Fake<IAsyncCollector<ServiceBusMessage>>(opt => opt.Strict());
        var setRetryContext = A.CallTo(() => _retry.SetContext(message, retryQueue));

        _mockBaseMessageProcessorService
            .Setup(x => x.ProcessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                message,
                _mockServiceBusMessageActions.Object,
                It.IsAny<IAsyncCollector<ServiceBusMessage>>(),
                It.IsAny<IAsyncCollector<ServiceBusMessage>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(false)
            .Verifiable();
        setRetryContext.DoesNothing();

        // Act
        var result = _sut.RunAsync(message, _mockServiceBusMessageActions.Object, executionContext.Object, null, retryQueue, _logger.Object);

        // Assert
        _ = result.ShouldNotBeNull();
        setRetryContext.MustNotHaveHappened();
        result.Status.ShouldBe(TaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task RunAsync_WhenTriggeredWithInvalidMessage_ShouldThrowException()
    {
        // Arrange
        const string Json = "invalid-json";

        var message = new ServiceBusReceivedMessageBuilder().WithBody(BinaryData.FromString(Json)).Build();
        var exception = new Exception();
        var executionContext = new Mock<ExecutionContext>();
        var retryQueue = A.Fake<IAsyncCollector<ServiceBusMessage>>(opt => opt.Strict());
        var setRetryContext = A.CallTo(() => _retry.SetContext(message, retryQueue));

        _mockBaseMessageProcessorService.Setup(
            x => x.ProcessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<ServiceBusMessageActions>(),
                It.IsAny<IAsyncCollector<ServiceBusMessage>>(),
                It.IsAny<IAsyncCollector<ServiceBusMessage>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())).Throws(exception);
        setRetryContext.DoesNothing();

        // Act
        await _sut.RunAsync(message, _mockServiceBusMessageActions.Object, executionContext.Object, null, retryQueue, _logger.Object);

        // Assert
        _logger.Verify(
            l => l.Log(
                It.Is<LogLevel>(level => level == LogLevel.Critical),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((@object, type) => @object.ToString()!.Length != 0),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        setRetryContext.MustNotHaveHappened();
    }
}
