// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

[TestFixture]
public partial class NodeBaseTests {
  private static async Task StartSession(
    IAccessRule? accessRule,
    Func<NodeBase, TcpClient, StreamWriter, StreamReader, CancellationToken, Task> action
  )
  {
    await using var node = CreateNode(accessRule, plugins: Array.Empty<IPlugin>());

    await node.StartAsync();

    using var cts = new CancellationTokenSource(
      delay: TimeSpan.FromSeconds(5) // timeout for hung up
    );

    var taskAccept = Task.Run(
      async () => await node.AcceptSingleSessionAsync(),
      cts.Token
    );

    using var client = CreateClient((IPEndPoint)node.LocalEndPoint, out var writer, out var reader);

    try {
      reader.ReadLine(); // banner

      try {
        await action(node, client, writer, reader, cts.Token);
      }
      finally {
        client.Close();
      }
    }
    finally {
      await taskAccept;
    }
  }

  [Test]
  public async Task AcceptSingleSessionAsync()
  {
    await using var node = CreateNode();

    await node.StartAsync();

    var taskAccept = Task.Run(async () => await node.AcceptSingleSessionAsync());

    using var client = CreateClient((IPEndPoint)node.LocalEndPoint, out var writer, out var reader);

    var banner = reader.ReadLine();

    Assert.That(banner, Is.EqualTo($"# munin node at {node.HostName}"), nameof(banner));

    writer.WriteLine(".");
    writer.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [Test]
  public async Task AcceptSingleSessionAsync_NodeNotStarted()
  {
    await using var node = CreateNode();

    Assert.ThrowsAsync<InvalidOperationException>(async () => await node.AcceptSingleSessionAsync());
  }

  [TestCase(0)]
  [TestCase(1)]
  [TestCase(1000)]
  public async Task AcceptSingleSessionAsync_CancellationRequested(int delayMilliseconds)
  {
    await using var node = CreateNode();

    await node.StartAsync();

    using var cts = new CancellationTokenSource(millisecondsDelay: delayMilliseconds);

    var ex = Assert.CatchAsync(async () => await node.AcceptSingleSessionAsync(cts.Token));

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public async Task AcceptSingleSessionAsync_ClientDisconnected_BeforeSendingBanner()
  {
    await using var node = CreateNode();

    await node.StartAsync();

    var taskAccept = Task.Run(async () => await node.AcceptSingleSessionAsync());

    using var client = CreateClient((IPEndPoint)node.LocalEndPoint, out _, out _);

    client.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [Test]
  public async Task AcceptSingleSessionAsync_ClientDisconnected_WhileAwaitingCommand()
  {
    await using var node = CreateNode();

    await node.StartAsync();

    var taskAccept = Task.Run(async () => await node.AcceptSingleSessionAsync());

    using var client = CreateClient((IPEndPoint)node.LocalEndPoint, out _, out var reader);

    reader.ReadLine(); // read banner

    client.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  private sealed class AcceptAllAccessRule : IAccessRule {
    public bool IsAcceptable(IPEndPoint remoteEndPoint) => true;
  }

  [Test]
  public async Task AcceptSingleSessionAsync_IAccessRule_AccessGranted()
  {
    await StartSession(
      accessRule: new AcceptAllAccessRule(),
      async static (node, client, writer, reader, cancellationToken
    ) => {
      await writer.WriteLineAsync("command", cancellationToken);
      await writer.FlushAsync(cancellationToken);

      Assert.That(
        await reader.ReadLineAsync(cancellationToken),
        Is.Not.Null,
        "line #1"
      );

      var connected = !(
        client.Client.Poll(1 /*microsecs*/, SelectMode.SelectRead) &&
        client.Client.Available == 0
      );

      Assert.That(connected, Is.True);
    });
  }

  private sealed class RefuseAllAccessRule : IAccessRule {
    public bool IsAcceptable(IPEndPoint remoteEndPoint) => false;
  }

  [Test]
  public async Task AcceptSingleSessionAsync_IAccessRule_AccessRefused()
  {
    await StartSession(
      accessRule: new RefuseAllAccessRule(),
      async static (node, client, writer, reader, cancellationToken
    ) => {
      await writer.WriteLineAsync(".", cancellationToken);
      await writer.FlushAsync(cancellationToken);

      try {
        Assert.That(
          await reader.ReadLineAsync(cancellationToken),
          Is.Null,
          "line #1"
        );
      }
      catch (IOException ex) when (
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
        ex.InnerException is SocketException
      ) {
        // ignore
      }

      var connected = !(
        client.Client.Poll(1 /*microsecs*/, SelectMode.SelectRead) &&
        client.Client.Available == 0
      );

      Assert.That(connected, Is.False);
    });
  }

  private class PseudoPluginWithSessionCallback : IPlugin, INodeSessionCallback {
    public string Name => throw new NotImplementedException();
    public IPluginGraphAttributes GraphAttributes => throw new NotImplementedException();
    public IPluginDataSource DataSource => throw new NotImplementedException();

    private readonly bool setSessionCallbackNull;
    public INodeSessionCallback? SessionCallback => setSessionCallbackNull ? null : this;
    public List<string> StartedSessionIds { get; } = new();
    public List<string> ClosedSessionIds { get; } = new();

    public PseudoPluginWithSessionCallback(bool setSessionCallbackNull)
    {
      this.setSessionCallbackNull = setSessionCallbackNull;
    }

    public ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
    {
      StartedSessionIds.Add(sessionId);

      return default;
    }

    public ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
    {
      ClosedSessionIds.Add(sessionId);

      return default;
    }
  }

  [TestCase(true)]
  [TestCase(false)]
  public async Task AcceptSingleSessionAsync_INodeSessionCallback(bool setSessionCallbackNull)
  {
    var plugin = new PseudoPluginWithSessionCallback(setSessionCallbackNull);
    var isSessionCallbackNull = plugin.SessionCallback is null;

    Assert.That(isSessionCallbackNull, Is.EqualTo(setSessionCallbackNull));

    await using var node = CreateNode(plugins: new IPlugin[] { plugin });

    await node.StartAsync();

    var taskAccept = Task.Run(async () => await node.AcceptSingleSessionAsync());

    Assert.That(plugin.StartedSessionIds.Count, Is.EqualTo(0), nameof(plugin.StartedSessionIds));
    Assert.That(plugin.ClosedSessionIds.Count, Is.EqualTo(0), nameof(plugin.ClosedSessionIds));

    using var client = CreateClient((IPEndPoint)node.LocalEndPoint, out var writer, out var reader);

    var banner = reader.ReadLine();

    Assert.That(banner, Is.EqualTo($"# munin node at {node.HostName}"), nameof(banner));

    await Task.Delay(500); // wait for node process completed

    if (isSessionCallbackNull) {
      Assert.That(plugin.StartedSessionIds.Count, Is.EqualTo(0), nameof(plugin.StartedSessionIds));
      Assert.That(plugin.ClosedSessionIds.Count, Is.EqualTo(0), nameof(plugin.ClosedSessionIds));
    }
    else {
      Assert.That(plugin.StartedSessionIds.Count, Is.EqualTo(1), nameof(plugin.StartedSessionIds));
      Assert.That(plugin.StartedSessionIds[0], Is.Not.Empty, nameof(plugin.StartedSessionIds));

      Assert.That(plugin.ClosedSessionIds.Count, Is.EqualTo(0), nameof(plugin.ClosedSessionIds));
    }

    writer.WriteLine(".");
    writer.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);

    if (isSessionCallbackNull) {
      Assert.That(plugin.StartedSessionIds.Count, Is.EqualTo(0), nameof(plugin.StartedSessionIds));
      Assert.That(plugin.ClosedSessionIds.Count, Is.EqualTo(0), nameof(plugin.ClosedSessionIds));
    }
    else {
      Assert.That(plugin.StartedSessionIds.Count, Is.EqualTo(1), nameof(plugin.StartedSessionIds));

      Assert.That(plugin.ClosedSessionIds.Count, Is.EqualTo(1), nameof(plugin.ClosedSessionIds));
      Assert.That(plugin.ClosedSessionIds[0], Is.Not.Empty, nameof(plugin.ClosedSessionIds));
    }
  }

  [TestCase(true)]
  [TestCase(false)]
  public async Task AcceptAsync(bool throwIfCancellationRequested)
  {
    await using var node = CreateNode();

    await node.StartAsync();

    using var cts = new CancellationTokenSource();

    var taskAccept = Task.Run(async () => await node.AcceptAsync(throwIfCancellationRequested, cts.Token));

    using var client0 = CreateClient((IPEndPoint)node.LocalEndPoint, out var writer0, out var reader0);

    reader0.ReadLine();
    writer0.WriteLine(".");
    writer0.Close();

    Assert.That(taskAccept.Wait(TimeSpan.FromSeconds(1.0)), Is.False, "task must not be completed");

    using var client1 = CreateClient((IPEndPoint)node.LocalEndPoint, out var writer1, out var reader1);

    reader1.ReadLine();
    writer1.WriteLine(".");
    writer1.Close();

    Assert.That(taskAccept.Wait(TimeSpan.FromSeconds(1.0)), Is.False, "task must not be completed");

    cts.Cancel();

    if (throwIfCancellationRequested)
      Assert.ThrowsAsync<OperationCanceledException>(async () => await taskAccept);
    else
      Assert.DoesNotThrowAsync(async () => await taskAccept);
  }
}
