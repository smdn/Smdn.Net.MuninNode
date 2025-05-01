// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogInformation(ILogger, string?, params object?[])'

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode.Hosting;

public class MuninNodeBackgroundService : BackgroundService {
  private IMuninNode node;
  private readonly ILogger<MuninNodeBackgroundService>? logger;

  /// <inheritdoc cref="IMuninNode.EndPoint"/>
  public EndPoint EndPoint => (node ?? throw new ObjectDisposedException(GetType().FullName)).EndPoint;

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
    this.logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<MuninNodeBackgroundService>();
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
    this.logger = logger;
  }

  public override void Dispose()
  {
    if (node is IDisposable disposableNode)
      disposableNode.Dispose();

    node = null!;

    base.Dispose();

    GC.SuppressFinalize(this);
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (node is null)
      throw new ObjectDisposedException(GetType().FullName);

    logger?.LogInformation("Munin node '{HostName}' starting.", node.HostName);

    await node.RunAsync(stoppingToken).ConfigureAwait(false);

    logger?.LogInformation("Munin node '{HostName}' stopped.", node.HostName);

    if (node is IDisposable disposableNode)
      disposableNode.Dispose();
  }
}
