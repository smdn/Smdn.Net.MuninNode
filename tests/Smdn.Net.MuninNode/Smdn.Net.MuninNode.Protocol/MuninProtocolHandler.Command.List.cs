// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using NUnit.Framework;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.Protocol;

#pragma warning disable IDE0040
partial class MuninProtocolHandlerTests {
#pragma warning restore IDE0040
  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_ListCommand))]
  public void HandleCommandAsync_ListCommand(
    MuninNodeProfile profile,
    string expectedResponseLine
  )
  {
    var handler = new MuninProtocolHandler(
      profile: profile
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence("list")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo(expectedResponseLine)
    );
  }

  [Test]
  public void HandleCommandAsync_ListCommand_Multigraph(
    [Values] bool enableMultigraph
  )
  {
    const string NonMultigraphPluginName = "plugin";
    const string MultigraphPluginName = "multigraph";
    const string MultigraphSubPluginName = "multigraph_sub";

    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        PluginProvider = new PluginProvider([
          PluginFactory.CreatePlugin(
            name: NonMultigraphPluginName,
            fieldLabel: "field",
            fetchFieldValue: static () => 0.0,
            graphAttributes: new PluginGraphAttributesBuilder(title: "title").Build()
          ),
          new MultigraphPlugin(
            name: MultigraphPluginName,
            plugins: [
              PluginFactory.CreatePlugin(
                name: MultigraphSubPluginName,
                fieldLabel: "field",
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

    // list command
    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence("list")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      enableMultigraph
        ? Is.EqualTo($"{NonMultigraphPluginName} {MultigraphPluginName}\n")
        : Is.EqualTo($"{NonMultigraphPluginName} {MultigraphSubPluginName}\n")
    );
  }
}
