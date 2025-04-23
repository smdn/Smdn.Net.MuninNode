// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Smdn.Net.MuninPlugin;

using Smdn.Net.MuninNode.Transport;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class NodeBaseTests {
#pragma warning restore IDE0040
  private class PseudoMuninNode : NodeBase {
    public override string HostName => nameof(PseudoMuninNode);
    public override IPluginProvider PluginProvider { get; }

    public PseudoMuninNode(IAccessRule? accessRule, IReadOnlyCollection<IPlugin>? plugins)
      : base(
        serverFactory: new PseudoMuninNodeServerFactory(),
        accessRule: accessRule,
        logger: null
      )
    {
      PluginProvider = new ReadOnlyCollectionPluginProvider(plugins ?? Array.Empty<IPlugin>());
    }

    private class ReadOnlyCollectionPluginProvider : IPluginProvider {
      public IReadOnlyCollection<IPlugin> Plugins { get; }
      public INodeSessionCallback? SessionCallback => null;

      public ReadOnlyCollectionPluginProvider(IReadOnlyCollection<IPlugin> plugins)
      {
        Plugins = plugins;
      }
    }

    public (
      PseudoMuninNodeClient Client,
      TextWriter ClientRequestWriter,
      TextReader ServerResponseReader
    )
    GetAcceptingClient(CancellationToken cancellationToken)
    {
      if (Server is null)
        throw new InvalidOperationException("not yet started");
      if (Server is not PseudoMuninNodeServer pseudoServer)
        throw new InvalidOperationException("server type mismatch");

      return pseudoServer.GetAcceptingClient(cancellationToken);
    }
  }

  private static Task RunSessionAsync(
    Func<PseudoMuninNode, PseudoMuninNodeClient, TextWriter, TextReader, CancellationToken, Task> action,
    CancellationToken cancellationToken = default
  )
    => RunSessionAsync(
      accessRule: null,
      plugins: null,
      action: action,
      cancellationToken: cancellationToken
    );

  private static Task RunSessionAsync(
    IReadOnlyList<IPlugin>? plugins,
    Func<PseudoMuninNode, PseudoMuninNodeClient, TextWriter, TextReader, CancellationToken, Task> action,
    CancellationToken cancellationToken = default
  )
    => RunSessionAsync(
      accessRule: null,
      plugins: plugins,
      action: action,
      cancellationToken: cancellationToken
    );

  private static async Task RunSessionAsync(
    IAccessRule? accessRule,
    IReadOnlyList<IPlugin>? plugins,
    Func<PseudoMuninNode, PseudoMuninNodeClient, TextWriter, TextReader, CancellationToken, Task> action,
    CancellationToken cancellationToken = default
  )
  {
    await using var node = new PseudoMuninNode(accessRule, plugins);

    await node.StartAsync(cancellationToken);

    var taskAccept = Task.Run(
      async () => await node.AcceptSingleSessionAsync(cancellationToken),
      cancellationToken
    );

    var (c, requestWriter, responseReader) = node.GetAcceptingClient(cancellationToken);
    using var client = c;

    try {
      Assert.That(
        async () => await responseReader.ReadLineAsync(cancellationToken), // receive banner
        Contains.Substring(node.HostName)
      );

      try {
        await action(node, client, requestWriter, responseReader, cancellationToken);
      }
      finally {
        await client.DisconnectAsync(cancellationToken);
      }
    }
    finally {
      await taskAccept;
    }
  }

  [TestCase("\r\n")]
  [TestCase("\n")]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_EndOfLine(string eol, CancellationToken cancellationToken)
  {
    PseudoMuninNodeClient? acceptedClient = null;

    await RunSessionAsync(
      accessRule: null,
      plugins: null,
      async (node, client, writer, reader, ct) => {
        Assert.That(client.IsConnected, Is.True);

        await writer.WriteAsync(("." + eol).AsMemory(), ct);

        acceptedClient = client;
      },
      cancellationToken: cancellationToken
    );

    Assert.That(acceptedClient, Is.Not.Null);
    Assert.That(acceptedClient!.IsConnected, Is.False);
  }

  [Test]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_UnknownCommand(CancellationToken cancellationToken)
  {
    const string UnknownCommand = "unknown";

    await RunSessionAsync(async static (node, client, writer, reader, ct) => {
      await writer.WriteLineAsync(UnknownCommand, ct);
      await writer.FlushAsync(ct);

      Assert.That(
        await reader.ReadLineAsync(ct),
        Is.EqualTo("# Unknown command. Try cap, list, nodes, config, fetch, version or quit"),
        "line #1"
      );
    }, cancellationToken);
  }

  [Test]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_NodesCommand(CancellationToken cancellationToken)
  {
    await RunSessionAsync(async static (node, client, writer, reader, ct) => {
      await writer.WriteLineAsync("nodes", ct);
      await writer.FlushAsync(ct);

      Assert.That(await reader.ReadLineAsync(ct), Is.EqualTo(node.HostName), "line #1");
      Assert.That(await reader.ReadLineAsync(ct), Is.EqualTo("."), "line #2");
    }, cancellationToken);
  }

  [TestCase(".")]
  [TestCase("quit")]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_QuitCommand(string command, CancellationToken cancellationToken)
  {
    PseudoMuninNodeClient? acceptedClient = null;

    await RunSessionAsync(async (node, client, writer, reader, ct) => {
      acceptedClient = client;

      Assert.That(client.IsConnected, Is.True);

      await writer.WriteLineAsync(command, ct);
      await writer.FlushAsync(ct);

      Assert.That(await reader.ReadLineAsync(ct), Is.Null);
    }, cancellationToken);

    Assert.That(acceptedClient, Is.Not.Null);
    Assert.That(acceptedClient!.IsConnected, Is.False);
  }

  [Test]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_CapCommand(CancellationToken cancellationToken)
  {
    await RunSessionAsync(async static (node, client, writer, reader, ct) => {
      await writer.WriteLineAsync("cap", ct);
      await writer.FlushAsync(ct);

      Assert.That(await reader.ReadLineAsync(ct), Is.EqualTo("cap"), "line #1");
    }, cancellationToken);
  }

  [Test]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_VersionCommand(CancellationToken cancellationToken)
  {
    await RunSessionAsync(async static (node, client, writer, reader, ct) => {
      await writer.WriteLineAsync("version", ct);
      await writer.FlushAsync(ct);

      var line = await reader.ReadLineAsync(ct);

      Assert.That(line, Does.Contain(node.HostName), "line #1 must contain hostname");
      Assert.That(line, Does.Contain(node.NodeVersion.ToString()), "line #1 must contain node version");
    }, cancellationToken);
  }

  [Test]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_ListCommand(CancellationToken cancellationToken)
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

    await RunSessionAsync(
      plugins: plugins,
      action: async static (node, client, writer, reader, ct) => {
        await writer.WriteLineAsync("list", ct);
        await writer.FlushAsync(ct);

        Assert.That(await reader.ReadLineAsync(ct), Is.EqualTo("plugin1 plugin2"), "line #1");
      },
      cancellationToken
    );
  }

  private static Task GetCommandResponseAsync(
    IReadOnlyList<IPlugin> plugins,
    Func<string> getCommand,
    Action<IReadOnlyList<string>> assertResponseLines,
    CancellationToken cancellationToken
  )
    => RunSessionAsync(
      plugins: plugins,
      action: async (node, client, writer, reader, ct) => {
        await writer.WriteLineAsync(getCommand(), ct);
        await writer.FlushAsync(ct);

        var lines = new List<string>();

        for (; ; ) {
          var line = await reader.ReadLineAsync(ct);

          if (line is null)
            break;

          lines.Add(line);

          if (line == ".")
            break;
        }

        assertResponseLines(lines);
      },
      cancellationToken
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
  [CancelAfter(3000)]
  public Task ProcessCommandAsync_FetchCommand_InvariantCulture(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => ProcessCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_FetchCommand))]
  [SetCulture("ja_JP")]
  [CancelAfter(3000)]
  public Task ProcessCommandAsync_FetchCommand_JA_JP(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => ProcessCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_FetchCommand))]
  [SetCulture("fr_CH")]
  [CancelAfter(3000)]
  public Task ProcessCommandAsync_FetchCommand_FR_CH(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => ProcessCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_ProcessCommandAsync_FetchCommand))]
  [SetCulture("ar_AE")]
  [CancelAfter(3000)]
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
      },
      cancellationToken: TestContext.CurrentContext.CancellationToken
    );

  [Test]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_ConfigCommand_UnknownService(CancellationToken cancellationToken)
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
      },
      cancellationToken: cancellationToken
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
  [CancelAfter(3000)]
  public Task ProcessCommandAsync_ConfigCommand(
    IReadOnlyList<IPlugin> plugins,
    string command,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: plugins,
      getCommand: () => command,
      assertResponseLines: assertResponseLines,
      cancellationToken: TestContext.CurrentContext.CancellationToken
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
  [CancelAfter(3000)]
  public Task ProcessCommandAsync_ConfigCommand_OptionalAttributes(
    IPlugin plugin,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: new[] { plugin },
      getCommand: () => $"config {plugin.Name}",
      assertResponseLines: assertResponseLines,
      cancellationToken: TestContext.CurrentContext.CancellationToken
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
  [CancelAfter(3000)]
  public Task ProcessCommandAsync_ConfigCommand_GraphOrder(
    IPlugin plugin,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: new[] { plugin },
      getCommand: () => $"config {plugin.Name}",
      assertResponseLines: assertResponseLines,
      cancellationToken: TestContext.CurrentContext.CancellationToken
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
  [CancelAfter(3000)]
  public Task ProcessCommandAsync_ConfigCommand_GraphTotal(
    IPlugin plugin,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: new[] { plugin },
      getCommand: () => $"config {plugin.Name}",
      assertResponseLines: assertResponseLines,
      cancellationToken: TestContext.CurrentContext.CancellationToken
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
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_ConfigCommand_TranslateGraphStyle(
    PluginFieldGraphStyle style,
    string? expectedFieldDrawAttribute,
    Type? expectedExceptionType,
    CancellationToken cancellationToken
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
        },
        cancellationToken: cancellationToken
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
  [CancelAfter(3000)]
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
      },
      cancellationToken: TestContext.CurrentContext.CancellationToken
    );
  }

  [TestCase]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_ConfigCommand_NegativeField(CancellationToken cancellationToken)
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
      },
      cancellationToken: cancellationToken
    );
  }
}
