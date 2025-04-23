// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode.Transport;

internal sealed partial class MuninNodeServer : IMuninNodeServer {
  private readonly EndPoint endPoint;
  private readonly ILogger? logger;
  private readonly IServiceProvider? serviceProvider;
  private Socket? server;
  private bool isRunning;

  public EndPoint? EndPoint => server?.LocalEndPoint;

  private const bool EnableDualMode = true; // XXX: this should be configurable(?)

  internal MuninNodeServer(EndPoint endPoint, IServiceProvider? serviceProvider)
    : this(
      endPoint: endPoint,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<MuninNodeServer>(),
      serviceProvider: serviceProvider
    )
  {
  }

  internal MuninNodeServer(EndPoint endPoint, ILogger? logger, IServiceProvider? serviceProvider)
  {
    this.endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
    this.logger = logger;
    this.serviceProvider = serviceProvider;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  public async ValueTask DisposeAsync()
  {
    await DisposeAsyncCore().ConfigureAwait(false);

    Dispose(disposing: false);
    GC.SuppressFinalize(this);
  }

  // protected virtual
  private
#if SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_REUSESOCKET_CANCELLATIONTOKEN
  async
#endif
  ValueTask DisposeAsyncCore()
  {
    try {
      if (server is not null && server.Connected) {
#if SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_REUSESOCKET_CANCELLATIONTOKEN
        await server.DisconnectAsync(reuseSocket: false).ConfigureAwait(false);
#else
        server.Disconnect(reuseSocket: false);
#endif
      }
    }
    catch (SocketException) {
      // swallow
    }

    server?.Close();
    server?.Dispose();
    server = null;

#if !SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_REUSESOCKET_CANCELLATIONTOKEN
    return default;
#endif
  }

  // protected virtual
  private void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    try {
      if (server is not null && server.Connected)
        server.Disconnect(reuseSocket: false);
    }
    catch (SocketException) {
      // swallow
    }

    server?.Close();
    server?.Dispose();
    server = null!;
  }

  public ValueTask StartAsync(CancellationToken cancellationToken)
  {
    if (isRunning)
      throw new InvalidOperationException("already started");

    try {
      if (server is null) {
        server = CreateServerSocket(endPoint);
      }
      else {
        // since the `server` has already been assigned to a bound socket, treat as running state
      }

      isRunning = true;

      return default;
    }
    catch {
      server = null;

      throw;
    }
  }

  internal static Socket CreateServerSocket(EndPoint endPoint)
  {
    const int MaxClients = 1;

    Socket? server = null;

    try {
#pragma warning disable CA2000
      server = new(
        endPoint.AddressFamily,
        SocketType.Stream,
        ProtocolType.Tcp
      );
#pragma warning restore CA2000

      if (
        EnableDualMode &&
        endPoint.AddressFamily == AddressFamily.InterNetworkV6 &&
        Socket.OSSupportsIPv4
      ) {
        server.DualMode = true;
      }

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

  public async ValueTask<IMuninNodeClient> AcceptAsync(CancellationToken cancellationToken)
  {
    if (!isRunning)
      throw new InvalidOperationException("not started or already closed");
    if (server is null)
      throw new InvalidOperationException("invalid state: server is not initialized");

    var client = await server
#if SYSTEM_NET_SOCKETS_SOCKET_ACCEPTASYNC_CANCELLATIONTOKEN
      .AcceptAsync(cancellationToken: cancellationToken)
#else
      .AcceptAsync()
#endif
      .ConfigureAwait(false);

    return new MuninNodeClient(
      client: client,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<TcpClient>() ?? logger
    );
  }
}
