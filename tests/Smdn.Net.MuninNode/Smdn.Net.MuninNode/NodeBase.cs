// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

[TestFixture]
public partial class NodeBaseTests {
  private class TestLocalNode : LocalNode {
    private class ReadOnlyCollectionPluginProvider : IPluginProvider {
      public IReadOnlyCollection<IPlugin> Plugins { get; }
      public INodeSessionCallback? SessionCallback => null;

      public ReadOnlyCollectionPluginProvider(IReadOnlyCollection<IPlugin> plugins)
      {
        Plugins = plugins;
      }
    }

    public override IPluginProvider PluginProvider { get; }
    public override string HostName => "test.munin-node.localhost";

    public TestLocalNode(
      IAccessRule? accessRule,
      IReadOnlyList<IPlugin> plugins
    )
      : this(
        accessRule: accessRule,
        pluginProvider: new ReadOnlyCollectionPluginProvider(plugins)
      )
    {
    }

    public TestLocalNode(
      IAccessRule? accessRule,
      IPluginProvider pluginProvider
    )
#pragma warning disable CS0618
      : base(
        accessRule: accessRule,
        logger: null
      )
#pragma warning restore CS0618
    {
      PluginProvider = pluginProvider;
    }

    protected override EndPoint GetLocalEndPointToBind()
      => new IPEndPoint(
        address:
          Socket.OSSupportsIPv6
            ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? IPAddress.IPv6Loopback : IPAddress.IPv6Any
            : Socket.OSSupportsIPv4
              ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? IPAddress.Loopback : IPAddress.Any
              : throw new NotSupportedException(),
        port: 0
      );
  }

  private static NodeBase CreateNode()
    => CreateNode(accessRule: null, plugins: Array.Empty<IPlugin>());

  private static NodeBase CreateNode(IReadOnlyList<IPlugin> plugins)
    => CreateNode(accessRule: null, plugins: plugins);

  private static NodeBase CreateNode(IAccessRule? accessRule, IReadOnlyList<IPlugin> plugins)
    => new TestLocalNode(accessRule, plugins);

  private static NodeBase CreateNode(IAccessRule? accessRule, IPluginProvider pluginProvider)
    => new TestLocalNode(accessRule, pluginProvider);

  private static TcpClient CreateClient(
    IPEndPoint endPoint,
    out StreamWriter writer,
    out StreamReader reader
  )
  {
    var client = new TcpClient(endPoint.AddressFamily);

    client.Connect(endPoint);

    writer = new StreamWriter(client.GetStream(), leaveOpen: true) {
      NewLine = "\r\n",
    };
    reader = new StreamReader(client.GetStream(), leaveOpen: true);

    return client;
  }

  [Test]
  public async Task Dispose()
  {
    await using var node = CreateNode();

    Assert.That(node.Dispose, Throws.Nothing, "Dispose() #1");

    Assert.That(() => _ = node.EndPoint, Throws.TypeOf<ObjectDisposedException>());
#pragma warning disable CS0618
    Assert.That(() => _ = node.LocalEndPoint, Throws.TypeOf<ObjectDisposedException>());
    Assert.That(node.Start, Throws.TypeOf<ObjectDisposedException>());
#pragma warning restore CS0618
    Assert.That(() => node.StartAsync(default), Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await node.StartAsync(default), Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await node.AcceptAsync(false, default), Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await node.AcceptSingleSessionAsync(default), Throws.TypeOf<ObjectDisposedException>());

    Assert.That(node.Dispose, Throws.Nothing, "Dispose() #2");
    Assert.That(node.DisposeAsync, Throws.Nothing, "DisposeAsync() after Dispose()");
  }

  [Test]
  public async Task DisposeAsync()
  {
    await using var node = CreateNode();

    Assert.That(node.DisposeAsync, Throws.Nothing, "DisposeAsync() #1");

    Assert.That(() => _ = node.EndPoint, Throws.TypeOf<ObjectDisposedException>());
#pragma warning disable CS0618
    Assert.That(() => _ = node.LocalEndPoint, Throws.TypeOf<ObjectDisposedException>());
    Assert.That(node.Start, Throws.TypeOf<ObjectDisposedException>());
#pragma warning restore CS0618
    Assert.That(() => node.StartAsync(default), Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await node.StartAsync(default), Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await node.AcceptAsync(false, default), Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await node.AcceptSingleSessionAsync(default), Throws.TypeOf<ObjectDisposedException>());

    Assert.That(node.DisposeAsync, Throws.Nothing, "DisposeAsync() #2");
    Assert.That(node.Dispose, Throws.Nothing, "Dispose() after DisposeAsync()");
  }

