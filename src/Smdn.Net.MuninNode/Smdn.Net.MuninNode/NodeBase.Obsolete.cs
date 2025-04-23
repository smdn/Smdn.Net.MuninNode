// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

// TODO: use LoggerMessage.Define
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogInformation(ILogger, string?, params object?[])'

using System;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class NodeBase {
#pragma warning restore IDE0040
  [Obsolete("This method will be deprecated in the future.")]
  protected virtual Socket CreateServerSocket()
    => throw new NotImplementedException(
      "This method will be deprecated in the future and its implementation has been disabled."
    );

  [Obsolete("This method will be deprecated in the future.")]
  public void Start()
  {
    ThrowIfDisposed();

    if (server is not null)
      throw new InvalidOperationException("already started");

    Logger?.LogInformation("starting");

    server = CreateServerSocket() ?? throw new InvalidOperationException("cannot start server");

    Logger?.LogInformation("started (end point: {LocalEndPoint})", server.LocalEndPoint);
  }
}
