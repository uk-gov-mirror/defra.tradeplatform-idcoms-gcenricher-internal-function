// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Infrastructure;

[ExcludeFromCodeCoverage]
public static class ConfigureMapperExtensions
{
    public static IServiceCollection ConfigureMapper(this IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName is { } fullName && fullName.Contains("Defra"))
            .OrderBy(a => a.FullName)
            .ToArray();
        services.AddAutoMapper(cfg => cfg.AddMaps(assemblies));
        return services;
    }
}
