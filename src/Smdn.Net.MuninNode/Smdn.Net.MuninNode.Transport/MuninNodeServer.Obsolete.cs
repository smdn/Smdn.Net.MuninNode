// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode.Transport;

#pragma warning disable IDE0040
partial class MuninNodeServer {
#pragma warning disable IDE0040
  /// <remarks>
  /// This constructor is used to maintain the functionality of the old API of
  /// creating server sockets with the <see cref="NodeBase.CreateServerSocket"/>.
  /// </remarks>
  [Obsolete]
  private MuninNodeServer(Socket server, ILogger? logger)
  {
    if (server is null)
      throw new ArgumentNullException(nameof(server));
    if (server.LocalEndPoint is null)
      throw new ArgumentException(message: "Must be a socket already bound to an endpoint.", paramName: nameof(server));

    endPoint = server.LocalEndPoint; // although set, this value would never be used
    this.server = server;
    this.logger = logger;
  }

  [Obsolete]
  internal static MuninNodeServer CreateFromStartedSocket(Socket server, ILogger? logger)
  {
    if (server is null)
      throw new ArgumentNullException(nameof(server));

    return new(server, logger);
  }
}
