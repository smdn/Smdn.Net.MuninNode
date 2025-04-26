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
  [Obsolete($"Use a constructor overload that takes {nameof(IMuninNodeListenerFactory)} as an argument.")]
  protected NodeBase(
    IAccessRule? accessRule,
    ILogger? logger
  )
    : this(
      listenerFactory: new CreateFromStartedSocketListenerFactory(),
      accessRule: accessRule,
      logger: logger
    )
  {
  }

  /// <summary>
  /// An <see cref="IMuninNodeListenerFactory"/> implementation that uses the
  /// socket created by the <see cref="CreateServerSocket"/> method.
  /// </summary>
  [Obsolete]
  private sealed class CreateFromStartedSocketListenerFactory : IMuninNodeListenerFactory {
    /// <remarks>
    /// <paramref name="endPoint"/> is not referenced because this method uses
    /// a socket already bound to a specific endpoint.
    /// </remarks>
    public ValueTask<IMuninNodeListener> CreateAsync(
      EndPoint endPoint,
      IMuninNode node,
      CancellationToken cancellationToken
    )
    {
      if (node is not NodeBase n)
        throw new InvalidOperationException($"Expected the caller to be {nameof(NodeBase)}, but not.");

#pragma warning disable CA2000
      return new(
        MuninNodeListener.CreateFromStartedSocket(
          logger: n.Logger,
          listener: n.CreateServerSocket() ?? throw new InvalidOperationException("cannot start server")
        )
      );
#pragma warning restore CA2000
    }
  }

  [Obsolete($"Use {nameof(EndPoint)} instead.")]
  public EndPoint LocalEndPoint => EndPoint;

  [Obsolete($"Use {nameof(IMuninNodeListenerFactory)} and {nameof(StartAsync)} instead.")]
  protected virtual Socket CreateServerSocket()
    => throw new NotImplementedException(
      "This method will be deprecated in the future and its implementation has been disabled. " +
      $"Use {nameof(IMuninNodeListenerFactory)} and {nameof(StartAsync)} instead."
    );

  [Obsolete(
    "This method will be deprecated in the future." +
    $"Use {nameof(IMuninNodeListenerFactory)} and {nameof(StartAsync)} instead." +
    $"Make sure to override {nameof(CreateServerSocket)} if you need to use this method."
  )]
  public void Start()
  {
    ThrowIfDisposed();

    if (listener is not null)
      throw new InvalidOperationException("already started");

    Logger?.LogInformation("starting");

    var createListenerValueTask = listenerFactory.CreateAsync(
      endPoint: GetLocalEndPointToBind(),
      node: this,
      cancellationToken: default
    );

    listener = createListenerValueTask.IsCompleted
      ? createListenerValueTask.Result
      : createListenerValueTask.AsTask().GetAwaiter().GetResult();

    var startListenerValueTask = listener.StartAsync(cancellationToken: default);

    if (!startListenerValueTask.IsCompleted)
      startListenerValueTask.AsTask().GetAwaiter().GetResult();

    Logger?.LogInformation("started (end point: {EndPoint})", listener.EndPoint);
  }
}
