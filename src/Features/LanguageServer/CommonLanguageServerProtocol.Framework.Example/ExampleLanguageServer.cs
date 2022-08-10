﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonLanguageServerProtocol.Framework.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace CommonLanguageServerProtocol.Framework.Example;

public class ExampleLanguageServer : AbstractLanguageServer<ExampleRequestContext>
{
    public ExampleLanguageServer(JsonRpc jsonRpc, ILspLogger logger) : base(jsonRpc, logger)
    {
    }

    protected override ILspServices ConstructLspServices()
    {
        var serviceCollection = new ServiceCollection();

        AddHandlers(serviceCollection)
            .AddSingleton<ILspLogger>(_logger)
            .AddSingleton<IRequestContextFactory<ExampleRequestContext>, ExampleRequestContextFactory>()
            .AddSingleton<IInitializeManager<InitializeParams, InitializeResult>, CapabilitiesManager>()
            .AddSingleton((s) => new LifeCycleManager<ExampleRequestContext>(this));

        var lspServices = new ExampleLspServices(serviceCollection);

        return lspServices;
    }

    private static IServiceCollection AddHandlers(IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddSingleton<IMethodHandler, InitializeHandler<InitializeParams, InitializeResult, ExampleRequestContext>>()
            .AddSingleton<IMethodHandler, InitializedHandler<InitializedParams, ExampleRequestContext>>()
            .AddSingleton<IMethodHandler, ShutdownHandler<ExampleRequestContext>>()
            .AddSingleton<IMethodHandler, ExitHandler<ExampleRequestContext>>();

        return serviceCollection;
    }
}
