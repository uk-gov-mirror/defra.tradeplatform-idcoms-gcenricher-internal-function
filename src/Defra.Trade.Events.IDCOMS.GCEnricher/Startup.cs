// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Diagnostics.CodeAnalysis;
using Defra.Trade.Common.AppConfig;
using Defra.Trade.Common.Function.Health.HealthChecks;
using Defra.Trade.Common.Infra.Infrastructure;
using Defra.Trade.Common.Security.Authentication.Infrastructure;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models.Settings;
using Defra.Trade.Events.IDCOMS.GCEnricher.Infrastructure;
using FunctionHealthCheck;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Defra.Trade.Events.IDCOMS.GCEnricher.Startup))]

namespace Defra.Trade.Events.IDCOMS.GCEnricher;

[ExcludeFromCodeCoverage]
public class Startup : FunctionsStartup
{
    public static IConfiguration Configuration { get; private set; }

    private static readonly string _crmApi = "/trade-crm-adapter/v1";
    public override void Configure(IFunctionsHostBuilder builder)
    {
        Configuration = builder.GetContext().Configuration;

        builder.Services
            .AddTradeAppConfiguration(Configuration)
            .AddServiceRegistrations(Configuration)
            .AddApimAuthentication(Configuration.GetSection(InternalApimSettings.SectionName));

        var healthChecksBuilder = builder.Services.AddFunctionHealthChecks();
        RegisterHealthChecks(healthChecksBuilder, builder.Services, Configuration);

        builder.ConfigureMapper();
    }

    private static void RegisterHealthChecks(
        IHealthChecksBuilder builder,
        IServiceCollection services,
        IConfiguration configuration)
    {
        builder.AddCheck<AppSettingHealthCheck>("ServiceBus:ConnectionString")
            .AddCheck<AppSettingHealthCheck>("Apim:Internal:BaseUrl");

        var internalApimSettings = services.BuildServiceProvider().GetRequiredService<IOptions<InternalApimSettings>>();
        var serviceBusQueuesSettings = services.BuildServiceProvider().GetRequiredService<IOptions<ServiceBusQueuesSettings>>();
        string idcomsUserId = "f8f6570d-ebb9-e911-a970-000d3a29be4a";
        builder.AddCrmAdapterHealthCheck<InternalApimSettings>(services.BuildServiceProvider(), $"{_crmApi}", idcomsUserId);

        builder.AddTradeInternalApiCheck<InternalApimSettings>(
            services.BuildServiceProvider(),
            $"{internalApimSettings.Value.DaeraInternalCertificateStoreApi}{internalApimSettings.Value.DaeraInternalCertificateStoreApiHealthEndpoint}");

        builder.AddAzureServiceBusCheck(configuration, "ServiceBus:ConnectionString", GcEnricherSettings.DefaultQueueName);
        builder.AddAzureServiceBusCheck(configuration, "ServiceBus:ConnectionString", serviceBusQueuesSettings.Value.QueueNameEhcoRemosNotification);
    }

    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        builder.ConfigurationBuilder
            .ConfigureTradeAppConfiguration(config =>
                {
                    config.UseKeyVaultSecrets = true;
                    config.RefreshKeys.Add($"{GcEnricherSettings.GcEnricherSettingsSettingsName}:{GcEnricherSettings.AppConfigSentinelName}");
                })
           .Build();
    }
}
