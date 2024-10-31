// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class LocalNode {
#pragma warning restore IDE0040
  private class ReadOnlyCollectionPluginProvider : IPluginProvider {
    public IReadOnlyCollection<IPlugin> Plugins { get; }
    public INodeSessionCallback? SessionCallback => null;

    public ReadOnlyCollectionPluginProvider(IReadOnlyCollection<IPlugin> plugins)
    {
      Plugins = plugins;
    }
  }

  private sealed class ConcreteLocalNode : LocalNode {
    public override IPluginProvider PluginProvider { get; }
    public override string HostName { get; }

    private readonly int port;

    public ConcreteLocalNode(
      IPluginProvider pluginProvider,
      string hostName,
      int port,
      IServiceProvider? serviceProvider = null
    )
      : base(
        accessRule: serviceProvider?.GetService<IAccessRule>(),
        logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<LocalNode>()
      )
    {
      PluginProvider = pluginProvider;
      HostName = hostName;
      this.port = port;
    }

    protected override EndPoint GetLocalEndPointToBind()
      => new IPEndPoint(
        address: ((IPEndPoint)base.GetLocalEndPointToBind()).Address,
        port: port
      );
  }

  /// <summary>
  /// Creates a new instance of the <see cref="LocalNode"/> class.
  /// </summary>
  /// <param name="plugins">
  /// The readolny collection of <see cref="IPlugin"/>s provided by this node.
  /// </param>
  /// <param name="port">
  /// The port number on which this node accepts connections.
  /// </param>
  /// <param name="hostName">
  /// The hostname advertised by this node. This value is used as the display name for HTML generated by Munin.
  /// If <see langword="null"/> or empty, the default hostname is used.
  /// </param>
  /// <param name="serviceProvider">
  /// The <see cref="IServiceProvider"/>.
  /// This constructor overload attempts to get a service of <see cref="ILoggerFactory"/>, to create an <see cref="ILogger"/>.
  /// </param>
  /// <remarks>
  /// Most Munin-Node uses port 4949 by default, but it is recommended to use other port numbers to avoid conflicts with other nodes.
  /// </remarks>
  public static LocalNode Create(
    IReadOnlyCollection<IPlugin> plugins,
    int port,
    string? hostName = null,
    IServiceProvider? serviceProvider = null
  )
    => Create(
      pluginProvider: new ReadOnlyCollectionPluginProvider(plugins ?? throw new ArgumentNullException(nameof(plugins))),
      hostName: string.IsNullOrEmpty(hostName) ? DefaultHostName : hostName,
      port: port,
      serviceProvider: serviceProvider
    );

  /// <summary>
  /// Creates a new instance of the <see cref="LocalNode"/> class.
  /// </summary>
  /// <param name="pluginProvider">
  /// The <see cref="IPluginProvider"/> that provides <see cref="IPlugin"/>s for this node.
  /// </param>
  /// <param name="port">
  /// The port number on which this node accepts connections.
  /// </param>
  /// <param name="hostName">
  /// The hostname advertised by this node. This value is used as the display name for HTML generated by Munin.
  /// If <see langword="null"/> or empty, the default hostname is used.
  /// </param>
  /// <param name="serviceProvider">
  /// The <see cref="IServiceProvider"/>.
  /// This constructor overload attempts to get a service of <see cref="ILoggerFactory"/>, to create an <see cref="ILogger"/>.
  /// </param>
  /// <remarks>
  /// Most Munin-Node uses port 4949 by default, but it is recommended to use other port numbers to avoid conflicts with other nodes.
  /// </remarks>
  public static LocalNode Create(
    IPluginProvider pluginProvider,
    int port,
    string? hostName = null,
    IServiceProvider? serviceProvider = null
  )
    => new ConcreteLocalNode(
      pluginProvider: pluginProvider ?? throw new ArgumentNullException(nameof(pluginProvider)),
      hostName: string.IsNullOrEmpty(hostName) ? DefaultHostName : hostName,
      port: port,
      serviceProvider: serviceProvider
    );
}
