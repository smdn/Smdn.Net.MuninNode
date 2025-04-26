// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode.Transport;

namespace Smdn.Net.MuninNode;

/// <summary>
/// Implement a <c>Munin-Node</c> that acts as a node on the localhost and only accepts connections from the local loopback address (127.0.0.1, ::1).
/// </summary>
public abstract partial class LocalNode : NodeBase {
  private const string DefaultHostName = "munin-node.localhost";

  /// <summary>
  /// Initializes a new instance of the <see cref="LocalNode"/> class.
  /// </summary>
  /// <param name="listenerFactory">
  /// The <see cref="IMuninNodeListenerFactory"/> factory to create an <see cref="IMuninNodeListener"/> to be used in this instance.
  /// If <see langword="null"/>, the default <see cref="IMuninNodeListenerFactory"/> implementation is used.
  /// </param>
  /// <param name="accessRule">
  /// The <see cref="IAccessRule"/> to determine whether to accept or reject a remote host that connects to <see cref="LocalNode"/>.
  /// </param>
  /// <param name="logger">
  /// The <see cref="ILogger"/> to report the situation.
  /// </param>
  protected LocalNode(
    IMuninNodeListenerFactory? listenerFactory,
    IAccessRule? accessRule,
    ILogger? logger
  )
    : base(
      listenerFactory: listenerFactory ?? ListenerFactory.Instance,
      accessRule: accessRule,
      logger: logger
    )
  {
  }

  private sealed class ListenerFactory : IMuninNodeListenerFactory {
    public static readonly ListenerFactory Instance = new();

    public ValueTask<IMuninNodeListener> CreateAsync(
      EndPoint endPoint,
      IMuninNode node,
      CancellationToken cancellationToken
    )
#pragma warning disable CA2000
      => new(
        new MuninNodeListener(
          endPoint: endPoint,
          logger: null,
          serviceProvider: null
        )
      );
#pragma warning restore CA2000
  }
}
