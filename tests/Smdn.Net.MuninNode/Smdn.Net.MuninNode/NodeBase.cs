// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.InteropServices;
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
      : base(
        accessRule: accessRule,
        logger: null
      )
    {
      PluginProvider = new ReadOnlyCollectionPluginProvider(plugins);
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

    Assert.That(() => _ = node.LocalEndPoint, Throws.TypeOf<ObjectDisposedException>());
    Assert.That(node.Start, Throws.TypeOf<ObjectDisposedException>());
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

    Assert.That(() => _ = node.LocalEndPoint, Throws.TypeOf<ObjectDisposedException>());
    Assert.That(node.Start, Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await node.AcceptAsync(false, default), Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await node.AcceptSingleSessionAsync(default), Throws.TypeOf<ObjectDisposedException>());

    Assert.That(node.DisposeAsync, Throws.Nothing, "DisposeAsync() #2");
    Assert.That(node.Dispose, Throws.Nothing, "Dispose() after DisposeAsync()");
  }

  [Test]
  public async Task Start()
  {
    await using var node = CreateNode();

    Assert.That(() => _ = node.LocalEndPoint, Throws.InvalidOperationException, $"{nameof(node.LocalEndPoint)} before start");

    Assert.DoesNotThrow(node.Start);
    Assert.Throws<InvalidOperationException>(node.Start, "already started");

    Assert.That(() => _ = node.LocalEndPoint, Throws.Nothing, $"{nameof(node.LocalEndPoint)} after start");
  }
}
