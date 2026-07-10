// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Defra.Trade.Common.Function.Health;
using Defra.Trade.Events.IDCOMS.GCEnricher.Functions;
using Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.Functions;

public class HealthCheckFunctionTests
{
    private readonly Mock<HealthCheckService> _healthCheckService;
    private readonly HealthCheckFunction _sut;

    public HealthCheckFunctionTests()
    {
        _healthCheckService = new Mock<HealthCheckService>();
        _sut = new HealthCheckFunction(_healthCheckService.Object);
    }

    [Fact]
    public void RunAsync_HasFunctionAttribute()
    {
        var attribute = FunctionTestHelpers.MethodHasSingleAttribute<HealthCheckFunction, FunctionAttribute>(
            nameof(HealthCheckFunction.RunAsync));

        attribute.Name.ShouldBe("HealthCheckFunction");
    }

    [Fact]
    public void RunAsync_HasHttpTriggerAttributeWithCorrectValues()
    {
        FunctionTestHelpers.Function_HasHttpTriggerAttributeWithCorrectValues<HealthCheckFunction>(
            nameof(HealthCheckFunction.RunAsync),
            "health",
            ["GET"],
            AuthorizationLevel.Anonymous);
    }

    [Fact]
    public async Task RunAsync_ValidHealthCheck_ReturnsOkResponse()
    {
        var req = CreateHttpRequest();
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>(), HealthStatus.Healthy, TimeSpan.FromSeconds(1));
        _healthCheckService.Setup(s => s.CheckHealthAsync(null, CancellationToken.None)).ReturnsAsync(healthReport);

        var result = await _sut.RunAsync(req);

        result.ShouldNotBeNull();
        var bodyText = result as JsonResult;
        bodyText.ShouldNotBeNull();
        bodyText.Value.ShouldBe("Healthy");
    }

    [Fact]
    public async Task RunAsync_InvalidHealthCheck_ReturnsInternalServerErrorResponse()
    {
        var req = CreateHttpRequest();
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>(), HealthStatus.Unhealthy, TimeSpan.FromSeconds(1));
        _healthCheckService.Setup(s => s.CheckHealthAsync(null, CancellationToken.None)).ReturnsAsync(healthReport);

        var result = await _sut.RunAsync(req);

        result.ShouldNotBeNull();
        var bodyText = result as JsonResult;
        bodyText.ShouldNotBeNull();
        bodyText.Value.ShouldNotBeNull();
        var errors = bodyText.Value as HealthCheckResponse;
        errors.ShouldNotBeNull();
        errors.Status.ShouldBe("Unhealthy");
    }

    private static HttpRequest CreateHttpRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
        return context.Request;
    }
}
