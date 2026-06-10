// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Collections.Generic;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Mappers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.UnitTests.Mappers;

public class EnrichedGcProfileTests
{
    protected IMapper Mapper { get; }

    public EnrichedGcProfileTests()
    {
        var profiles =
            new List<Profile>
            {
                new GcEnrichedProfile(),
                new ContactInfoProfile(),
                new EnrichedApplicantProfile(),
                new EnrichedContactProfile(),
                new EnrichedTraderProfile(),
                new AddressProfile(),
                new EnrichedEstablishmentProfile(),
                new EnrichedCountryProfile(),
                new GcEnrichedDtoProfile(),
                new GcEnrichedDataProfile()
            };
        var config = new MapperConfiguration(cfg => cfg.AddProfiles(profiles), NullLoggerFactory.Instance);
        Mapper = config.CreateMapper();
    }
}