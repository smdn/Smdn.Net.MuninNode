// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Smdn.Net.MuninNode.Hosting;

public partial class MuninNodeBackgroundService : BackgroundService {
  private IMuninNode node;

  /// <inheritdoc cref="IMuninNode.EndPoint"/>
  public EndPoint EndPoint => (node ?? throw new ObjectDisposedException(GetType().FullName)).EndPoint;

  protected ILogger? Logger { get; }

  // In addition to the Logger property exposed outside the class, provide an
  // ILogger field that is referenced by the LoggerMessage source generator.
  private readonly ILogger logger;

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

    // In addition to the Logger property, set a non-null ILogger field.
    // This field is used by the LoggerMessage source generator.
    this.logger = (ILogger?)logger ?? NullLogger.Instance;
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

    LogInformationStarting(logger, node.HostName);

    await base.StartAsync(cancellationToken).ConfigureAwait(false);

    LogInformationStarted(logger, node.HostName);
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

    LogInformationStopping(logger, node.HostName);

    await base.StopAsync(cancellationToken).ConfigureAwait(false);

    cancellationToken.ThrowIfCancellationRequested();

    // attempt graceful shutdown if possible
    if (node is NodeBase stoppableNode)
      await stoppableNode.StopAsync(cancellationToken).ConfigureAwait(false);

    LogInformationStopped(logger, node.HostName);

    if (node is IDisposable disposableNode)
      disposableNode.Dispose();
  }
}