  [Test]
  public async Task Start()
  {
    await using var node = CreateNode();

    Assert.That(() => _ = node.EndPoint, Throws.InvalidOperationException, $"{nameof(node.EndPoint)} before start");
#pragma warning disable CS0618
    Assert.That(() => _ = node.LocalEndPoint, Throws.InvalidOperationException, $"{nameof(node.LocalEndPoint)} before start");
#pragma warning restore CS0618

#pragma warning disable CS0618
    Assert.DoesNotThrow(node.Start);
    Assert.Throws<InvalidOperationException>(node.Start, "already started");
#pragma warning restore CS0618

    Assert.That(() => _ = node.EndPoint, Throws.Nothing, $"{nameof(node.EndPoint)} after start");
#pragma warning disable CS0618
    Assert.That(() => _ = node.LocalEndPoint, Throws.Nothing, $"{nameof(node.LocalEndPoint)} after start");
#pragma warning restore CS0618
  }

  [Test]
  public async Task Start_Restart()
  {
    await using var node = CreateNode();

#pragma warning disable CS0618
    Assert.DoesNotThrow(node.Start);
    Assert.Throws<InvalidOperationException>(node.Start, "already started");
#pragma warning restore CS0618

    Assert.That(async () => await node.StopAsync(default), Throws.Nothing);

#pragma warning disable CS0618
    Assert.DoesNotThrow(node.Start, "restart");
    Assert.Throws<InvalidOperationException>(node.Start, "already restarted");
#pragma warning restore CS0618
  }

  [Test]
  public async Task StartAsync()
  {
    await using var node = CreateNode();

    Assert.That(() => _ = node.EndPoint, Throws.InvalidOperationException, $"{nameof(node.EndPoint)} before start");
#pragma warning disable CS0618
    Assert.That(() => _ = node.LocalEndPoint, Throws.InvalidOperationException, $"{nameof(node.LocalEndPoint)} before start");
#pragma warning restore CS0618

    Assert.That(async () => await node.StartAsync(default), Throws.Nothing);
    Assert.That(async () => await node.StartAsync(default), Throws.InvalidOperationException, "already started");

    Assert.That(() => _ = node.EndPoint, Throws.Nothing, $"{nameof(node.EndPoint)} after start");
#pragma warning disable CS0618
    Assert.That(() => _ = node.LocalEndPoint, Throws.Nothing, $"{nameof(node.LocalEndPoint)} after start");
#pragma warning restore CS0618
  }

  [Test]
  public async Task StartAsync_Restart()
  {
    await using var node = CreateNode();

#pragma warning disable CS0618
    Assert.That(async () => await node.StartAsync(default), Throws.Nothing);
    Assert.That(async () => await node.StartAsync(default), Throws.InvalidOperationException, "already started");
#pragma warning restore CS0618

    Assert.That(async () => await node.StopAsync(default), Throws.Nothing);

#pragma warning disable CS0618
    Assert.That(async () => await node.StartAsync(default), Throws.Nothing, "restart");
    Assert.That(async () => await node.StartAsync(default), Throws.InvalidOperationException, "already restarted");
#pragma warning restore CS0618
  }

  [Test]
  public async Task StopAsync_StartedByStartAsync()
  {
    await using var node = CreateNode();

    Assert.That(async () => await node.StartAsync(default), Throws.Nothing);

    Assert.That(async () => await node.StopAsync(default), Throws.Nothing);
    Assert.That(async () => await node.StopAsync(default), Throws.Nothing, "stop again");
  }

  [Test]
  public async Task StopAsync_StartedByStart()
  {
    await using var node = CreateNode();

#pragma warning disable CS0618
    Assert.That(() => node.Start(), Throws.Nothing);
#pragma warning restore CS0618

    Assert.That(async () => await node.StopAsync(default), Throws.Nothing);
    Assert.That(async () => await node.StopAsync(default), Throws.Nothing, "stop again");
  }

  [Test]
  public async Task StopAsync_NotStarted()
  {
    await using var node = CreateNode();

    Assert.That(async () => await node.StopAsync(default), Throws.Nothing);
    Assert.That(async () => await node.StopAsync(default), Throws.Nothing, "stop again");
  }

  [Test]
  public async Task StopAsync_WhileProcessingClient()
  {
    await using var node = CreateNode();

    await node.StartAsync();

    using var cts = new CancellationTokenSource();

    var taskAccept = Task.Run(async () => await node.AcceptAsync(throwIfCancellationRequested: false, cts.Token));

    using var client = CreateClient((IPEndPoint)node.EndPoint, out var writer, out var reader);

    reader.ReadLine(); // banner

    // attempt stop while processing client
    const int MaxAttempt = 10;

    for (var n = 0; n < MaxAttempt; n++) {
      using var ctsStopWhileProcessingClientTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(0.1));

      Assert.That(
        async () => await node.StopAsync(ctsStopWhileProcessingClientTimeout.Token),
        Throws
          .InstanceOf<OperationCanceledException>()
          .With
          .Property(nameof(OperationCanceledException.CancellationToken))
          .EqualTo(ctsStopWhileProcessingClientTimeout.Token)
      );
    }

    // close session, disconnect client
    writer.WriteLine(".");
    writer.Close();

    // stop accepting task
    cts.Cancel();

    Assert.DoesNotThrowAsync(async () => await taskAccept);

    // attempt stop after processing client
    using var ctsStopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));

    Assert.That(
      async () => await node.StopAsync(ctsStopTimeout.Token),
      Throws.Nothing
    );
  }
}
