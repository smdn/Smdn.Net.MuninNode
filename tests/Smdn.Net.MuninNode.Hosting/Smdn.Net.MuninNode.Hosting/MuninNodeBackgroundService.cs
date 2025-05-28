// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.Hosting;

[TestFixture]
public class MuninNodeBackgroundServiceTests {
  [Test]
  public void Dispose()
  {
    var services = new ServiceCollection();

    services.AddHostedMuninNodeService(
      configureNode: options => { },
      buildNode: builder => { }
    );

    var serviceProvider = services.BuildServiceProvider();
    using var muninNodeService = (MuninNodeBackgroundService)serviceProvider.GetRequiredService<IHostedService>();

    Assert.That(
      muninNodeService.Dispose,
      Throws.Nothing
    );
    Assert.That(
      muninNodeService.Dispose,
      Throws.Nothing,
      "dispose again"
    );

    Assert.That(
      () => _ = muninNodeService.EndPoint,
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      async () => await muninNodeService.StartAsync(default),
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      async () => await muninNodeService.StopAsync(default),
      Throws.TypeOf<ObjectDisposedException>()
    );
  }

  [Test]
  [CancelAfter(5000)]
  public async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    const string HostName = "munin-node.localhost";
    var services = new ServiceCollection();

    services.AddHostedMuninNodeService(
      configureNode: options => {
        options.HostName = HostName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
          options.UseLoopbackAddress(port: 0);
        else
          options.UseAnyAddress(port: 0);
      },
      buildNode: builder => { }
    );

    var serviceProvider = services.BuildServiceProvider();
    using var muninNodeService = (MuninNodeBackgroundService)serviceProvider.GetRequiredService<IHostedService>();

    // start service
    await muninNodeService.StartAsync(cancellationToken);

    Assert.That(muninNodeService.EndPoint, Is.Not.Null);

    var ipEndPoint = muninNodeService.EndPoint as IPEndPoint;

    Assert.That(ipEndPoint, Is.Not.Null);
    Assert.That(ipEndPoint.Port, Is.Not.Zero);

    // connect to node
    Assert.That(
      async () => await ConnectToNodeAsync(ipEndPoint, HostName, cancellationToken),
      Throws.Nothing
    );

    // stop service
    await muninNodeService.StopAsync(cancellationToken);

