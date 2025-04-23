// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode.Transport;

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
  [Obsolete($"Use a constructor overload that takes {nameof(IMuninNodeServerFactory)} as an argument.")]
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

  [Obsolete($"Use {nameof(IMuninNodeServerFactory)} and {nameof(StartAsync)} instead.")]
  protected override Socket CreateServerSocket()
    => MuninNodeServer.CreateServerSocket(endPoint: GetLocalEndPointToBind());
}
