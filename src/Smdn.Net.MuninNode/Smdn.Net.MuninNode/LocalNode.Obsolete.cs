// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class LocalNode {
#pragma warning restore IDE0040
  /// <summary>
  /// Initializes a new instance of the <see cref="LocalNode"/> class.
  /// </summary>
  /// <param name="accessRule">
  /// The <see cref="IAccessRule"/> to determine whether to accept or reject a remote host that connects to <see cref="LocalNode"/>.
  /// </param>
  /// <param name="logger">
  /// The <see cref="ILogger"/> to report the situation.
  /// </param>
  [Obsolete("This constructor will be deprecated in the future.")]
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

  [Obsolete("This method will be deprecated in the future.")]
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
