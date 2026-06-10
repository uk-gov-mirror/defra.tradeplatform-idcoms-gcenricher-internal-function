// Copyright DEFRA (c). All rights reserved.
// Licensed under the Open Government License v3.0.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Shouldly;

namespace Defra.Trade.Events.IDCOMS.GCEnricher.UnitTests.Helpers;

public static class FunctionTestHelpers
{
    public static async Task<string> StreamToStringAsync(this Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        string text = await reader.ReadToEndAsync();
        return text;
    }

    public static TAttribute MethodHasSingleAttribute<TClass, TAttribute>(string methodName)
        where TClass : class
        where TAttribute : Attribute
    {
        object[] attributes = GetMethodAttributes<TClass, TAttribute>(methodName);

        attributes.ShouldNotBeNull();
        attributes.ShouldHaveSingleItem();

        var attribute = attributes![0] as TAttribute;
        attribute.ShouldNotBeNull();

        return attribute!;
    }

    public static object[] GetMethodAttributes<TClass, TAttribute>(string methodName)
        where TClass : class
        where TAttribute : Attribute
    {
        var methodInfo = typeof(TClass)
            .GetMethod(methodName);

        object[] attribute = methodInfo!
            .GetCustomAttributes(typeof(TAttribute), false);

        return attribute;
    }

    public static void Function_HasHttpTriggerAttributeWithCorrectValues<TClass>(
        string methodName,
        string expectedRoute,
        string[] expectedMethods,
        AuthorizationLevel expectedAuthLevel = AuthorizationLevel.Function)
        where TClass : class
    {
        var methodInfo = GetMethodInfo<TClass>(methodName)
            ?? throw new InvalidOperationException($"Could not find method {methodName} on class {typeof(TClass).Name}");

        var httpRequestDataParam = methodInfo.GetParameters()
            .Single(x => x.ParameterType == typeof(HttpRequest));

        var httpTriggerAttribute = httpRequestDataParam
            .GetCustomAttributes(typeof(HttpTriggerAttribute), false)
            .Select(x => x as HttpTriggerAttribute)
            .Single()!;

        httpTriggerAttribute.AuthLevel.ShouldBe(expectedAuthLevel);
        httpTriggerAttribute.Route.ShouldBe(expectedRoute);
        httpTriggerAttribute.Methods.ShouldNotBeNull();
        httpTriggerAttribute.Methods!.Length.ShouldBe(expectedMethods.Length);

        var commonEl = expectedMethods.Intersect(httpTriggerAttribute.Methods, StringComparer.InvariantCultureIgnoreCase);
        commonEl.Count().ShouldBe(expectedMethods.Length);
    }

    public static Mock<FunctionContext> CreateFunctionContext()
    {
        return new Mock<FunctionContext>();
    }

    public static Mock<HttpRequestData> CreateHttpRequestData(FunctionContext functionContext = null)
    {
        functionContext ??= new Mock<FunctionContext>().Object;
        return new Mock<HttpRequestData>(functionContext);
    }

    public static Mock<HttpResponseData> CreateHttpResponseData(FunctionContext functionContext = null)
    {
        functionContext ??= new Mock<FunctionContext>().Object;
        return new Mock<HttpResponseData>(functionContext);
    }

    private static MethodInfo GetMethodInfo<TClass>(string methodName)
    {
        return typeof(TClass).GetMethod(methodName)!;
    }
}