// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.DependencyInjection;

public static class MuninNodeBuilderExtensions {
#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static TMuninNodeBuilder AddPlugin<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    IPlugin plugin
  )
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (plugin is null)
      throw new ArgumentNullException(nameof(plugin));

    return AddPlugin(
      builder: builder,
      buildPlugin: _ => plugin
    );
  }

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static TMuninNodeBuilder AddPlugin<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    Func<IServiceProvider, IPlugin> buildPlugin
  )
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (buildPlugin is null)
      throw new ArgumentNullException(nameof(buildPlugin));

    builder.AddPluginFactory(buildPlugin);

    return builder;
  }

#pragma warning disable CS0419
  /// <remarks>
  /// Calling this method will override the configurations made by
  /// <see cref="AddPlugin"/> and <see cref="UseSessionCallback"/>.
  /// </remarks>
  public static TMuninNodeBuilder UsePluginProvider<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    IPluginProvider pluginProvider
  )
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (pluginProvider is null)
      throw new ArgumentNullException(nameof(pluginProvider));

    return UsePluginProvider(
      builder: builder,
      buildPluginProvider: _ => pluginProvider
    );
  }

#pragma warning disable CS0419
  /// <remarks>
  /// Calling this method will override the configurations made by
  /// <see cref="AddPlugin"/> and <see cref="UseSessionCallback"/>.
  /// </remarks>
  public static TMuninNodeBuilder UsePluginProvider<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    Func<IServiceProvider, IPluginProvider> buildPluginProvider
  )
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (buildPluginProvider is null)
      throw new ArgumentNullException(nameof(buildPluginProvider));

    builder.SetPluginProviderFactory(buildPluginProvider);

    return builder;
  }

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static TMuninNodeBuilder UseSessionCallback<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    INodeSessionCallback sessionCallback
  )
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (sessionCallback is null)
      throw new ArgumentNullException(nameof(sessionCallback));

    return UseSessionCallback(
      builder: builder,
      buildSessionCallback: _ => sessionCallback
    );
  }

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static TMuninNodeBuilder UseSessionCallback<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    Func<string, CancellationToken, ValueTask>? reportSessionStartedAsyncFunc,
    Func<string, CancellationToken, ValueTask>? reportSessionClosedAsyncFunc
  )
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore CS0419
    => UseSessionCallback(
      builder: builder,
      buildSessionCallback: _ => new SessionCallbackFuncWrapper(
        reportSessionStartedAsyncFunc,
        reportSessionClosedAsyncFunc
      )
    );

  private sealed class SessionCallbackFuncWrapper(
    Func<string, CancellationToken, ValueTask>? reportSessionStartedAsyncFunc,
    Func<string, CancellationToken, ValueTask>? reportSessionClosedAsyncFunc
  ) : INodeSessionCallback {
    public ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
      => reportSessionStartedAsyncFunc is null
        ? default
        : reportSessionStartedAsyncFunc(sessionId, cancellationToken);

    public ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
      => reportSessionClosedAsyncFunc is null
        ? default
        : reportSessionClosedAsyncFunc(sessionId, cancellationToken);
  }

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static TMuninNodeBuilder UseSessionCallback<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    Func<IServiceProvider, INodeSessionCallback> buildSessionCallback
  )
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (buildSessionCallback is null)
      throw new ArgumentNullException(nameof(buildSessionCallback));

    builder.SetSessionCallbackFactory(buildSessionCallback);

    return builder;
  }

  public static TMuninNodeBuilder UseListenerFactory<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    IMuninNodeListenerFactory listenerFactory
  )
    where TMuninNodeBuilder : MuninNodeBuilder
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (listenerFactory is null)
      throw new ArgumentNullException(nameof(listenerFactory));

    return UseListenerFactory(
      builder: builder,
      buildListenerFactory: _ => listenerFactory
    );
  }

  public static TMuninNodeBuilder UseListenerFactory<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    Func<IServiceProvider, EndPoint, IMuninNode, CancellationToken, ValueTask<IMuninNodeListener>> createListenerAsyncFunc
  )
    where TMuninNodeBuilder : MuninNodeBuilder
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (createListenerAsyncFunc is null)
      throw new ArgumentNullException(nameof(createListenerAsyncFunc));

    return UseListenerFactory(
      builder: builder,
      buildListenerFactory: serviceProvider => new CreateListenerAsyncFuncWrapper(
        serviceProvider,
        createListenerAsyncFunc
      )
    );
  }

  private sealed class CreateListenerAsyncFuncWrapper(
    IServiceProvider serviceProvider,
    Func<IServiceProvider, EndPoint, IMuninNode, CancellationToken, ValueTask<IMuninNodeListener>> createListenerAsyncFunc
  ) : IMuninNodeListenerFactory {
    public ValueTask<IMuninNodeListener> CreateAsync(EndPoint endPoint, IMuninNode node, CancellationToken cancellationToken)
      => createListenerAsyncFunc(serviceProvider, endPoint, node, cancellationToken);
  }

  public static TMuninNodeBuilder UseListenerFactory<TMuninNodeBuilder>(
    this TMuninNodeBuilder builder,
    Func<IServiceProvider, IMuninNodeListenerFactory> buildListenerFactory
  )
    where TMuninNodeBuilder : MuninNodeBuilder
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (buildListenerFactory is null)
      throw new ArgumentNullException(nameof(buildListenerFactory));

    builder.SetListenerFactory(buildListenerFactory);

    return builder;
  }

  public static TMuninNode Build<TMuninNode>(
    this MuninNodeBuilder builder,
    IServiceProvider serviceProvider
  ) where TMuninNode : IMuninNode
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (serviceProvider is null)
      throw new ArgumentNullException(nameof(serviceProvider));

    var n = builder.Build(serviceProvider);

    if (n is not TMuninNode node)
      throw new InvalidOperationException($"The type '{n.GetType()}' of the constructed instance did not match the requested type '{typeof(TMuninNode)}'.");

    return node;
  }
}
