// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

public class LocalNode : NodeBase {
  private const string DefaultHostName = "munin-node.localhost";
  private static readonly int MaxClients = 1;

  public IPEndPoint LocalEndPoint { get; }

  public LocalNode(
    IReadOnlyList<Plugin> plugins,
    int port,
    IServiceProvider? serviceProvider = null
  )
    : this(
      plugins: plugins,
      hostName: DefaultHostName,
      port: port,
      serviceProvider: serviceProvider
    )
  {
  }

  public LocalNode(
    IReadOnlyList<Plugin> plugins,
    string hostName,
    int port,
    IServiceProvider? serviceProvider = null
  )
    : base(
      plugins: plugins,
      hostName: hostName,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<LocalNode>()
    )
  {
    if (Socket.OSSupportsIPv6) {
      LocalEndPoint = new IPEndPoint(
        address: IPAddress.IPv6Loopback,
        port: port
      );
    }
#pragma warning disable IDE0045
    else if (Socket.OSSupportsIPv4) {
#pragma warning restore IDE0045
      LocalEndPoint = new IPEndPoint(
        address: IPAddress.Loopback,
        port: port
      );
    }
    else {
      throw new NotSupportedException();
    }
  }

  protected override Socket CreateServerSocket()
  {
    Socket? server = null;

    try {
      server = new Socket(
        LocalEndPoint.AddressFamily,
        SocketType.Stream,
        ProtocolType.Tcp
      );

      server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      server.Bind(LocalEndPoint);
      server.Listen(MaxClients);

      return server;
    }
    catch {
      server?.Dispose();
      throw;
    }
  }

  protected override bool IsClientAcceptable(IPEndPoint remoteEndPoint)
    => IPAddress.IsLoopback(remoteEndPoint.Address);
}
