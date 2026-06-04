// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Defra.Trade.Common.Functions.Isolated.Validation;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Dtos.Inbound;
using FluentValidation;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Validators;

public class GcEnrichmentInboundValidator : AbstractValidator<GcEnrichmentInbound>
{
    public GcEnrichmentInboundValidator()
    {
        RuleFor(x => x.GcId)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField);
    }
}