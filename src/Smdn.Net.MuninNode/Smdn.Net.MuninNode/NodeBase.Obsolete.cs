// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

// TODO: use LoggerMessage.Define
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogInformation(ILogger, string?, params object?[])'

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode.Transport;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class NodeBase {
#pragma warning restore IDE0040
  [Obsolete($"Use a constructor overload that takes {nameof(IMuninNodeServerFactory)} as an argument.")]
  protected NodeBase(
    IAccessRule? accessRule,
    ILogger? logger
  )
    : this(
      serverFactory: new CreateFromStartedSocketServerFactory(),
      accessRule: accessRule,
      logger: logger
    )
  {
  }

  /// <summary>
  /// An <see cref="IMuninNodeServerFactory"/> implementation that uses the
  /// socket created by the <see cref="CreateServerSocket"/> method.
  /// </summary>
  [Obsolete]
  private sealed class CreateFromStartedSocketServerFactory : IMuninNodeServerFactory {
    /// <remarks>
    /// <paramref name="endPoint"/> is not referenced because this method uses
    /// a socket already bound to a specific endpoint.
    /// </remarks>
    public ValueTask<IMuninNodeServer> CreateAsync(
      EndPoint endPoint,
      IMuninNode node,
      CancellationToken cancellationToken
    )
    {
      if (node is not NodeBase n)
        throw new InvalidOperationException($"Expected the caller to be {nameof(NodeBase)}, but not.");

#pragma warning disable CA2000
      return new(
        MuninNodeServer.CreateFromStartedSocket(
          logger: n.Logger,
          server: n.CreateServerSocket() ?? throw new InvalidOperationException("cannot start server")
        )
      );
#pragma warning restore CA2000
    }
  }

  [Obsolete($"Use {nameof(IMuninNodeServerFactory)} instead.")]
  protected virtual Socket CreateServerSocket()
    => throw new NotImplementedException(
      "This method will be deprecated in the future and its implementation has been disabled. " +
      $"Use {nameof(IMuninNodeServerFactory)} instead."
    );

  [Obsolete(
    "This method will be deprecated in the future." +
    $"Use {nameof(IMuninNodeServerFactory)} instead." +
    $"Make sure to override {nameof(CreateServerSocket)} if you need to use this method."
  )]
  public void Start()
  {
    ThrowIfDisposed();

    if (server is not null)
      throw new InvalidOperationException("already started");

    Logger?.LogInformation("starting");

    var createServerValueTask = serverFactory.CreateAsync(
      endPoint: GetLocalEndPointToBind(),
      node: this,
      cancellationToken: default
    );

    server = createServerValueTask.IsCompleted
      ? createServerValueTask.Result
      : createServerValueTask.AsTask().GetAwaiter().GetResult();

    var startServerValueTask = server.StartAsync(cancellationToken: default);

    if (!startServerValueTask.IsCompleted)
      startServerValueTask.AsTask().GetAwaiter().GetResult();

    Logger?.LogInformation("started (end point: {EndPoint})", server.EndPoint);
  }
}
