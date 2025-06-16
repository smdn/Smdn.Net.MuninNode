// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.Protocol;

#pragma warning disable IDE0040
partial class MuninProtocolHandlerTests {
#pragma warning restore IDE0040
  private static readonly char[] NewLineChars = { '\r', '\n' };

  private static async Task GetCommandResponseAsync(
    IReadOnlyList<IPlugin> plugins,
    Func<string> getCommand,
    Action<IReadOnlyList<string>> assertResponseLines,
    CancellationToken cancellationToken
  )
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        PluginProvider = new PluginProvider(plugins),
      }
    );
    var client = new PseudoMuninNodeClient();

    await handler.HandleCommandAsync(
      client,
      CreateCommandLineSequence(getCommand()),
      cancellationToken
    );

    if (client.Responses.Count != 1)
      throw new InvalidOperationException("unexpected number of response");

    assertResponseLines(
      client.Responses[0].TrimEnd().Split(NewLineChars, StringSplitOptions.None)
    );
  }

  [TestCase("")]
  [TestCase(".quit")] // not to be confused with `.`
  [TestCase("ca")] // not to be confused with `cap`
  [TestCase("capa")] // not to be confused with `cap`
  [TestCase("unknown")]
  public void HandleCommandAsync_UnknownCommand(string command)
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleCommandAsync(client, CreateCommandLineSequence(command)),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo("# Unknown command. Try cap, list, nodes, config, fetch, version or quit\n")
    );
  }

  [TestCase("munin-node.localhost")]
  [TestCase("_")]
  public void HandleCommandAsync_NodesCommand(string hostName)
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = hostName,
      }
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence("nodes")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo($"{hostName}\n.\n")
    );
  }

  [TestCase(".")]
  [TestCase("quit")]
  public void HandleCommandAsync_QuitCommand(string command)
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence(command)
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.Zero);
    Assert.That(client.Connected, Is.False);
  }

  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_VersionCommand()
  {
    foreach (var hostName in new[] { "munin-node.localhost", "_" }) {
      foreach (var versionString in new[] { "1.0.0.0", "1.2.3+build.123", "not-a-semver-string" }) {
        yield return new object[] {
          new MuninNodeProfile() {
            HostName = hostName,
            Version = versionString
          },
          hostName,
          versionString
        };
      }
    }
  }

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_VersionCommand))]
  public void HandleCommandAsync_VersionCommand(
    MuninNodeProfile profile,
    string expectedHostName,
    string expectedVersionString
  )
  {
    var handler = new MuninProtocolHandler(
      profile: profile
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence("version")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo($"munins node on {expectedHostName} version: {expectedVersionString}\n")
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_ListCommand()
  {
    static MuninNodeProfile CreateProfile(IReadOnlyCollection<IPlugin> plugins)
      => new() {
        PluginProvider = new PluginProvider(plugins)
      };

    var graphAttrs = new PluginGraphAttributes(
      title: "title",
      category: "test",
      verticalLabel: "test",
      scale: false,
      arguments: "--args"
    );

    yield return new object[] {
      CreateProfile([]),
      "\n"
    };

    yield return new object[] {
      CreateProfile([
        new Plugin("plugin", graphAttrs, Array.Empty<IPluginField>()),
      ]),
      "plugin\n"
    };

    yield return new object[] {
      CreateProfile([
        new Plugin("plugin1", graphAttrs, Array.Empty<IPluginField>()),
        new Plugin("plugin2", graphAttrs, Array.Empty<IPluginField>()),
      ]),
      "plugin1 plugin2\n"
    };
  }
}
