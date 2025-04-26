// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode.Transport;

#pragma warning disable IDE0040
partial class MuninNodeListener {
#pragma warning disable IDE0040
  /// <remarks>
  /// This constructor is used to maintain the functionality of the old API of
  /// creating server sockets with the <see cref="NodeBase.CreateServerSocket"/>.
  /// </remarks>
  [Obsolete]
  private MuninNodeListener(Socket listener, ILogger? logger)
  {
    if (listener is null)
      throw new ArgumentNullException(nameof(listener));
    if (listener.LocalEndPoint is null)
      throw new ArgumentException(message: "Must be a socket already bound to an endpoint.", paramName: nameof(listener));

    endPoint = listener.LocalEndPoint; // although set, this value would never be used
    this.listener = listener;
    this.logger = logger;
  }

  [Obsolete]
  internal static MuninNodeListener CreateFromStartedSocket(Socket listener, ILogger? logger)
  {
    if (listener is null)
      throw new ArgumentNullException(nameof(listener));

    return new(listener, logger);
  }
}