    // attempt to connect to stopped node
    using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctsTimeout.Token);

    Assert.That(
      async () => await ConnectToNodeAsync(ipEndPoint, HostName, cts.Token),
      Throws.TypeOf<SocketException>().Or.InstanceOf<OperationCanceledException>()
    );

    static async Task ConnectToNodeAsync(
      IPEndPoint endPoint,
      string expectedBannerHostName,
      CancellationToken cancellationToken
    )
    {
      using var client = new TcpClient();

      await client.ConnectAsync(endPoint);

      using var reader = new StreamReader(
        stream: client.GetStream(),
        leaveOpen: false
      );

      Assert.That(
        await reader.ReadLineAsync(cancellationToken),
        Contains.Substring(expectedBannerHostName),
        "banner response"
      );

      client.Close();
    }
  }

  private class NullMuninNode : IMuninNode {
    public string HostName => "munin-node.localhost";
    public EndPoint EndPoint { get; } = new IPEndPoint(IPAddress.Any, 0);

    public Task RunAsync(CancellationToken cancellationToken)
      => Task.Run(() => cancellationToken.WaitHandle.WaitOne());
  }

  [Test]
  [CancelAfter(5000)]
  public async Task StopAsync_NodeDoesNotSupportGracefulShutdown(CancellationToken cancellationToken)
  {
    using var muninNodeService = new MuninNodeBackgroundService(new NullMuninNode());

    // start service
    using var ctsRunning = new CancellationTokenSource();

    await muninNodeService.StartAsync(ctsRunning.Token);

    ctsRunning.Cancel();

    // stop service
    Assert.That(
      async () => await muninNodeService.StopAsync(cancellationToken),
      Throws.Nothing
    );
  }

  private class HookStopNode : LocalNode {
    private sealed class NullPluginProvider : IPluginProvider {
      public IReadOnlyCollection<IPlugin> Plugins { get; } = [];
      public INodeSessionCallback? SessionCallback => null;
    }

    public override IPluginProvider PluginProvider { get; } = new NullPluginProvider();
    public override string HostName => "munin-node.localhost";

    public EventHandler? Stopping = null;
    public EventHandler? Stopped = null;

    public HookStopNode()
      : base(
        listenerFactory: null,
        accessRule: null,
        logger: null
      )
    {
    }

    protected override ValueTask StoppingAsync(CancellationToken cancellationToken)
    {
      Stopping?.Invoke(this, EventArgs.Empty);

      return default;
    }

    protected override ValueTask StoppedAsync(CancellationToken cancellationToken)
    {
      Stopped?.Invoke(this, EventArgs.Empty);

      return default;
    }
  }

  [Test]
  [CancelAfter(5000)]
  public async Task StopAsync_NodeSupportsGracefulShutdown(CancellationToken cancellationToken)
  {
    var numberOfTimesStoppingInvoked = 0;
    var numberOfTimesStoppedInvoked = 0;

    using var hookStopNode = new HookStopNode();

    hookStopNode.Stopping += (_, _) => numberOfTimesStoppingInvoked++;
    hookStopNode.Stopped += (_, _) => numberOfTimesStoppedInvoked++;

    using var muninNodeService = new MuninNodeBackgroundService(hookStopNode);

    // start service
    using var ctsRunning = new CancellationTokenSource();

    await muninNodeService.StartAsync(ctsRunning.Token);

    ctsRunning.Cancel();

    // stop service
    Assert.That(
      async () => await muninNodeService.StopAsync(cancellationToken),
      Throws.Nothing
    );

    Assert.That(numberOfTimesStoppingInvoked, Is.EqualTo(1));
    Assert.That(numberOfTimesStoppedInvoked, Is.EqualTo(1));
  }

  [Test]
  public async Task StopAsync_NodeSupportsGracefulShutdown_CancellationRequestedBeforeStopping()
  {
    var numberOfTimesStoppingInvoked = 0;
    var numberOfTimesStoppedInvoked = 0;

    using var hookStopNode = new HookStopNode();

    hookStopNode.Stopping += (_, _) => numberOfTimesStoppingInvoked++;
    hookStopNode.Stopped += (_, _) => numberOfTimesStoppedInvoked++;

    using var muninNodeService = new MuninNodeBackgroundService(hookStopNode);

    // start service
    using var ctsRunning = new CancellationTokenSource();

    await muninNodeService.StartAsync(ctsRunning.Token);

    ctsRunning.Cancel();

    // stop service
    Assert.That(
      async () => await muninNodeService.StopAsync(new CancellationToken(canceled: true)),
      Throws.InstanceOf<OperationCanceledException>()
    );

    Assert.That(numberOfTimesStoppingInvoked, Is.EqualTo(0));
    Assert.That(numberOfTimesStoppedInvoked, Is.EqualTo(0));
  }

  [Test]
  public async Task StopAsync_NodeSupportsGracefulShutdown_CancellationRequestedWhileShutdown()
  {
    var numberOfTimesStoppingInvoked = 0;
    var numberOfTimesStoppedInvoked = 0;

    using var hookStopNode = new HookStopNode();
    using var ctsStopping = new CancellationTokenSource();

    hookStopNode.Stopping += (_, _) => {
      numberOfTimesStoppingInvoked++;
      ctsStopping.Cancel(); // request cancellation during graceful shutdown
    };
    hookStopNode.Stopped += (_, _) => numberOfTimesStoppedInvoked++;

    using var muninNodeService = new MuninNodeBackgroundService(hookStopNode);

    // start service
    using var ctsRunning = new CancellationTokenSource();

    await muninNodeService.StartAsync(ctsRunning.Token);

    ctsRunning.Cancel();

    // stop service
    Assert.That(
      async () => await muninNodeService.StopAsync(ctsStopping.Token),
      Throws.InstanceOf<OperationCanceledException>()
    );

    Assert.That(numberOfTimesStoppingInvoked, Is.EqualTo(1));
    Assert.That(numberOfTimesStoppedInvoked, Is.Zero);
  }
}
