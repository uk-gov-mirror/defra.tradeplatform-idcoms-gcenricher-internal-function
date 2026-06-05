// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Defra.Trade.Common.AppConfig;
using Defra.Trade.Common.Function.Health.Extensions;
using Defra.Trade.Common.Function.Health.HealthChecks;
using Defra.Trade.Common.Infra.Infrastructure;
using Defra.Trade.Common.Security.Authentication.Infrastructure;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models.Settings;
using Defra.Trade.Events.IDCOMS.GCEnricher.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration
    .ConfigureTradeAppConfiguration(config =>
    {
        config.UseKeyVaultSecrets = true;
        config.RefreshKeys.Add($"{GcEnricherSettings.GcEnricherSettingsSettingsName}:{GcEnricherSettings.AppConfigSentinelName}");
    });

builder.Services
    .AddTradeAppConfiguration(builder.Configuration)
    .AddServiceRegistrations(builder.Configuration)
    .AddApimAuthentication(builder.Configuration.GetSection(InternalApimSettings.SectionName))
    .ConfigureMapper();

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

var healthChecksBuilder = builder.Services.AddHealthChecks();
RegisterHealthChecks(healthChecksBuilder, builder.Services, builder.Configuration);

await builder.Build().RunAsync();

static void RegisterHealthChecks(
    IHealthChecksBuilder builder,
    IServiceCollection services,
    IConfiguration configuration)
{
    builder.AddCheck<AppSettingHealthCheck>("ServiceBus:ConnectionString")
        .AddCheck<AppSettingHealthCheck>("Apim:Internal:BaseUrl");

    var sp = services.BuildServiceProvider();
    var internalApimSettings = sp.GetRequiredService<IOptions<InternalApimSettings>>();
    var serviceBusQueuesSettings = sp.GetRequiredService<IOptions<ServiceBusQueuesSettings>>();

    builder.AddDynamicsCheck(sp);

    builder.AddTradeApiHealthCheck(
        internalApimSettings.Value.DaeraInternalCertificateStoreApiHealthEndpoint,
        "CertificateStoreApi");

    builder.AddAzureServiceBusQueueCheck(serviceBusQueuesSettings.Value, GcEnricherSettings.DefaultQueueName);
    builder.AddAzureServiceBusQueueCheck(serviceBusQueuesSettings.Value, serviceBusQueuesSettings.Value.QueueNameEhcoRemosNotification);
}
