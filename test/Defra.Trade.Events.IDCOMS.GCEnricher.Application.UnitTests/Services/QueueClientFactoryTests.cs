// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Azure.Messaging.ServiceBus;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models.Settings;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.UnitTests.Services;

public class QueueClientFactoryTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=AQIDBAUGBwgJCgsMDQ4PEA==";
    private const string QueueName = "notifier-queue";

    private static QueueClientFactory CreateSut() => new(Options.Create(new ServiceBusQueuesSettings
    {
        ConnectionString = FakeConnectionString,
        QueueNameEhcoRemosNotification = QueueName,
    }), new ServiceBusClient(FakeConnectionString));

    [Fact]
    public void Ctor_NullSettings_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new QueueClientFactory(null!, new ServiceBusClient(FakeConnectionString)));
    }

    [Fact]
    public void CreateNotifierQueueClient_ReturnsSenderForConfiguredQueue()
    {
        var sut = CreateSut();

        var sender = sut.CreateNotifierQueueClient();

        sender.ShouldNotBeNull();
        sender.EntityPath.ShouldBe(QueueName);
    }

    [Fact]
    public void CreateNotifierQueueClient_CalledTwice_ReturnsSameSender()
    {
        var sut = CreateSut();

        var first = sut.CreateNotifierQueueClient();
        var second = sut.CreateNotifierQueueClient();

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public async Task DisposeAsync_DisposesCleanly()
    {
        var sut = CreateSut();
        var sender = sut.CreateNotifierQueueClient();

        await sut.DisposeAsync();

        sender.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public async Task DisposeAsync_WithoutCreatingSender_DisposesCleanly()
    {
        var sut = CreateSut();

        await Should.NotThrowAsync(async () => await sut.DisposeAsync());
    }
}
