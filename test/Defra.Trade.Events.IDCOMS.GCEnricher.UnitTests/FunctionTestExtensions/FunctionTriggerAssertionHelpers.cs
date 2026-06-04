// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Shouldly;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.FunctionTestExtensions;

public static class FunctionTriggerAssertionHelpers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used as unit testing mock")]
    public static MethodInfo ShouldHaveServiceBusTrigger<TClass>(
        string methodName,
        string queueName,
        string connection)
        where TClass : class
    {
        var method = GetMethodInfo<TClass>(methodName) ??
            throw new InvalidOperationException($"Method {nameof(TClass)}.{methodName} is missing");

        var parameters = method.GetParameters();

        var triggerParameters = parameters.Where(x => x.GetCustomAttribute<ServiceBusTriggerAttribute>() != null).ToArray();
        triggerParameters.ShouldHaveSingleItem($"{nameof(TClass)}.{methodName}: " +
            "A function cannot have multiple trigger bindings");

        var sbTriggerAttribute = triggerParameters[0].GetCustomAttribute<ServiceBusTriggerAttribute>();
        sbTriggerAttribute.ShouldNotBeNull();
        sbTriggerAttribute.QueueName.ShouldBe(queueName);
        return method;
    }

    public static MethodInfo GetMethodInfo<TClass>(string methodName)
    {
        return typeof(TClass).GetMethod(methodName);
    }
}
