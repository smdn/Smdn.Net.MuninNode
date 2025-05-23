// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.Protocol;

#pragma warning disable IDE0040
partial class MuninProtocolHandlerTests {
#pragma warning restore IDE0040
  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_FetchCommand()
  {
    var graphAttrs = new PluginGraphAttributesBuilder(title: "title").Build();

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

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_FetchCommand))]
  [SetCulture("")]
  [CancelAfter(3000)]
  public Task HandleCommandAsync_FetchCommand_InvariantCulture(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => HandleCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_FetchCommand))]
  [SetCulture("ja_JP")]
  [CancelAfter(3000)]
  public Task HandleCommandAsync_FetchCommand_JA_JP(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => HandleCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_FetchCommand))]
  [SetCulture("fr_CH")]
  [CancelAfter(3000)]
  public Task HandleCommandAsync_FetchCommand_FR_CH(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => HandleCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_FetchCommand))]
  [SetCulture("ar_AE")]
  [CancelAfter(3000)]
  public Task HandleCommandAsync_FetchCommand_AR_AE(
    IReadOnlyList<IPlugin> plugins,
    string command,
    string[] expectedResponseLines
  )
    => HandleCommandAsync_FetchCommand(plugins, command, expectedResponseLines);

  private Task HandleCommandAsync_FetchCommand(
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
  public void HandleCommandAsync_FetchCommand_DirtyConfig(
    [Values] bool enableDirtyConfig
  )
  {
    const string PluginName = "plugin";
    const string FieldLabel = "field";
    const string ExpectedFieldValue = "0";

    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        PluginProvider = new PluginProvider([
          PluginFactory.CreatePlugin(
            name: PluginName,
            fieldLabel: "field",
            fetchFieldValue: static () => 0.0,
            graphAttributes: new PluginGraphAttributesBuilder(title: "title").Build()
          )
        ])
      }
    );

    var client = new PseudoMuninNodeClient();

    // cap command
    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence(enableDirtyConfig ? "cap dirtyconfig" : "cap")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo(enableDirtyConfig ? "cap dirtyconfig\n" : "cap\n")
    );

    client.ClearResponses();

    // fetch command
    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence($"fetch {PluginName}")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Does.Contain($"{FieldLabel}.value {ExpectedFieldValue}\n"),
      "independent of dirtyconfig status"
    );

    Assert.That(client.Responses[0], Does.EndWith("\n.\n"));
    Assert.That(client.Responses[0].TrimEnd(), Does.Not.Contain("\n.\n"));
  }

  [Test]
  public void HandleCommandAsync_FetchCommand_Multigraph(
    [Values] bool enableMultigraph
  )
  {
    const string NonMultigraphPluginName = "nonmultigraph";
    const string NonMultigraphFieldLabel = "nonmultigraph_field";
    const string MultigraphPluginName = "multigraph";
    const string MultigraphSubPluginName = "multigraph_sub";
    const string MultigraphFieldLabel = "multigraph_field";
    const string ExpectedFieldValue = "0";

    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        PluginProvider = new PluginProvider([
          PluginFactory.CreatePlugin(
            name: NonMultigraphPluginName,
            fieldLabel: NonMultigraphFieldLabel,
            fetchFieldValue: static () => 0.0,
            graphAttributes: new PluginGraphAttributesBuilder(title: "title").Build()
          ),
          new MultigraphPlugin(
            name: MultigraphPluginName,
            plugins: [
              PluginFactory.CreatePlugin(
                name: MultigraphSubPluginName,
                fieldLabel: MultigraphFieldLabel,
                fetchFieldValue: static () => 0.0,
                graphAttributes: new PluginGraphAttributesBuilder(title: "title").Build()
              )
            ]
          )
        ])
      }
    );

    var client = new PseudoMuninNodeClient();

    // cap command
    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence(enableMultigraph ? "cap multigraph" : "cap")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo(enableMultigraph ? "cap multigraph\n" : "cap\n")
    );

    client.ClearResponses();

    // fetch command (against the multigraph plugin)
    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence($"fetch {(enableMultigraph ? MultigraphPluginName : MultigraphSubPluginName)}")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      enableMultigraph
        ? Does.Contain($"multigraph {MultigraphSubPluginName}\n{MultigraphFieldLabel}.value {ExpectedFieldValue}\n")
        : Does.Contain($"{MultigraphFieldLabel}.value {ExpectedFieldValue}\n")
    );

    client.ClearResponses();

    // fetch command (against the plugin that is multigraph but not listed)
    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence($"fetch {(enableMultigraph ? MultigraphSubPluginName : MultigraphPluginName)}")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo("# Unknown service\n.\n")
    );

    client.ClearResponses();

    // fetch command (against the non-multigraph plugin)
    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence($"fetch {NonMultigraphPluginName}")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Does.Contain($"{NonMultigraphFieldLabel}.value {ExpectedFieldValue}\n")
    );
  }
}
