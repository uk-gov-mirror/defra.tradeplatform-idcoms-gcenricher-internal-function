// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Defra.Trade.Common.Functions.Models;
using Defra.Trade.Common.Functions.Models.Enum;
using Defra.Trade.Common.Functions.Validation;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Config;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Models;
using FluentValidation;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.Application.Validators;

public class GcEnrichmentMessageHeaderValidator : AbstractValidator<TradeEventMessageHeader>
{
    public GcEnrichmentMessageHeaderValidator() : base()
    {
        RuleFor(x => x.MessageId)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField);

        RuleFor(x => x.CorrelationId)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField);

        RuleFor(x => x.CausationId)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField);

        RuleFor(x => x.ContentType)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField)
            .Must(c => c.Equals(GcMessageConstants.MessageContentType, StringComparison.OrdinalIgnoreCase)).WithMessage(GcMessageConstants.ContentErrorMessage);

        RuleFor(x => x.EntityKey)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField);

        RuleFor(x => x.PublisherId)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField)
            .Must(pid => pid.Equals(GcEnricherSettings.PublisherId, StringComparison.OrdinalIgnoreCase)).WithMessage(GcMessageConstants.PublisherIdErrorMessage);

        RuleFor(x => x.SchemaVersion)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField)
            .Must(sv => sv.Equals(GcMessageConstants.SchemaVersion)).WithMessage(GcMessageConstants.SchemaVersionMessage);

        RuleFor(x => x.Type)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .IsInEnum().WithMessage(GcMessageConstants.TypeErrorMessage)
            .Must(type => type.Equals(EventType.Internal)).WithMessage(GcMessageConstants.TypeErrorMessage);

        RuleFor(x => x.TimestampUtc)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField);

        RuleFor(x => x.Label)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.NullField);

        When(x => x.Label != null, () =>
        {
            RuleFor(x => x.Label)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.EmptyField);

            RuleFor(x => x.Label)
            .Cascade(CascadeMode.Stop)
            .Must(lbl => lbl.Equals(GcMessageConstants.BrokerLabel, StringComparison.OrdinalIgnoreCase)).WithMessage(GcMessageConstants.LabelErrorMessage);
        });
    }
}
