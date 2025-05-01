// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode.AccessRules;
using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.DependencyInjection;

/// <summary>
/// Implement a <c>Munin-Node</c> that can be configured by IServiceProvider or other interfaces.
/// </summary>
internal sealed class DefaultMuninNode : NodeBase, IMuninNode {
  private readonly MuninNodeOptions options;

  public override string HostName => options.HostName;
  public override IPluginProvider PluginProvider { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="DefaultMuninNode"/> class.
  /// </summary>
  /// <param name="options">
  /// The <see cref="MuninNodeOptions"/> to configure the <c>Munin-Node</c>.
  /// </param>
  /// <param name="pluginProvider">
  /// The <see cref="IPluginProvider"/> to provide <c>Munin plugins</c> for this instance.
  /// </param>
  /// <param name="listenerFactory">
  /// The <see cref="IMuninNodeListenerFactory"/> to create a listener for this instance.
  /// If <see langword="null"/>, the default factory will be used.
  /// </param>
  /// <param name="logger">
  /// The <see cref="ILogger"/> to report the situation.
  /// </param>
  /// <remarks>
  /// If <see cref="MuninNodeOptions.AccessRule"/> is <see langword="null"/>, only allow access from the loopback address.
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="options"/> is <see langword="null"/>, or
  /// <paramref name="pluginProvider"/> is <see langword="null"/>.
  /// </exception>
  public DefaultMuninNode(
    MuninNodeOptions options,
    IPluginProvider pluginProvider,
    IMuninNodeListenerFactory? listenerFactory,
    ILogger? logger
  )
    : base(
      listenerFactory: listenerFactory ?? MuninNodeListenerFactory.Instance,
      accessRule: (options ?? throw new ArgumentNullException(nameof(options))).AccessRule ?? LoopbackOnlyAccessRule.Instance,
      logger: logger
    )
  {
    this.options = options;
    PluginProvider = pluginProvider ?? throw new ArgumentNullException(nameof(pluginProvider));
  }

  protected override EndPoint GetLocalEndPointToBind()
    => new IPEndPoint(options.Address, options.Port);
}
