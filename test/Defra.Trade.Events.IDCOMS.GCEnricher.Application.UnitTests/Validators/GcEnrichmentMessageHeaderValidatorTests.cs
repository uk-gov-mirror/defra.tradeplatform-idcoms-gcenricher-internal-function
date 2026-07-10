// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using Defra.Trade.Common.Functions.Isolated.Models;
using Defra.Trade.Common.Functions.Isolated.Models.Enum;
using Defra.Trade.Events.IDCOMS.GCEnricher.Application.Validators;

namespace Defra.Trade.Events.IDCOMS.GCE.Application.UnitTests.Validators;

public class GcEnrichmentMessageHeaderValidatorTests
{
    private readonly GcEnrichmentMessageHeaderValidator validator;
    private readonly string validGuid;
    private readonly string validContentType;
    private readonly string invalidContentType;
    private readonly string validLabel;
    private readonly string invalidLabel;

    public GcEnrichmentMessageHeaderValidatorTests()
    {
        validator = new GcEnrichmentMessageHeaderValidator();

        validGuid = Guid.NewGuid().ToString();
        validContentType = "application/json";
        invalidContentType = "invalid/type";
        validLabel = "trade.remos.enrichment";
        invalidLabel = "trade.invalid.label";
    }

    [Fact]
    public void Should_Validate_MessageId_Is_Null_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "MessageId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            MessageId = null
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_MessageId_Is_Empty_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "MessageId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            MessageId = string.Empty
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_MessageId_Is_A_Guid_Returns_No_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "MessageId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            MessageId = validGuid
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(0);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeFalse();
    }

    [Fact]
    public void Should_Validate_CausationId_Is_Null_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "CausationId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            CausationId = null
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_CausationId_Is_Empty_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "CausationId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            CausationId = string.Empty
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_CausationId_Is_A_Guid_Returns_No_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "CausationId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            CausationId = validGuid
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(0);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeFalse();
    }

    [Fact]
    public void Should_Validate_CorrelationId_Is_Null_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "CorrelationId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            CorrelationId = null
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_CorrelationId_Is_Empty_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "CorrelationId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            CorrelationId = string.Empty
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_CorrelationId_Is_A_Guid_Returns_No_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "CorrelationId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            CorrelationId = validGuid
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(0);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeFalse();
    }

    [Fact]
    public void Should_Validate_UserId_Is_A_Guid_Returns_No_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "UserId";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            UserId = validGuid
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(0);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeFalse();
    }

    [Fact]
    public void Should_Validate_ContentType_Is_Invalid_Type_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "ContentType";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            ContentType = invalidContentType
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_ContentType_Is_Valid_Type_Returns_No_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "ContentType";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            ContentType = validContentType
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(0);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeFalse();
    }

    [Fact]
    public void Should_Validate_Label_Is_Null_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "Label";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            Label = null
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_Label_Is_Empty_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "Label";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            Label = string.Empty
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(2);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_Label_Is_Invalid_Returns_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "Label";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            Label = invalidLabel
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(1);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeTrue();
    }

    [Fact]
    public void Should_Validate_Label_Is_Valid_Returns_No_ErrorMessage()
    {
        // Arrange
        string propertyNameToValidate = "Label";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            Label = validLabel
        });

        // Assert
        result.Errors.Count(x => x.PropertyName == propertyNameToValidate).ShouldBe(0);
        result.Errors.Any(x => x.PropertyName == propertyNameToValidate).ShouldBeFalse();
    }

    [Fact]
    public void ValidHeader_ShouldNotHaveAnyErrors()
    {
        // Arrange
        string contentType = "application/json";
        string messageId = Guid.NewGuid().ToString();
        string correlationId = Guid.NewGuid().ToString();
        string causationId = Guid.NewGuid().ToString();
        string entityKey = "123456";
        string publisherId = "TradeApi";
        var type = EventType.Internal;
        int timestamp = 20231212;
        string label = "trade.remos.enrichment";
        string schemaVersion = "1";

        // Act
        var result = validator.Validate(new TradeEventMessageHeader
        {
            MessageId = messageId,
            ContentType = contentType,
            TimestampUtc = timestamp,
            SchemaVersion = schemaVersion,
            CorrelationId = correlationId,
            CausationId = causationId,
            EntityKey = entityKey,
            Label = label,
            PublisherId = publisherId,
            Type = type,
        });

        // Assert
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void ValidHeader_ShouldHaveAnyErrors()
    {
        // Act
        var result = validator.Validate(new TradeEventMessageHeader());

        // Assert
        result.Errors.Count.ShouldBe(10);
        result.Errors.Any(x => x.PropertyName == "MessageId").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "ContentType").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "TimestampUtc").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "SchemaVersion").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "CorrelationId").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "CausationId").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "EntityKey").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "Label").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "PublisherId").ShouldBeTrue();
        result.Errors.Any(x => x.PropertyName == "Type").ShouldBeTrue();
    }
}