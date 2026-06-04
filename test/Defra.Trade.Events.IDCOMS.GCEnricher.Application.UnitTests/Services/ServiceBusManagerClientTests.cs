// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Azure.Messaging.ServiceBus;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services.Contracts;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.UnitTests.Services;

public class ServiceBusManagerClientTests
{
    private readonly Mock<IQueueClientFactory> _queueClientFactory;
    private readonly Mock<ServiceBusSender> _sender;
    private readonly ServiceBusManagerClient _sut;

    public ServiceBusManagerClientTests()
    {
        _queueClientFactory = new Mock<IQueueClientFactory>();
        _sender = new Mock<ServiceBusSender>();
        _queueClientFactory.Setup(x => x.CreateNotifierQueueClient()).Returns(_sender.Object);
        _sut = new ServiceBusManagerClient(_queueClientFactory.Object);
    }

    [Fact]
    public async Task ServiceBusManagerClient_Should_SendMessage()
    {
        var message = new ServiceBusMessage();
        _sender.Setup(s => s.SendMessageAsync(message, default)).Returns(Task.CompletedTask).Verifiable();

        await _sut.SendMessageAsync(message);

        _queueClientFactory.Verify(x => x.CreateNotifierQueueClient(), Times.Once());
        _sender.Verify(s => s.SendMessageAsync(message, default), Times.Once());
    }
}
