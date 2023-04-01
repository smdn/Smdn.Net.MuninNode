// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

[TestFixture]
public class NodeBaseTests {
  private static NodeBase CreateNode(out IPEndPoint endPoint)
    => CreateNode(plugins: Array.Empty<Plugin>(), out endPoint);

  private static NodeBase CreateNode(IReadOnlyList<Plugin> plugins, out IPEndPoint endPoint)
  {
    var node = PortNumberUtils.CreateServiceWithAvailablePort(
      createService: port => new LocalNode(plugins, port),
      isPortInUseException: static ex => ex is SocketException
    );

    endPoint = node.LocalEndPoint;

    return node;
  }

  private static TcpClient CreateClient(
    IPEndPoint endPoint,
    out StreamWriter writer,
    out StreamReader reader
  )
  {
    var client = new TcpClient();

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
    => StartSession(plugins: Array.Empty<Plugin>(), action: action);

  private static async Task StartSession(
    IReadOnlyList<Plugin> plugins,
    Func<NodeBase, TcpClient, StreamWriter, StreamReader, CancellationToken, Task> action
  )
  {
    using var node = CreateNode(plugins, out var endPoint);

    node.Start();

    using var cts = new CancellationTokenSource(
      delay: TimeSpan.FromSeconds(5) // timeout for hung up
    );

    var taskAccept = Task.Run(
      async () => await node.AcceptClientAsync(),
      cts.Token
    );

    using var client = CreateClient(endPoint, out var writer, out var reader);

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
  public void Start()
  {
    using var node = CreateNode(out _);

    Assert.DoesNotThrow(() => node.Start());
    Assert.Throws<InvalidOperationException>(() => node.Start(), "already started");
  }

  [Test]
  public void AcceptClientAsync()
  {
    using var node = CreateNode(out var endPoint);

    node.Start();

    var taskAccept = Task.Run(async () => await node.AcceptClientAsync());

    using var client = CreateClient(endPoint, out var writer, out var reader);

    var banner = reader.ReadLine();

    Assert.AreEqual($"# munin node at {node.HostName}", banner, nameof(banner));

    writer.WriteLine(".");
    writer.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [Test]
  public void AcceptClientAsync_NodeNotStarted()
  {
    using var node = CreateNode(out var endPoint);

    Assert.ThrowsAsync<InvalidOperationException>(async () => await node.AcceptClientAsync());
  }

  [TestCase(0)]
  [TestCase(1)]
  [TestCase(1000)]
  public void AcceptClientAsync_CancellationRequested(int delayMilliseconds)
  {
    using var node = CreateNode(out var endPoint);

    node.Start();

    using var cts = new CancellationTokenSource(millisecondsDelay: delayMilliseconds);

    var ex = Assert.CatchAsync(async () => await node.AcceptClientAsync(cts.Token));

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public void AcceptClientAsync_ClientDisconnected_BeforeSendingBanner()
  {
    using var node = CreateNode(out var endPoint);

    node.Start();

    var taskAccept = Task.Run(async () => await node.AcceptClientAsync());

    using var client = CreateClient(endPoint, out _, out _);

    client.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [Test]
  public void AcceptClientAsync_ClientDisconnected_WhileAwaitingCommand()
  {
    using var node = CreateNode(out var endPoint);

    node.Start();

    var taskAccept = Task.Run(async () => await node.AcceptClientAsync());

    using var client = CreateClient(endPoint, out _, out var reader);

    reader.ReadLine(); // read banner

    client.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [TestCase("\r\n")]
  [TestCase("\n")]
  public void ProcessCommandAsync_EndOfLine(string eol)
  {
    using var node = CreateNode(out var endPoint);

    node.Start();

    var taskAccept = Task.Run(async () => await node.AcceptClientAsync());

    using var client = CreateClient(endPoint, out var writer, out _);

    writer.Write(".");
    writer.Write(eol);
    writer.Close();

    Assert.DoesNotThrowAsync(async () => await taskAccept);
  }

  [Test]
  public async Task ProcessCommandAsync_UnknownCommand()
  {
    const string unknownCommand = "unknown";

    await StartSession(async static (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync(unknownCommand, cancellationToken);
      await writer.FlushAsync(cancellationToken);

      Assert.AreEqual(
        "# Unknown command. Try cap, list, nodes, config, fetch, version or quit",
        await reader.ReadLineAsync(cancellationToken),
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

      Assert.AreEqual(node.HostName, await reader.ReadLineAsync(cancellationToken), "line #1");
      Assert.AreEqual(".", await reader.ReadLineAsync(cancellationToken), "line #2");
    });
  }

  [TestCase(".")]
  [TestCase("quit")]
  public async Task ProcessCommandAsync_QuitCommand(string command)
  {
    await StartSession(async (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync(command, cancellationToken);
      await writer.FlushAsync(cancellationToken);

      Assert.AreEqual(0, client.Available);

      var ex = Assert.Throws<IOException>(() => reader.ReadLine());

      Assert.IsInstanceOf<SocketException>(ex!.InnerException);

      Assert.IsFalse(client.Connected, nameof(client.Connected));
    });
  }

  [Test]
  public async Task ProcessCommandAsync_CapCommand()
  {
    await StartSession(async static (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync("cap", cancellationToken);
      await writer.FlushAsync(cancellationToken);

      Assert.AreEqual("cap", await reader.ReadLineAsync(cancellationToken), "line #1");
    });
  }

  [Test]
  public async Task ProcessCommandAsync_VersionCommand()
  {
    await StartSession(async static (node, client, writer, reader, cancellationToken) => {
      await writer.WriteLineAsync("version", cancellationToken);
      await writer.FlushAsync(cancellationToken);

      var line = await reader.ReadLineAsync(cancellationToken);

      StringAssert.Contains(node.HostName, line, "line #1 must contain hostname");
      StringAssert.Contains(node.NodeVersion.ToString(), line, "line #1 must contain node version");
    });
  }

  [Test]
  public async Task ProcessCommandAsync_ListCommand()
  {
    var graphConfig = new PluginGraphConfiguration(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args",
      updateRate: TimeSpan.FromMinutes(1)
    );

    var plugins = new[] {
      new Plugin(
        "plugin1",
        graphConfig,
        PluginFieldConfiguration.Null
      ),
      new Plugin(
        "plugin2",
        graphConfig,
        PluginFieldConfiguration.Null
      ),
    };

    await StartSession(
      plugins: plugins,
      action: async static (node, client, writer, reader, cancellationToken) => {
        await writer.WriteLineAsync("list", cancellationToken);
        await writer.FlushAsync(cancellationToken);

        Assert.AreEqual("plugin1 plugin2", await reader.ReadLineAsync(cancellationToken), "line #1");
      }
    );
  }

  private class StaticPluginFieldConfiguration : PluginFieldConfiguration {
    private readonly IEnumerable<(string, double)> fields;

    public StaticPluginFieldConfiguration(string? graphStyle, IEnumerable<(string, double)> fields)
      : base(defaultGraphStyle: graphStyle ?? "AREA")
    {
      this.fields = fields;
    }

    public override IEnumerable<PluginField> FetchFields()
    {
      foreach (var (label, value) in fields) {
        yield return new(label: label, value: value);
      }
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_ProcessCommandAsync_FetchCommand()
  {
    var graphConfig = new PluginGraphConfiguration(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args",
      updateRate: TimeSpan.FromMinutes(1)
    );

    var plugins = new[] {
      new Plugin(
        "plugin1",
        graphConfig,
        new StaticPluginFieldConfiguration(
          graphStyle: null,
          new[] {
            ("plugin1field1", 1.1),
            ("plugin1field2", 1.2)
          }
        )
      ),
      new Plugin(
        "plugin2",
        graphConfig,
        new StaticPluginFieldConfiguration(
          graphStyle: null,
          new[] {
            ("plugin2field1", 2.1)
          }
        )
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
  public async Task ProcessCommandAsync_FetchCommand(
    IReadOnlyList<Plugin> plugins,
    string command,
    string[] expectedResponseLines
  )
  {
    await StartSession(
      plugins: plugins,
      action: async (node, client, writer, reader, cancellationToken) => {
        await writer.WriteLineAsync(command, cancellationToken);
        await writer.FlushAsync(cancellationToken);

        var totalLineCount = 0;

        foreach (var (expectedResponseLine, lineNumber) in expectedResponseLines.Select(static (line, index) => (line, index))) {
          Assert.AreEqual(expectedResponseLine, await reader.ReadLineAsync(cancellationToken), $"line #{lineNumber}");
          totalLineCount++;
        }

        Assert.AreEqual(expectedResponseLines.Length, totalLineCount, nameof(totalLineCount));
      }
    );
  }

  [Test]
  public async Task ProcessCommandAsync_ConfigCommand_UnknownService()
  {
    var graphConfig = new PluginGraphConfiguration(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args",
      updateRate: TimeSpan.FromMinutes(1)
    );

    var plugins = new[] {
      new Plugin(
        "plugin",
        graphConfig,
        PluginFieldConfiguration.Null
      ),
    };

    await StartSession(
      plugins: plugins,
      action: async static (node, client, writer, reader, cancellationToken) => {
        await writer.WriteLineAsync("config nonexistentplugin", cancellationToken);
        await writer.FlushAsync(cancellationToken);

        Assert.AreEqual("# Unknown service", await reader.ReadLineAsync(cancellationToken), "line #1");
        Assert.AreEqual(".", await reader.ReadLineAsync(cancellationToken), "line #2");
      }
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ProcessCommandAsync_ConfigCommand()
  {
    var graphConfig = new PluginGraphConfiguration(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args",
      updateRate: TimeSpan.FromMinutes(1)
    );

    var plugins = new[] {
      new Plugin(
        "plugin1",
        graphConfig,
        new StaticPluginFieldConfiguration(
          graphStyle: "AREA",
          new[] {
            ("plugin1field1", 1.1),
            ("plugin1field2", 1.2)
          }
        )
      ),
      new Plugin(
        "plugin2",
        graphConfig,
        new StaticPluginFieldConfiguration(
          graphStyle: "LINE",
          new[] {
            ("plugin2field1", 2.1)
          }
        )
      ),
    };

    yield return new object[] {
      plugins,
      "config plugin1",
      new Action<IReadOnlyList<string>>(
        responseLines => {
          AssertGraphConfigurations(responseLines, plugins[0]);

          // plugin1field1
          CollectionAssert.Contains(responseLines, $"plugin1field1.label plugin1field1", "plugin1field1.label");
          CollectionAssert.Contains(responseLines, $"plugin1field1.draw AREA", "plugin1field1.draw");

          // plugin1field2
          CollectionAssert.Contains(responseLines, $"plugin1field2.label plugin1field2", "plugin1field2.label");
          CollectionAssert.Contains(responseLines, $"plugin1field2.draw AREA", "plugin1field2.draw");
        }
      )
    };

    yield return new object[] {
      plugins,
      "config plugin2",
      new Action<IReadOnlyList<string>>(
        responseLines => {
          AssertGraphConfigurations(responseLines, plugins[1]);

          // plugin2field1
          CollectionAssert.Contains(responseLines, $"plugin2field1.label plugin2field1", "plugin2field1.label");
          CollectionAssert.Contains(responseLines, $"plugin2field1.draw LINE", "plugin2field1.draw");
        }
      )
    };

    static void AssertGraphConfigurations(
      IReadOnlyList<string> responseLines,
      Plugin expectedPlugin
    )
    {
      var graph = expectedPlugin.GraphConfiguration;
      var scaleString = graph.Scale ? "yes" : "no";

      CollectionAssert.Contains(responseLines, $"graph_title {graph.Title}", "graph_title");
      CollectionAssert.Contains(responseLines, $"graph_category {graph.Category}", "graph_category");
      CollectionAssert.Contains(responseLines, $"graph_args {graph.Arguments}", "graph_args");
      CollectionAssert.Contains(responseLines, $"graph_scale {scaleString}", "graph_scale");
      CollectionAssert.Contains(responseLines, $"graph_vlabel {graph.VerticalLabel}", "graph_vlabel");
      CollectionAssert.Contains(responseLines, $"update_rate {(int)graph.UpdateRate.TotalSeconds}", "update_rate");
    }
  }

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_ConfigCommand))]
  public async Task ProcessCommandAsync_ConfigCommand(
    IReadOnlyList<Plugin> plugins,
    string command,
    Action<IReadOnlyList<string>> assertResponseLines
  )
  {
    await StartSession(
      plugins: plugins,
      action: async (node, client, writer, reader, cancellationToken) => {
        await writer.WriteLineAsync(command, cancellationToken);
        await writer.FlushAsync(cancellationToken);

        var lines = new List<string>();

        for (; ;) {
          var line = await reader.ReadLineAsync(cancellationToken);

          if (line is null)
            break;

          lines.Add(line);

          if (line == ".")
            break;
        }

        assertResponseLines(lines);
      }
    );
  }
}
