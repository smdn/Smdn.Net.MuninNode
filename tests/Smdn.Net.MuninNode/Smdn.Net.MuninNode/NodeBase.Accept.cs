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

    using var client = CreateClient((IPEndPoint)node.EndPoint, out var writer, out var reader);

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

    using var client = CreateClient((IPEndPoint)node.EndPoint, out var writer, out var reader);

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

    using var client = CreateClient((IPEndPoint)node.EndPoint, out _, out _);

    client.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [Test]
  public async Task AcceptSingleSessionAsync_ClientDisconnected_WhileAwaitingCommand()
  {
    await using var node = CreateNode();

    await node.StartAsync();

    var taskAccept = Task.Run(async () => await node.AcceptSingleSessionAsync());

    using var client = CreateClient((IPEndPoint)node.EndPoint, out _, out var reader);

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
        client.Client.Poll(1 /*microseconds*/, SelectMode.SelectRead) &&
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
        client.Client.Poll(1 /*microseconds*/, SelectMode.SelectRead) &&
        client.Client.Available == 0
      );

      Assert.That(connected, Is.False);
    });
  }

#pragma warning disable CS0618
  private class PseudoPluginProviderWithSessionCallback : IPluginProvider, INodeSessionCallback {
    public IReadOnlyCollection<IPlugin> Plugins { get; }

    private readonly bool setSessionCallbackNull;
    [Obsolete] public INodeSessionCallback? SessionCallback => setSessionCallbackNull ? null : this;
    public List<string> StartedSessionIds { get; } = new();
    public List<string> ClosedSessionIds { get; } = new();

    public PseudoPluginProviderWithSessionCallback(IReadOnlyCollection<IPlugin> plugins, bool setSessionCallbackNull)
    {
      Plugins = plugins;
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

  private class PseudoPluginWithSessionCallback : IPlugin, INodeSessionCallback {
    public string Name => "plugin";
    public IPluginGraphAttributes GraphAttributes => throw new NotImplementedException();
    public IPluginDataSource DataSource => throw new NotImplementedException();

    private readonly bool setSessionCallbackNull;
    [Obsolete] public INodeSessionCallback? SessionCallback => setSessionCallbackNull ? null : this;
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
#pragma warning restore CS0618

#pragma warning disable CS0612
  [Test]
  public async Task AcceptSingleSessionAsync_INodeSessionCallback(
    [Values] bool setPluginProviderSessionCallbackNull,
    [Values] bool setPluginSessionCallbackNull
  )
  {
    var plugin = new PseudoPluginWithSessionCallback(setPluginSessionCallbackNull);
    var isPluginSessionCallbackNull = plugin.SessionCallback is null;
    var pluginProvider = new PseudoPluginProviderWithSessionCallback(
      plugins: [plugin],
      setSessionCallbackNull: setPluginProviderSessionCallbackNull
    );
    var isPluginProviderSessionCallbackNull = pluginProvider.SessionCallback is null;

    Assert.That(isPluginProviderSessionCallbackNull, Is.EqualTo(setPluginProviderSessionCallbackNull));
    Assert.That(isPluginSessionCallbackNull, Is.EqualTo(setPluginSessionCallbackNull));

    await using var node = CreateNode(
      accessRule: null,
      pluginProvider: pluginProvider
    );

    await node.StartAsync();

    var taskAccept = Task.Run(async () => await node.AcceptSingleSessionAsync());

    Assert.That(plugin.StartedSessionIds.Count, Is.Zero, nameof(plugin.StartedSessionIds));
    Assert.That(plugin.ClosedSessionIds.Count, Is.Zero, nameof(plugin.ClosedSessionIds));

    using var client = CreateClient((IPEndPoint)node.EndPoint, out var writer, out var reader);

    var banner = reader.ReadLine();

    Assert.That(banner, Is.EqualTo($"# munin node at {node.HostName}"), nameof(banner));

    await Task.Delay(500); // wait for node process completed

    if (isPluginProviderSessionCallbackNull) {
      Assert.That(pluginProvider.StartedSessionIds.Count, Is.Zero, nameof(pluginProvider.StartedSessionIds));
      Assert.That(pluginProvider.ClosedSessionIds.Count, Is.Zero, nameof(pluginProvider.ClosedSessionIds));
    }
    else {
      Assert.That(pluginProvider.StartedSessionIds.Count, Is.EqualTo(1), nameof(pluginProvider.StartedSessionIds));
      Assert.That(pluginProvider.StartedSessionIds[0], Is.Not.Empty, nameof(pluginProvider.StartedSessionIds));

      Assert.That(pluginProvider.ClosedSessionIds.Count, Is.Zero, nameof(pluginProvider.ClosedSessionIds));
    }

    if (isPluginSessionCallbackNull) {
      Assert.That(plugin.StartedSessionIds.Count, Is.Zero, nameof(plugin.StartedSessionIds));
      Assert.That(plugin.ClosedSessionIds.Count, Is.Zero, nameof(plugin.ClosedSessionIds));
    }
    else {
      Assert.That(plugin.StartedSessionIds.Count, Is.EqualTo(1), nameof(plugin.StartedSessionIds));
      Assert.That(plugin.StartedSessionIds[0], Is.Not.Empty, nameof(plugin.StartedSessionIds));

      Assert.That(plugin.ClosedSessionIds.Count, Is.Zero, nameof(plugin.ClosedSessionIds));
    }

    writer.WriteLine(".");
    writer.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);

    if (isPluginProviderSessionCallbackNull) {
      Assert.That(pluginProvider.StartedSessionIds.Count, Is.Zero, nameof(pluginProvider.StartedSessionIds));
      Assert.That(pluginProvider.ClosedSessionIds.Count, Is.Zero, nameof(pluginProvider.ClosedSessionIds));
    }
    else {
      Assert.That(pluginProvider.StartedSessionIds.Count, Is.EqualTo(1), nameof(pluginProvider.StartedSessionIds));

      Assert.That(pluginProvider.ClosedSessionIds.Count, Is.EqualTo(1), nameof(pluginProvider.ClosedSessionIds));
      Assert.That(pluginProvider.ClosedSessionIds[0], Is.Not.Empty, nameof(pluginProvider.ClosedSessionIds));
    }

    if (isPluginSessionCallbackNull) {
      Assert.That(plugin.StartedSessionIds.Count, Is.Zero, nameof(plugin.StartedSessionIds));
      Assert.That(plugin.ClosedSessionIds.Count, Is.Zero, nameof(plugin.ClosedSessionIds));
    }
    else {
      Assert.That(plugin.StartedSessionIds.Count, Is.EqualTo(1), nameof(plugin.StartedSessionIds));

      Assert.That(plugin.ClosedSessionIds.Count, Is.EqualTo(1), nameof(plugin.ClosedSessionIds));
      Assert.That(plugin.ClosedSessionIds[0], Is.Not.Empty, nameof(plugin.ClosedSessionIds));
    }
  }
#pragma warning restore CS0612

  [TestCase(true)]
  [TestCase(false)]
  public async Task AcceptAsync(bool throwIfCancellationRequested)
  {
    await using var node = CreateNode();

    await node.StartAsync();

    using var cts = new CancellationTokenSource();

    var taskAccept = Task.Run(async () => await node.AcceptAsync(throwIfCancellationRequested, cts.Token));

    using var client0 = CreateClient((IPEndPoint)node.EndPoint, out var writer0, out var reader0);

    reader0.ReadLine();
    writer0.WriteLine(".");
    writer0.Close();

    Assert.That(taskAccept.Wait(TimeSpan.FromSeconds(1.0)), Is.False, "task must not be completed");

    using var client1 = CreateClient((IPEndPoint)node.EndPoint, out var writer1, out var reader1);

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

  [Test]
  public async Task RunAsync()
  {
    await using var node = CreateNode();

    using var cts = new CancellationTokenSource();

    var taskRun = node.RunAsync(cts.Token);

    for (var i = 0; i < 3; i++) {
      using var client = CreateClient((IPEndPoint)node.EndPoint, out var writer, out var reader);

      Assert.That(reader.ReadLine(), Contains.Substring(node.HostName), "banner");

      writer.WriteLine(".");
      writer.Close();

      Assert.That(
        taskRun.Wait(TimeSpan.FromSeconds(0.5)),
        Is.False,
        "task must not be completed"
      );
    }

    Assert.That(
      async () => await node.RunAsync(cts.Token),
      Throws.InvalidOperationException,
      "already running"
    );

    cts.Cancel();

    Assert.That(
      async () => await taskRun,
      Throws
        .InstanceOf<OperationCanceledException>()
        .With
        .Property(nameof(OperationCanceledException.CancellationToken))
        .EqualTo(cts.Token)
    );
  }
}
