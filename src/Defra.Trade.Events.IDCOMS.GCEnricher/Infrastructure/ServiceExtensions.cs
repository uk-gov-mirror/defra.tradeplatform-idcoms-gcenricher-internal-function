// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Defra.Trade.API.CertificatesStore.V1.ApiClient.Api;
using Defra.Trade.API.CertificatesStore.V1.ApiClient.Client;
using Defra.Trade.Common.Config;
using Defra.Trade.Common.Functions;
using Defra.Trade.Common.Functions.EventStore;
using Defra.Trade.Common.Functions.Interfaces;
using Defra.Trade.Common.Functions.Models;
using Defra.Trade.Common.Functions.Services;
using Defra.Trade.Common.Functions.Validation;
using Defra.Trade.Common.Infra.Infrastructure;
using Defra.Trade.Common.Security.Authentication;
using Defra.Trade.Common.Security.Authentication.Interfaces;
using Defra.Trade.CrmAdapter.Api.V1.ApiClient.Api;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models.Settings;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Services.Contracts;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Validators;
using Defra.Trade.Events.IDCOMS.GCEnricher.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Infrastructure;

[ExcludeFromCodeCoverage]
public static class ServiceExtensions
{
    private static readonly string _crmApi = "/trade-crm-adapter/v1";

    public static IServiceCollection AddServiceRegistrations(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddSingleton<ICustomValidatorFactory, CustomValidatorFactory>();
        services.AddSingleton<AbstractValidator<TradeEventMessageHeader>, GcEnrichmentMessageHeaderValidator>();
        services.AddSingleton<AbstractValidator<GcEnrichmentInbound>, GcEnrichmentInboundValidator>();

        services.AddSingleton<IAuthenticationTokenProvider, ManagedIdentityAuthenticationTokenProvider>();

        services.AddSingleton<MessageRetryService>();
        services.AddSingleton<IMessageRetryService>(p => p.GetRequiredService<MessageRetryService>());
        services.AddSingleton<IMessageRetryContextAccessor>(p => p.GetRequiredService<MessageRetryService>());

        services.AddEventStoreConfiguration();

        services.AddTransient<IMessageProcessor<GcEnrichmentRequest, TradeEventMessageHeader>, SbMessageProcessor>();
        services.AddTransient<IInboundMessageValidator<GcEnrichmentInbound, TradeEventMessageHeader>,
            InboundMessageValidator<GcEnrichmentInbound, GcEnrichmentRequest, TradeEventMessageHeader>>();
        services.AddTransient<IBaseMessageProcessorService<GcEnrichmentInbound>,
            BaseMessageProcessorService<GcEnrichmentInbound, GcEnrichmentRequest, GcEnrichmentRequest, TradeEventMessageHeader>>();

        services.AddSingleton<IMessageCollector, EventStoreCollector>();
        services.AddTransient<IGcEnrichmentMessageProcessor, GcEnrichmentMessageProcessor>();

        services.AddOptions<ServiceBusQueuesSettings>().Bind(configuration.GetSection(ServiceBusSettings.OptionsName));
        services.AddScoped<IServiceBusManagerClient, ServiceBusManagerClient>();
        services.AddSingleton<IQueueClientFactory, QueueClientFactory>();

        var gcConfig = configuration.GetSection(GcEnricherSettings.GcEnricherSettingsSettingsName);
        services.AddOptions<GcEnricherSettings>().Bind(gcConfig);

        var appConfig = configuration.GetSection(InternalApimSettings.SectionName);
        services.AddOptions<InternalApimSettings>().Bind(appConfig);

        services.Configure<ServiceBusSettings>(configuration.GetSection(ServiceBusSettings.OptionsName));

        services.AddClientApiServices();

        return services;
    }

    private static void AddClientApiServices(this IServiceCollection services)
    {
        services
            .ConfigureCrmAdapterApi()
            .ConfigureGCStoreApi();
    }

    private static IServiceCollection ConfigureCrmAdapterApi(this IServiceCollection services)
    {
        services
            .AddScoped((provider) =>
            {
                var authService = provider.GetService<IAuthenticationService>();
                var apimSettings = provider.GetService<IOptions<InternalApimSettings>>()!.Value;
                string authToken = authService.GetAuthenticationHeaderAsync().Result.ToString();
                var config = new CrmAdapter.Api.V1.ApiClient.Client.Configuration
                {
                    BasePath = $"{apimSettings.BaseUrl}{_crmApi}",
                    DefaultHeaders = new Dictionary<string, string>
                    {
                        {"Authorization", authToken},
                        {apimSettings.SubscriptionKeyHeaderName, apimSettings.SubscriptionKey}
                    }
                };
                return config;
            })
            .AddTransient<IEnrichmentApi>(provider => new EnrichmentApi(provider.GetService<CrmAdapter.Api.V1.ApiClient.Client.Configuration>()));
        return services;
    }

    private static IServiceCollection ConfigureGCStoreApi(this IServiceCollection services)
    {
        services.AddScoped(
        (provider) =>
        {
            var authService = provider.GetService<IAuthenticationService>();
            var apimSettings = provider.GetService<IOptions<InternalApimSettings>>()!.Value;
            string authToken = authService.GetAuthenticationHeaderAsync().Result.ToString();
            var config = new Configuration
            {
                BasePath =
                 $"{apimSettings.BaseUrl}{apimSettings.DaeraInternalCertificateStoreApi}",
                DefaultHeaders = new Dictionary<string, string>
                {
                    {"Authorization", authToken},
                    {
                        apimSettings.SubscriptionKeyHeaderName,
                        apimSettings.SubscriptionKey
                    }
                }
            };
            return config;
        }).AddTransient<IEhcoGeneralCertificateApplicationApi>(
                provider => new EhcoGeneralCertificateApplicationApi(provider.GetService<Configuration>()))
            .AddTransient<IHealthApi>(provider => new HealthApi(provider.GetService<Configuration>()))
            .AddTransient<IIdcomsGeneralCertificateEnrichmentApi>(
                provider => new IdcomsGeneralCertificateEnrichmentApi(provider.GetService<Configuration>()));


        return services;
    }
}
