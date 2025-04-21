// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
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
public class NodeBaseTests {
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

  private static NodeBase CreateNode(IAccessRule? accessRule)
    => CreateNode(accessRule: accessRule, plugins: Array.Empty<IPlugin>());

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

  private static Task StartSession(
    Func<NodeBase, TcpClient, StreamWriter, StreamReader, CancellationToken, Task> action
  )
    => StartSession(
      accessRule: null,
      plugins: Array.Empty<IPlugin>(),
      action: action
    );

  private static Task StartSession(
    IReadOnlyList<IPlugin> plugins,
    Func<NodeBase, TcpClient, StreamWriter, StreamReader, CancellationToken, Task> action
  )
    => StartSession(
      accessRule: null,
      plugins: plugins,
      action: action
    );

  private static async Task StartSession(
    IAccessRule? accessRule,
    IReadOnlyList<IPlugin> plugins,
    Func<NodeBase, TcpClient, StreamWriter, StreamReader, CancellationToken, Task> action
  )
  {
    await using var node = CreateNode(accessRule, plugins);

    node.Start();

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

  [Test]
  public async Task AcceptSingleSessionAsync()
  {
    await using var node = CreateNode();

    node.Start();

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

    node.Start();

    using var cts = new CancellationTokenSource(millisecondsDelay: delayMilliseconds);

    var ex = Assert.CatchAsync(async () => await node.AcceptSingleSessionAsync(cts.Token));

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public async Task AcceptSingleSessionAsync_ClientDisconnected_BeforeSendingBanner()
  {
    await using var node = CreateNode();

    node.Start();

    var taskAccept = Task.Run(async () => await node.AcceptSingleSessionAsync());

    using var client = CreateClient((IPEndPoint)node.LocalEndPoint, out _, out _);

    client.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [Test]
  public async Task AcceptSingleSessionAsync_ClientDisconnected_WhileAwaitingCommand()
  {
    await using var node = CreateNode();

    node.Start();

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
      plugins: Array.Empty<IPlugin>(),
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
      plugins: Array.Empty<IPlugin>(),
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

    node.Start();

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

    node.Start();

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

  [TestCase("\r\n")]
  [TestCase("\n")]
  public async Task ProcessCommandAsync_EndOfLine(string eol)
  {
    await using var node = CreateNode();

    node.Start();

    var taskAccept = Task.Run(async () => await node.AcceptSingleSessionAsync());

    using var client = CreateClient((IPEndPoint)node.LocalEndPoint, out var writer, out _);

    writer.Write(".");
    writer.Write(eol);
    writer.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [Test]
  public async Task ProcessCommandAsync_UnknownCommand()
  {
    const string UnknownCommand = "unknown";

    await StartSession(async static (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync(UnknownCommand, cancellationToken);
      await writer.FlushAsync(cancellationToken);

      Assert.That(
        await reader.ReadLineAsync(cancellationToken),
        Is.EqualTo("# Unknown command. Try cap, list, nodes, config, fetch, version or quit"),
        "line #1"
      );
    });
  }

  [Test]
  public async Task ProcessCommandAsync_NodesCommand()
  {
    await StartSession(async static (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync("nodes", cancellationToken);
      await writer.FlushAsync(cancellationToken);

      Assert.That(await reader.ReadLineAsync(cancellationToken), Is.EqualTo(node.HostName), "line #1");
      Assert.That(await reader.ReadLineAsync(cancellationToken), Is.EqualTo("."), "line #2");
    });
  }

  [TestCase(".")]
  [TestCase("quit")]
  public async Task ProcessCommandAsync_QuitCommand(string command)
  {
    await StartSession(async (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync(command, cancellationToken);
      await writer.FlushAsync(cancellationToken);

      Assert.That(client.Available, Is.EqualTo(0));

      try {
        Assert.That(await reader.ReadLineAsync(cancellationToken), Is.Null);

        // Assert.IsFalse(client.Connected, nameof(client.Connected));
      }
      catch (IOException ex) {
        Assert.That(ex!.InnerException, Is.InstanceOf<SocketException>()); // expected case

        // Assert.IsFalse(client.Connected, nameof(client.Connected));
      }
    });
  }

  [Test]
  public async Task ProcessCommandAsync_CapCommand()
  {
    await StartSession(async static (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync("cap", cancellationToken);
      await writer.FlushAsync(cancellationToken);

      Assert.That(await reader.ReadLineAsync(cancellationToken), Is.EqualTo("cap"), "line #1");
    });
  }

  [Test]
  public async Task ProcessCommandAsync_VersionCommand()
  {
    await StartSession(async static (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync("version", cancellationToken);
      await writer.FlushAsync(cancellationToken);

      var line = await reader.ReadLineAsync(cancellationToken);

      Assert.That(line, Does.Contain(node.HostName), "line #1 must contain hostname");
      Assert.That(line, Does.Contain(node.NodeVersion.ToString()), "line #1 must contain node version");
    });
  }

  [Test]
  public async Task ProcessCommandAsync_ListCommand()
  {
    var graphAttrs = new PluginGraphAttributes(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args"
    );

    var plugins = new[] {
      new Plugin(
        "plugin1",
        graphAttrs,
        Array.Empty<IPluginField>()
      ),
      new Plugin(
        "plugin2",
        graphAttrs,
        Array.Empty<IPluginField>()
      ),
    };

    await StartSession(
      plugins: plugins,
      action: async static (node, client, writer, reader, cancellationToken) => {
        await writer.WriteLineAsync("list", cancellationToken);
        await writer.FlushAsync(cancellationToken);

        Assert.That(await reader.ReadLineAsync(cancellationToken), Is.EqualTo("plugin1 plugin2"), "line #1");
      }
    );
  }

  private static Task GetCommandResponseAsync(
    IReadOnlyList<IPlugin> plugins,
    Func<string> getCommand,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => StartSession(
      plugins: plugins,
      action: async (node, client, writer, reader, cancellationToken) => {
        await writer.WriteLineAsync(getCommand(), cancellationToken);
        await writer.FlushAsync(cancellationToken);

        var lines = new List<string>();

        try {
          for (; ; ) {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
              break;

            lines.Add(line);

            if (line == ".")
              break;
          }
        }
        catch (IOException ex) when (ex.InnerException is SocketException) {
          // ignore
        }

        assertResponseLines(lines);
      }
    );

  private static System.Collections.IEnumerable YieldTestCases_ProcessCommandAsync_FetchCommand()
  {
    var graphAttrs = new PluginGraphAttributes(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args"
    );

    var plugins = new[] {
      PluginFactory.CreatePlugin(
        "plugin1",
        graphAttrs,
        new[] {
          PluginFactory.CreateField("plugin1field1", static () => 1.1),
          PluginFactory.CreateField("plugin1field2", static () => 1.2),
        }
      ),
      PluginFactory.CreatePlugin(
        "plugin2",
        "plugin2field1",
        static () => 2.1,
        graphAttrs
      ),
    };

    yield return new object[] {
      plugins,
      "fetch plugin1",
      new[] {
        "plugin1field1.value 1.1",
        "plugin1field2.value 1.2",
        "."
      }
    };

    yield return new object[] {
      plugins,
      "fetch plugin2",
      new[] {
        "plugin2field1.value 2.1",
        "."
      }
    };

    yield return new object[] {
      plugins,
      "fetch nonexistentplugin",
      new[] {
        "# Unknown service",
        "."
      }
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_FetchCommand))]
  [SetCulture("")]
  public Task ProcessCommandAsync_FetchCommand_InvariantCulture(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => ProcessCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_FetchCommand))]
  [SetCulture("ja_JP")]
  public Task ProcessCommandAsync_FetchCommand_JA_JP(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => ProcessCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_FetchCommand))]
  [SetCulture("fr_CH")]
  public Task ProcessCommandAsync_FetchCommand_FR_CH(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => ProcessCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_FetchCommand))]
  [SetCulture("ar_AE")]
  public Task ProcessCommandAsync_FetchCommand_AR_AE(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => ProcessCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  private Task ProcessCommandAsync_FetchCommand(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => GetCommandResponseAsync(
      plugins: plugins,
      getCommand: () => command,
      assertResponseLines: lines => {
        Assert.That(lines.Count, Is.EqualTo(expectedResponseLines.Length), nameof(lines.Count));

        foreach (var (expectedResponseLine, lineNumber) in expectedResponseLines.Select(static (line, index) => (line, index))) {
          Assert.That(lines[lineNumber], Is.EqualTo(expectedResponseLine), $"line #{lineNumber}");
        }
      }
    );

  [Test]
  public async Task ProcessCommandAsync_ConfigCommand_UnknownService()
  {
    var graphAttrs = new PluginGraphAttributes(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args"
    );

    var plugins = new[] {
      new Plugin(
        "plugin",
        graphAttrs,
        Array.Empty<IPluginField>()
      ),
    };

    await GetCommandResponseAsync(
      plugins: plugins,
      getCommand: () => "config nonexistentplugin",
      assertResponseLines: lines => {
        Assert.That(lines.Count, Is.EqualTo(2), "# of lines");
        Assert.That(lines[0], Is.EqualTo("# Unknown service"), "line #1");
        Assert.That(lines[1], Is.EqualTo("."), "line #2");
      }
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ProcessCommandAsync_ConfigCommand()
  {
    var graphAttrs = new PluginGraphAttributes(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args"
    );

    var plugins = new[] {
      PluginFactory.CreatePlugin(
        "plugin1",
        graphAttrs,
        new[] {
          PluginFactory.CreateField("plugin1field1", static () => 1.1),
          PluginFactory.CreateField("plugin1field2", PluginFieldGraphStyle.LineWidth3, static () => 1.2)
        }
      ),
      PluginFactory.CreatePlugin(
        "plugin2",
        "plugin2field1",
        PluginFieldGraphStyle.Area,
        static () => 2.1,
        graphAttrs
      ),
    };

    yield return new object[] {
      plugins,
      "config plugin1",
      new Action<IReadOnlyList<string>>(
        responseLines => {
          AssertGraphConfigurations(responseLines, (PluginGraphAttributes)plugins[0].GraphAttributes);

          // plugin1field1
          Assert.That(responseLines, Has.Member("plugin1field1.label plugin1field1"), "plugin1field1.label");
          Assert.That(responseLines, Has.No.Member("plugin1field1.draw "), "plugin1field1.draw");

          // plugin1field2
          Assert.That(responseLines, Has.Member("plugin1field2.label plugin1field2"), "plugin1field2.label");
          Assert.That(responseLines, Has.Member("plugin1field2.draw LINE3"), "plugin1field2.draw");
        }
      )
    };

    yield return new object[] {
      plugins,
      "config plugin2",
      new Action<IReadOnlyList<string>>(
        responseLines => {
          AssertGraphConfigurations(responseLines, (PluginGraphAttributes)plugins[1].GraphAttributes);

          // plugin2field1
          Assert.That(responseLines, Has.Member("plugin2field1.label plugin2field1"), "plugin2field1.label");
          Assert.That(responseLines, Has.Member("plugin2field1.draw AREA"), "plugin2field1.draw");
        }
      )
    };

    static void AssertGraphConfigurations(
      IReadOnlyList<string> responseLines,
      PluginGraphAttributes attributes
    )
    {
      var scaleString = attributes.Scale ? "yes" : "no";

      Assert.That(responseLines, Has.Member($"graph_title {attributes.Title}"), "graph_title");
      Assert.That(responseLines, Has.Member($"graph_category {attributes.Category}"), "graph_category");
      Assert.That(responseLines, Has.Member($"graph_args {attributes.Arguments}"), "graph_args");
      Assert.That(responseLines, Has.Member($"graph_scale {scaleString}"), "graph_scale");
      Assert.That(responseLines, Has.Member($"graph_vlabel {attributes.VerticalLabel}"), "graph_vlabel");
    }
  }

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_ConfigCommand))]
  public Task ProcessCommandAsync_ConfigCommand(
    IReadOnlyList<IPlugin> plugins,
    string command,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: plugins,
      getCommand: () => command,
      assertResponseLines: assertResponseLines
    );

  private static System.Collections.IEnumerable YieldTestCases_ProcessCommandAsync_ConfigCommand_OptionalAttributes()
  {
    foreach (var (width, height) in new (int?, int?)[] {
      (null, null),
      (100, null),
      (null, 100),
      (100, 100),
    }) {
      var graphAttrs = new PluginGraphAttributes(
        title: "title",
        category: "test",
        verticalLabel: "test",
        scale: false,
        arguments: "--args",
        updateRate: null,
        width: width,
        height: height,
        order: null,
        totalValueLabel: null
      );

      var plugin = PluginFactory.CreatePlugin(
        "plugin1",
        graphAttrs,
        new[] {
          PluginFactory.CreateField("plugin1field1", static () => 1.1),
        }
      );

      yield return new object[] {
        plugin,
        new Action<IReadOnlyList<string>>(
          responseLines => {
            if (width.HasValue)
              Assert.That(responseLines, Has.Member($"graph_width {width}"), "graph_width");
            else
              Assert.That(responseLines, Has.No.Member("graph_width "), "graph_width");

            if (height.HasValue)
              Assert.That(responseLines, Has.Member($"graph_height {height}"), "graph_height");
            else
              Assert.That(responseLines, Has.No.Member("graph_height "), "graph_height");
          }
        )
      };
    }

    foreach (var updateRate in new TimeSpan?[] {
      null,
      TimeSpan.FromSeconds(1.0),
      TimeSpan.FromSeconds(10.0)
    }) {
      var graphAttrs = new PluginGraphAttributes(
        title: "title",
        category: "test",
        verticalLabel: "test",
        scale: false,
        arguments: "--args",
        updateRate: updateRate,
        width: null,
        height: null,
        order: null,
        totalValueLabel: null
      );

      var plugin = PluginFactory.CreatePlugin(
        "plugin2",
        graphAttrs,
        new[] {
          PluginFactory.CreateField("plugin1field1", static () => 2.1),
        }
      );

      yield return new object[] {
        plugin,
        new Action<IReadOnlyList<string>>(
          responseLines => {
            if (updateRate.HasValue)
              Assert.That(responseLines, Has.Member($"update_rate {(int)updateRate.Value.TotalSeconds}"), "update_rate");
            else
              Assert.That(responseLines, Has.No.Member("update_rate "), "update_rate");
          }
        )
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_ConfigCommand_OptionalAttributes))]
  public Task ProcessCommandAsync_ConfigCommand_OptionalAttributes(
    IPlugin plugin,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: new[] { plugin },
      getCommand: () => $"config {plugin.Name}",
      assertResponseLines: assertResponseLines
    );

  private static System.Collections.IEnumerable YieldTestCases_ProcessCommandAsync_ConfigCommand_GraphOrder()
  {
    foreach (var (order, expectedGraphOrder) in new[] {
      (null, null),
      (Array.Empty<string>(), null),
      (new[] { "field1" }, "field1"),
      (new[] { "field2", "field1" }, "field2 field1"),
    }) {
      var graphAttrs = new PluginGraphAttributes(
        title: "title",
        category: "test",
        verticalLabel: "test",
        scale: false,
        arguments: "--args",
        updateRate: TimeSpan.FromMinutes(1),
        width: null,
        height: null,
        order: order,
        totalValueLabel: null
      );

      var plugin = PluginFactory.CreatePlugin(
        "plugin1",
        graphAttrs,
        new[] {
          PluginFactory.CreateField("plugin1field1", static () => 1.1),
          PluginFactory.CreateField("plugin1field2", PluginFieldGraphStyle.LineWidth3, static () => 1.2)
        }
      );

      yield return new object[] {
        plugin,
        new Action<IReadOnlyList<string>>(
          responseLines => {
            if (expectedGraphOrder is null)
              Assert.That(responseLines, Has.No.Member("graph_order "), "graph_order");
            else
              Assert.That(responseLines, Has.Member($"graph_order {expectedGraphOrder}"), "graph_order");
          }
        )
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_ConfigCommand_GraphOrder))]
  public Task ProcessCommandAsync_ConfigCommand_GraphOrder(
    IPlugin plugin,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: new[] { plugin },
      getCommand: () => $"config {plugin.Name}",
      assertResponseLines: assertResponseLines
    );

  private static System.Collections.IEnumerable YieldTestCases_ProcessCommandAsync_ConfigCommand_GraphTotal()
  {
    foreach (var total in new[] {
      null,
      string.Empty,
      "Total",
    }) {
      var graphAttrs = new PluginGraphAttributes(
        title: "title",
        category: "test",
        verticalLabel: "test",
        scale: false,
        arguments: "--args",
        updateRate: TimeSpan.FromMinutes(1),
        width: null,
        height: null,
        order: null,
        totalValueLabel: total
      );

      var plugin = PluginFactory.CreatePlugin(
        "plugin1",
        graphAttrs,
        new[] {
          PluginFactory.CreateField("plugin1field1", static () => 1.1),
          PluginFactory.CreateField("plugin1field2", PluginFieldGraphStyle.LineWidth3, static () => 1.2)
        }
      );

      yield return new object[] {
        plugin,
        new Action<IReadOnlyList<string>>(
          responseLines => {
            if (string.IsNullOrEmpty(total))
              Assert.That(responseLines, Has.No.Member("graph_total "), "graph_total");
            else
              Assert.That(responseLines, Has.Member($"graph_total {total}"), "graph_total");
          }
        )
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_ConfigCommand_GraphTotal))]
  public Task ProcessCommandAsync_ConfigCommand_GraphTotal(
    IPlugin plugin,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: new[] { plugin },
      getCommand: () => $"config {plugin.Name}",
      assertResponseLines: assertResponseLines
    );

  [TestCase(PluginFieldGraphStyle.Default, null, null)]
  [TestCase(PluginFieldGraphStyle.Area, "AREA", null)]
  [TestCase(PluginFieldGraphStyle.Stack, "STACK", null)]
  [TestCase(PluginFieldGraphStyle.AreaStack, "AREASTACK", null)]
  [TestCase(PluginFieldGraphStyle.Line, "LINE", null)]
  [TestCase(PluginFieldGraphStyle.LineWidth1, "LINE1", null)]
  [TestCase(PluginFieldGraphStyle.LineWidth2, "LINE2", null)]
  [TestCase(PluginFieldGraphStyle.LineWidth3, "LINE3", null)]
  [TestCase(PluginFieldGraphStyle.LineStack, "LINESTACK", null)]
  [TestCase(PluginFieldGraphStyle.LineStackWidth1, "LINE1STACK", null)]
  [TestCase(PluginFieldGraphStyle.LineStackWidth2, "LINE2STACK", null)]
  [TestCase(PluginFieldGraphStyle.LineStackWidth3, "LINE3STACK", null)]
  [TestCase((PluginFieldGraphStyle)(-1), null, typeof(InvalidOperationException))]
  [TestCase((PluginFieldGraphStyle)999999, null, typeof(InvalidOperationException))]
  public async Task ProcessCommandAsync_ConfigCommand_TranslateGraphStyle(
    PluginFieldGraphStyle style,
    string? expectedFieldDrawAttribute,
    Type? expectedExceptionType
  )
  {
    var graphAttrs = new PluginGraphAttributes(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args"
    );

    var plugins = new[] {
      PluginFactory.CreatePlugin(
        "plugin",
        "field",
        style,
        static () => 0.0,
        graphAttrs
      ),
    };

    try {
      await GetCommandResponseAsync(
        plugins: plugins,
        getCommand: () => "config plugin",
        assertResponseLines: lines => {
          if (expectedFieldDrawAttribute is null)
            Assert.That(lines, Has.No.Member("field.draw"));
          else
            Assert.That(lines, Has.Member($"field.draw {expectedFieldDrawAttribute}"));
        }
      );
    }
    catch (Exception ex) {
      if (expectedExceptionType is null)
        throw;

      Assert.That(ex, Is.InstanceOf(expectedExceptionType));
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_ProcessCommandAsync_ConfigCommand_WarningAndCriticalField()
  {
    foreach (var (range, expectedAttributeValue) in new[] {
      (PluginFieldNormalValueRange.CreateMin(0.0), "0:"),
      (PluginFieldNormalValueRange.CreateMin(1.0), "1:"),
      (PluginFieldNormalValueRange.CreateMax(0.0), ":0"),
      (PluginFieldNormalValueRange.CreateMax(-1.0), ":-1"),
      (PluginFieldNormalValueRange.CreateRange(-1.0, 1.0), "-1:1"),
      (PluginFieldNormalValueRange.None, null),
    }) {
      yield return new object?[] {
        PluginFactory.CreateField(
          label: "field",
          graphStyle: default,
          normalRangeForWarning: range,
          normalRangeForCritical: default,
          fetchValue: static () => 0.0
        ),
        expectedAttributeValue is null ? null : "field.warning " + expectedAttributeValue,
        null
      };

      yield return new object?[] {
        PluginFactory.CreateField(
          label: "field",
          graphStyle: default,
          normalRangeForWarning: default,
          normalRangeForCritical: range,
          fetchValue: static () => 0.0
        ),
        null,
        expectedAttributeValue is null ? null : "field.critical " + expectedAttributeValue,
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_ConfigCommand_WarningAndCriticalField))]
  public async Task ProcessCommandAsync_ConfigCommand_WarningAndCriticalField(
    IPluginField field,
    string? expectedFieldWarningAttributeLine,
    string? expectedFieldCriticalAttributeLine
  )
  {
    var graphAttrs = new PluginGraphAttributes(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args"
    );

    var plugins = new[] {
      PluginFactory.CreatePlugin(
        "plugin",
        graphAttrs,
        new[] { field }
      ),
    };

    await GetCommandResponseAsync(
      plugins: plugins,
      getCommand: () => "config plugin",
      assertResponseLines: lines => {
        if (expectedFieldWarningAttributeLine is null)
          Assert.That(lines, Has.No.Member(expectedFieldWarningAttributeLine));
        else
          Assert.That(lines, Has.Member(expectedFieldWarningAttributeLine));

        if (expectedFieldCriticalAttributeLine is null)
          Assert.That(lines, Has.No.Member(expectedFieldCriticalAttributeLine));
        else
          Assert.That(lines, Has.Member(expectedFieldCriticalAttributeLine));
      }
    );
  }

  [TestCase]
  public async Task ProcessCommandAsync_ConfigCommand_NegativeField()
  {
    var graphAttrs = new PluginGraphAttributes(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args"
    );

    const string PositiveFieldName = "field_plus";
    const string NegativeFieldName = "field_minus";

    var plugins = new[] {
      PluginFactory.CreatePlugin(
        "plugin",
        graphAttrs,
        new[] {
          PluginFactory.CreateField(
            name: PositiveFieldName,
            label: "Field(+)",
            graphStyle: default,
            normalRangeForWarning: default,
            normalRangeForCritical: default,
            negativeFieldName: NegativeFieldName,
            fetchValue: static () => +1.0
          ),
          PluginFactory.CreateField(
            name: NegativeFieldName,
            label: "Field(-)",
            normalRangeForWarning: default,
            normalRangeForCritical: default,
            graphStyle: default,
            fetchValue: static () => -1.0
          ),
        }
      ),
    };

    await GetCommandResponseAsync(
      plugins: plugins,
      getCommand: () => "config plugin",
      assertResponseLines: lines => {
        var expectedAttrNegativeLine = $"{PositiveFieldName}.negative {NegativeFieldName}";
        var expectedAttrGraphLine = $"{NegativeFieldName}.graph no";

        Assert.That(lines, Has.Member(expectedAttrNegativeLine));
        Assert.That(lines, Has.Member(expectedAttrGraphLine));

        var ls = lines.ToList();

        Assert.That(
          ls.IndexOf(expectedAttrGraphLine),
          Is.LessThan(ls.IndexOf(expectedAttrNegativeLine)),
          "negative field's attributes must be listed first"
        );
      }
    );
  }
}
