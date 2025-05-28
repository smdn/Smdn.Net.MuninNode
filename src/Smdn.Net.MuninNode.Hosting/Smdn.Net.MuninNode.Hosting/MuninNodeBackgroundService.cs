// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode.Hosting;

public class MuninNodeBackgroundService : BackgroundService {
  private static readonly Action<ILogger, string, Exception?> LogStarting = LoggerMessage.Define<string>(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Munin node '{HostName}' starting."
  );
  private static readonly Action<ILogger, string, Exception?> LogStarted = LoggerMessage.Define<string>(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Munin node '{HostName}' started."
  );
  private static readonly Action<ILogger, string, Exception?> LogStopping = LoggerMessage.Define<string>(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Munin node '{HostName}' stopping."
  );
  private static readonly Action<ILogger, string, Exception?> LogStopped = LoggerMessage.Define<string>(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Munin node '{HostName}' stopped."
  );

  private IMuninNode node;

  /// <inheritdoc cref="IMuninNode.EndPoint"/>
  public EndPoint EndPoint => (node ?? throw new ObjectDisposedException(GetType().FullName)).EndPoint;

  protected ILogger? Logger { get; }

#if false
  // TODO: support ServiceKey
  // this code does not work currently
  // https://github.com/dotnet/runtime/issues/99085
  public MuninNodeBackgroundService(
    [ServiceKey] string serviceKey,
    IServiceProvider serviceProvider
  )
  {
    this.node = serviceProvider.GetRequiredKeyedService<IMuninNode>(serviceKey);
    Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<MuninNodeBackgroundService>();
  }
#endif

  public MuninNodeBackgroundService(
    IMuninNode node
  )
    : this(
      node: node ?? throw new ArgumentNullException(nameof(node)),
      logger: null
    )
  {
  }

  public MuninNodeBackgroundService(
    IMuninNode node,
    ILogger<MuninNodeBackgroundService>? logger
  )
  {
    this.node = node ?? throw new ArgumentNullException(nameof(node));
    Logger = logger;
  }

  public override void Dispose()
  {
    if (node is IDisposable disposableNode)
      disposableNode.Dispose();

    node = null!;

    base.Dispose();

    GC.SuppressFinalize(this);
  }

  public override async Task StartAsync(CancellationToken cancellationToken)
  {
    if (node is null)
      throw new ObjectDisposedException(GetType().FullName);

    cancellationToken.ThrowIfCancellationRequested();

    if (Logger is not null)
      LogStarting(Logger, node.HostName, null);

    await base.StartAsync(cancellationToken).ConfigureAwait(false);

    if (Logger is not null)
      LogStarted(Logger, node.HostName, null);
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (node is null)
      throw new ObjectDisposedException(GetType().FullName);

    await node.RunAsync(stoppingToken).ConfigureAwait(false);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    if (node is null)
      throw new ObjectDisposedException(GetType().FullName);

    if (Logger is not null)
      LogStopping(Logger, node.HostName, null);

    await base.StopAsync(cancellationToken).ConfigureAwait(false);

    cancellationToken.ThrowIfCancellationRequested();

    // attempt graceful shutdown if possible
    if (node is NodeBase stoppableNode)
      await stoppableNode.StopAsync(cancellationToken).ConfigureAwait(false);

    if (Logger is not null)
      LogStopped(Logger, node.HostName, null);

    if (node is IDisposable disposableNode)
      disposableNode.Dispose();
  }
}
