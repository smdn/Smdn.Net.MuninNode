// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
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
  [Test]
  [CancelAfter(3000)]
  public async Task HandleCommandAsync_ConfigCommand_UnknownService(CancellationToken cancellationToken)
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

  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_ConfigCommand()
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
        (IPluginGraphAttributes)graphAttrs,
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
        (IPluginGraphAttributes)graphAttrs
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

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_ConfigCommand))]
  [CancelAfter(3000)]
  public Task HandleCommandAsync_ConfigCommand(
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

  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_ConfigCommand_OptionalAttributes()
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
        (IPluginGraphAttributes)graphAttrs,
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
        (IPluginGraphAttributes)graphAttrs,
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

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_ConfigCommand_OptionalAttributes))]
  [CancelAfter(3000)]
  public Task HandleCommandAsync_ConfigCommand_OptionalAttributes(
    IPlugin plugin,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: new[] { plugin },
      getCommand: () => $"config {plugin.Name}",
      assertResponseLines: assertResponseLines,
      cancellationToken: TestContext.CurrentContext.CancellationToken
    );

  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_ConfigCommand_GraphOrder()
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
        (IPluginGraphAttributes)graphAttrs,
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

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_ConfigCommand_GraphOrder))]
  [CancelAfter(3000)]
  public Task HandleCommandAsync_ConfigCommand_GraphOrder(
    IPlugin plugin,
    Action<IReadOnlyList<string>> assertResponseLines
  )
    => GetCommandResponseAsync(
      plugins: new[] { plugin },
      getCommand: () => $"config {plugin.Name}",
      assertResponseLines: assertResponseLines,
      cancellationToken: TestContext.CurrentContext.CancellationToken
    );

  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_ConfigCommand_GraphTotal()
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
        (IPluginGraphAttributes)graphAttrs,
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

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_ConfigCommand_GraphTotal))]
  [CancelAfter(3000)]
  public Task HandleCommandAsync_ConfigCommand_GraphTotal(
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
  public async Task HandleCommandAsync_ConfigCommand_TranslateGraphStyle(
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
        (IPluginGraphAttributes)graphAttrs
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

  private static System.Collections.IEnumerable YieldTestCases_HandleCommandAsync_ConfigCommand_WarningAndCriticalField()
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

  [TestCaseSource(nameof(YieldTestCases_HandleCommandAsync_ConfigCommand_WarningAndCriticalField))]
  [CancelAfter(3000)]
  public async Task HandleCommandAsync_ConfigCommand_WarningAndCriticalField(
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
        (IPluginGraphAttributes)graphAttrs,
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
  public async Task HandleCommandAsync_ConfigCommand_NegativeField(CancellationToken cancellationToken)
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
        (IPluginGraphAttributes)graphAttrs,
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

  [Test]
  public void HandleCommandAsync_ConfigCommand_DirtyConfig(
    [Values] bool enableDirtyConfig
  )
  {
    const string PluginName = "plugin";
    const string GraphTitle = "title";
    const string FieldLabel = "field";
    const string ExpectedFieldValue = "0";

    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        PluginProvider = new PluginProvider([
          PluginFactory.CreatePlugin(
            name: PluginName,
            fieldLabel: "field",
            fetchFieldValue: static () => 0.0,
            graphAttributes: new PluginGraphAttributesBuilder(title: GraphTitle).Build()
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

    // config command
    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence($"config {PluginName}")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));

    Assert.That(client.Responses[0], Does.Contain($"graph_title {GraphTitle}\n"));
    Assert.That(client.Responses[0], Does.Contain($"{FieldLabel}.label {FieldLabel}\n"));

    if (enableDirtyConfig)
      Assert.That(client.Responses[0], Does.Contain($"{FieldLabel}.value {ExpectedFieldValue}\n"));
    else
      Assert.That(client.Responses[0], Does.Not.Contain($"{FieldLabel}.value "));

    Assert.That(client.Responses[0], Does.EndWith("\n.\n"));
    Assert.That(client.Responses[0].TrimEnd(), Does.Not.Contain("\n.\n"));
  }
}
