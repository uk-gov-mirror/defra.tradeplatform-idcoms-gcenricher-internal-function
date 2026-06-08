// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Defra.Trade.Common.AppConfig;
using Defra.Trade.Common.Config;
using Defra.Trade.Common.Dynamics.ApiClient;
using Defra.Trade.Common.Dynamics.ApiClient.Infra;
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

builder.Services.AddHttpClient();
builder.Services.Configure<ApimInternalSettings>(builder.Configuration.GetSection(ApimInternalSettings.OptionsName));
builder.Services.Configure<DynamicsClientConfig>(builder.Configuration.GetSection(DynamicsClientConfig.SectionName));
builder.Services.AddTransient<IDynamicsClientAuthenticator, DynamicsClientAuthenticator>();

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

var healthChecksBuilder = builder.Services.AddHealthChecks();
RegisterHealthChecks(healthChecksBuilder, builder.Services);

await builder.Build().RunAsync();

static void RegisterHealthChecks(
    IHealthChecksBuilder builder,
    IServiceCollection services)
{
    builder.AddCheck<AppSettingHealthCheck>("ServiceBus:ConnectionString")
        .AddCheck<AppSettingHealthCheck>("Apim:Internal:BaseUrl");

    var sp = services.BuildServiceProvider();
    var internalApimSettings = sp.GetRequiredService<IOptions<InternalApimSettings>>();
    var serviceBusQueuesSettings = sp.GetRequiredService<IOptions<ServiceBusQueuesSettings>>();

    builder.AddDynamicsCheck(sp);

    var apim = internalApimSettings.Value;
    var healthEndpoint = apim.DaeraInternalCertificateStoreApiHealthEndpoint ?? string.Empty;
    // The package's AddTradeApiHealthCheck appends "/health" itself, so strip it from the configured endpoint.
    var trimmedEndpoint = healthEndpoint.EndsWith("/health", StringComparison.OrdinalIgnoreCase)
        ? healthEndpoint[..^"/health".Length]
        : healthEndpoint;
    var certificateStoreApiPath = $"{apim.BaseUrl}{apim.DaeraInternalCertificateStoreApi}{trimmedEndpoint}";

    builder.AddTradeApiHealthCheck(certificateStoreApiPath, "CertificateStoreApi");

    builder.AddAzureServiceBusQueueCheck(serviceBusQueuesSettings.Value, GcEnricherSettings.DefaultQueueName);
    builder.AddAzureServiceBusQueueCheck(serviceBusQueuesSettings.Value, serviceBusQueuesSettings.Value.QueueNameEhcoRemosNotification);
}
