// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.Protocol;

#pragma warning disable IDE0040
partial class MuninProtocolHandlerTests {
#pragma warning restore IDE0040
  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_FetchCommand()
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
}
