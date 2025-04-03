// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode.AccessRules;

namespace Smdn.Net.MuninNode;

/// <summary>
/// Implement a <c>Munin-Node</c> that acts as a node on the localhost and only accepts connections from the local loopback address (127.0.0.1, ::1).
/// </summary>
public abstract partial class LocalNode : NodeBase {
  private const string DefaultHostName = "munin-node.localhost";

  /// <summary>
  /// Initializes a new instance of the <see cref="LocalNode"/> class.
  /// </summary>
  /// <param name="accessRule">
  /// The <see cref="IAccessRule"/> to determine whether to accept or reject a remote host that connects to <see cref="LocalNode"/>.
  /// </param>
  /// <param name="logger">
  /// The <see cref="ILogger"/> to report the situation.
  /// </param>
  protected LocalNode(
    IAccessRule? accessRule,
    ILogger? logger = null
  )
    : base(
      accessRule: accessRule,
      logger: logger
    )
  {
  }

  /// <summary>
  /// Gets the <see cref="EndPoint"/> to be bound as the <c>Munin-Node</c>'s endpoint.
  /// </summary>
  /// <returns>
  /// An <see cref="EndPoint"/>.
  /// The default implementation returns an <see cref="IPEndPoint"/> with the port number <c>0</c>
  /// and <see cref="IPAddress.IPv6Loopback"/>/<see cref="IPAddress.Loopback"/>.
  /// </returns>
  protected virtual EndPoint GetLocalEndPointToBind()
    => new IPEndPoint(
      address:
        Socket.OSSupportsIPv6
          ? IPAddress.IPv6Loopback
          : Socket.OSSupportsIPv4
            ? IPAddress.Loopback
            : throw new NotSupportedException(),
      port: 0
    );

  protected override Socket CreateServerSocket()
  {
    const int MaxClients = 1;

    Socket? server = null;

    try {
      var endPoint = GetLocalEndPointToBind();

      server = new Socket(
        endPoint.AddressFamily,
        SocketType.Stream,
        ProtocolType.Tcp
      );

      if (endPoint.AddressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv4)
        server.DualMode = true;

      server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      server.Bind(endPoint);
      server.Listen(MaxClients);

      return server;
    }
    catch {
      server?.Dispose();
      throw;
    }
  }
}
