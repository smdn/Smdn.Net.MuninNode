// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.DependencyInjection;

public static class IMuninNodeBuilderExtensions {
#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static IMuninNodeBuilder AddPlugin(
    this IMuninNodeBuilder builder,
    IPlugin plugin
  )
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
  public static IMuninNodeBuilder AddPlugin(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, IPlugin> buildPlugin
  )
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (buildPlugin is null)
      throw new ArgumentNullException(nameof(buildPlugin));

    if (builder is not MuninNodeBuilder muninNodeBuilder)
      throw new NotSupportedException($"The builder implementation of type `{builder.GetType().FullName}` does not support service key configuration.");

    muninNodeBuilder.AddPluginFactory(buildPlugin);

    return builder;
  }

#pragma warning disable CS0419
  /// <remarks>
  /// Calling this method will override the configurations made by
  /// <see cref="AddPlugin"/> and <see cref="UseSessionCallback"/>.
  /// </remarks>
  public static IMuninNodeBuilder UsePluginProvider(
    this IMuninNodeBuilder builder,
    IPluginProvider pluginProvider
  )
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
  public static IMuninNodeBuilder UsePluginProvider(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, IPluginProvider> buildPluginProvider
  )
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (buildPluginProvider is null)
      throw new ArgumentNullException(nameof(buildPluginProvider));

    if (builder is not MuninNodeBuilder muninNodeBuilder)
      throw new NotSupportedException($"The builder implementation of type `{builder.GetType().FullName}` does not support service key configuration.");

    muninNodeBuilder.SetPluginProviderFactory(buildPluginProvider);

    return builder;
  }

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static IMuninNodeBuilder UseSessionCallback(
    this IMuninNodeBuilder builder,
    INodeSessionCallback sessionCallback
  )
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
  public static IMuninNodeBuilder UseSessionCallback(
    this IMuninNodeBuilder builder,
    Func<string, CancellationToken, ValueTask>? reportSessionStartedAsyncFunc,
    Func<string, CancellationToken, ValueTask>? reportSessionClosedAsyncFunc
  )
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
  public static IMuninNodeBuilder UseSessionCallback(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, INodeSessionCallback> buildSessionCallback
  )
#pragma warning restore CS0419
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (buildSessionCallback is null)
      throw new ArgumentNullException(nameof(buildSessionCallback));

    if (builder is not MuninNodeBuilder muninNodeBuilder)
      throw new NotSupportedException($"The builder implementation of type `{builder.GetType().FullName}` does not support service key configuration.");

    muninNodeBuilder.SetSessionCallbackFactory(buildSessionCallback);

    return builder;
  }

  public static IMuninNodeBuilder UseListenerFactory(
    this IMuninNodeBuilder builder,
    IMuninNodeListenerFactory listenerFactory
  )
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

  public static IMuninNodeBuilder UseListenerFactory(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, EndPoint, IMuninNode, CancellationToken, ValueTask<IMuninNodeListener>> createListenerAsyncFunc
  )
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

  public static IMuninNodeBuilder UseListenerFactory(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, IMuninNodeListenerFactory> buildListenerFactory
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (buildListenerFactory is null)
      throw new ArgumentNullException(nameof(buildListenerFactory));

    if (builder is not MuninNodeBuilder muninNodeBuilder)
      throw new NotSupportedException($"The builder implementation of type `{builder.GetType().FullName}` does not support service key configuration.");

    muninNodeBuilder.SetListenerFactory(buildListenerFactory);

    return builder;
  }

  internal static TMuninNode Build<TMuninNode>(
    this IMuninNodeBuilder builder,
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
