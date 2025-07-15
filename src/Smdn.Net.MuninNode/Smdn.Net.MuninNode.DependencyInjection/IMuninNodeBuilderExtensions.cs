// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.DependencyInjection;

[Obsolete($"Use {nameof(MuninNodeBuilderExtensions)} instead.")]
public static class IMuninNodeBuilderExtensions {
  private static MuninNodeBuilder ThrowIfBuilderTypeIsNotSupported(IMuninNodeBuilder builder)
  {
    if (builder is not MuninNodeBuilder muninNodeBuilder)
      throw new NotSupportedException($"The builder implementation of type `{builder.GetType().FullName}` does not support service key configuration.");

    return muninNodeBuilder;
  }

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static IMuninNodeBuilder AddPlugin(
    this IMuninNodeBuilder builder,
    IPlugin plugin
  )
#pragma warning restore CS0419
    => MuninNodeBuilderExtensions.AddPlugin(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      plugin: plugin ?? throw new ArgumentNullException(nameof(plugin))
    );

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static IMuninNodeBuilder AddPlugin(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, IPlugin> buildPlugin
  )
#pragma warning restore CS0419
    => MuninNodeBuilderExtensions.AddPlugin(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      buildPlugin: buildPlugin ?? throw new ArgumentNullException(nameof(buildPlugin))
    );

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
    => MuninNodeBuilderExtensions.UsePluginProvider(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      pluginProvider: pluginProvider ?? throw new ArgumentNullException(nameof(pluginProvider))
    );

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
    => MuninNodeBuilderExtensions.UsePluginProvider(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      buildPluginProvider: buildPluginProvider ?? throw new ArgumentNullException(nameof(buildPluginProvider))
    );

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static IMuninNodeBuilder UseSessionCallback(
    this IMuninNodeBuilder builder,
    INodeSessionCallback sessionCallback
  )
#pragma warning restore CS0419
    => MuninNodeBuilderExtensions.UseSessionCallback(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      sessionCallback: sessionCallback ?? throw new ArgumentNullException(nameof(sessionCallback))
    );

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
    => MuninNodeBuilderExtensions.UseSessionCallback(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      reportSessionStartedAsyncFunc: reportSessionStartedAsyncFunc,
      reportSessionClosedAsyncFunc: reportSessionClosedAsyncFunc
    );

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static IMuninNodeBuilder UseSessionCallback(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, INodeSessionCallback> buildSessionCallback
  )
#pragma warning restore CS0419
    => MuninNodeBuilderExtensions.UseSessionCallback(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      buildSessionCallback: buildSessionCallback ?? throw new ArgumentNullException(nameof(buildSessionCallback))
    );

#pragma warning disable CS0419
  /// <remarks>
  /// If <see cref="UsePluginProvider"/> is called, the configurations made by this method will be overridden.
  /// </remarks>
  public static IMuninNodeBuilder UseTransactionCallback(
    this IMuninNodeBuilder builder,
    Func<CancellationToken, ValueTask>? onStartTransactionAsyncFunc,
    Func<CancellationToken, ValueTask>? onEndTransactionAsyncFunc
  )
#pragma warning restore CS0419
    => MuninNodeBuilderExtensions.UseTransactionCallback(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      onStartTransactionAsyncFunc: onStartTransactionAsyncFunc,
      onEndTransactionAsyncFunc: onEndTransactionAsyncFunc
    );

  public static IMuninNodeBuilder UseListenerFactory(
    this IMuninNodeBuilder builder,
    IMuninNodeListenerFactory listenerFactory
  )
    => MuninNodeBuilderExtensions.UseListenerFactory(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      listenerFactory: listenerFactory ?? throw new ArgumentNullException(nameof(listenerFactory))
    );

  public static IMuninNodeBuilder UseListenerFactory(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, EndPoint, IMuninNode, CancellationToken, ValueTask<IMuninNodeListener>> createListenerAsyncFunc
  )
    => MuninNodeBuilderExtensions.UseListenerFactory(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      createListenerAsyncFunc: createListenerAsyncFunc ?? throw new ArgumentNullException(nameof(createListenerAsyncFunc))
    );

  public static IMuninNodeBuilder UseListenerFactory(
    this IMuninNodeBuilder builder,
    Func<IServiceProvider, IMuninNodeListenerFactory> buildListenerFactory
  )
    => MuninNodeBuilderExtensions.UseListenerFactory(
      builder: ThrowIfBuilderTypeIsNotSupported(builder ?? throw new ArgumentNullException(nameof(builder))),
      buildListenerFactory: buildListenerFactory ?? throw new ArgumentNullException(nameof(buildListenerFactory))
    );
}
