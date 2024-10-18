// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;

public class GcEnricherSettings
{
    public const string GcEnricherSettingsSettingsName = "IDCOMSGCEnricher";

    public const string AppName = "Defra.Trade.Events.IDCOMS.GCEnricher";

#if DEBUG

    // In 'Debug' (locally) use connection string
    public const string ConnectionStringConfigurationKey = "ServiceBus:ConnectionString";

#else
        // Assumes that this is 'Release' and uses Managed Identity rather than connection string
        // ie it will actually bind to ServiceBus:FullyQualifiedNamespace !
        public const string ConnectionStringConfigurationKey = "ServiceBus";
#endif

    public string GcCreatedQueue { get; set; } = DefaultQueueName;

    public const string PublisherId = "TradeApi";

    public const string AppConfigSentinelName = "Sentinel";

    public const string DefaultQueueName = "defra.trade.ehco.remos.enrichment";

    /// <summary>
    /// Number attempts messages needs retrying.
    /// </summary>
    public const int DefaultQueueRetryAttempts = 10;

    /// <summary>
    /// Time in seconds between each retry attempt of particular message.
    /// </summary>
    public const int DefaultQueueRetryIntervalInSeconds = 30;

    public const string TradeEventInfo = Common.Functions.Constants.QueueName.DefaultEventsInfoQueueName;

    public static class MessageRetry
    {
        public const int EnqueueTimeInSeconds = 30;
        public const int RetryWindowInSeconds = 300;
    }
}
