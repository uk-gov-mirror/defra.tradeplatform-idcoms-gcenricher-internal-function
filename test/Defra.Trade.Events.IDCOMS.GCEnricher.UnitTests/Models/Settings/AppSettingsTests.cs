// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Defra.Trade.Common.Infra.Infrastructure;
using Shouldly;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.Models.Settings;

public class AppSettingsTests
{
    [Fact]
    public void Options_ShouldBe_AsExpected()
    {
        // Act
        var sut = new InternalApimSettings
        {
            Authority = "mocked-host",
            BaseUrl = "version-mocked",
            SubscriptionKey = "version-mocked",
            SubscriptionKeyHeaderName = "version-mocked",
            DaeraInternalCertificateStoreApi = "version-mocked",
        };

        // Assert
        sut.Authority.ShouldBe("mocked-host");
        sut.BaseUrl.ShouldBe("version-mocked");
        sut.SubscriptionKey.ShouldBe("version-mocked");
        sut.SubscriptionKeyHeaderName.ShouldBe("version-mocked");
        sut.DaeraInternalCertificateStoreApi.ShouldBe("version-mocked");
    }
}
