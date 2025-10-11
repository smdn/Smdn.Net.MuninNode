// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Smdn.Net.MuninPlugin;

[TestFixture]
public partial class PluginGraphAttributesBuilderTests {
  private static System.Collections.IEnumerable YieldTestCases_WithTitle_ArgumentException()
  {
    yield return new object?[] { null, typeof(ArgumentNullException) };
    yield return new object?[] { "", typeof(ArgumentException) };
    yield return new object?[] { " ", typeof(ArgumentException) };
    yield return new object?[] { "\0", typeof(ArgumentException) };
    yield return new object?[] { "\0title", typeof(ArgumentException) };
    yield return new object?[] { "\ntitle", typeof(ArgumentException) };
    yield return new object?[] { "\rtitle", typeof(ArgumentException) };
    yield return new object?[] { "\ttitle", typeof(ArgumentException) };
    yield return new object?[] { "title\0", typeof(ArgumentException) };
    yield return new object?[] { "title\n", typeof(ArgumentException) };
    yield return new object?[] { "title\r", typeof(ArgumentException) };
    yield return new object?[] { "title\t", typeof(ArgumentException) };
  }

  private static System.Collections.IEnumerable YieldTestCases_WithTitle()
  {
    yield return new object[] { "t" };
    yield return new object[] { "title" };
    yield return new object[] { "Title" };
    yield return new object[] { "TITLE" };
    yield return new object[] { "title0" };
    yield return new object[] { "title9" };
    yield return new object[] { "0title" };
    yield return new object[] { "9title" };
    yield return new object[] { "title-x" };
    yield return new object[] { ".title." };
    yield return new object[] { "'title'" };
    yield return new object[] { "title - title" };
    yield return new object[] { "<title>" };
    yield return new object[] { "タイトル" };
  }

  [TestCaseSource(nameof(YieldTestCases_WithTitle_ArgumentException))]
  public void Ctor_ArgumentException(string? title, Type expectedTypeOfException)
    => Assert.That(
      () => new PluginGraphAttributesBuilder(
        title: title!
      ),
      Throws
        .TypeOf(expectedTypeOfException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("title")
    );

  private static void AssertBuiltGraphAttributes(
    PluginGraphAttributesBuilder builder,
    IEnumerable<string> expectedAttributeList
  )
    => Assert.That(
      builder.Build().EnumerateAttributes(),
      Is.EquivalentTo(expectedAttributeList).Using((IEqualityComparer<string>)StringComparer.Ordinal)
    );

  [TestCaseSource(nameof(YieldTestCases_WithTitle))]
  public void Ctor(string title)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder(title),
      [$"graph_title {title}"]
    );

  [Test]
  public void Ctor_WithBaseBuilder()
  {
    var baseBuilder = new PluginGraphAttributesBuilder("title")
      .ShowGraph()
      .WithCategory("category")
      .WithHeight(42)
      .WithFieldOrder(["a", "b", "c"])
      .WithFormatString("%e")
      .EnableUnitScaling()
      .WithTotal("total")
      .WithVerticalLabel("vlabel")
      .WithWidth(42)
      .WithUpdateRate(TimeSpan.FromSeconds(30))
      .AddGraphArgument("--arg0")
      .AddGraphArgument("--arg1");
    var builderSameTitle = new PluginGraphAttributesBuilder("title", baseBuilder);

    Assert.That(
      builderSameTitle.Build().EnumerateAttributes(),
      Is.EquivalentTo(baseBuilder.Build().EnumerateAttributes())
    );

    var builderDifferentTitle = new PluginGraphAttributesBuilder("another title", baseBuilder);

    Assert.That(
      builderDifferentTitle.Build().EnumerateAttributes(),
      Is.Not.EquivalentTo(baseBuilder.Build().EnumerateAttributes())
    );
    Assert.That(
      builderDifferentTitle.Build().EnumerateAttributes().Where(IsNotGraphTitle),
      Is.EquivalentTo(baseBuilder.Build().EnumerateAttributes().Where(IsNotGraphTitle))
    );

    static bool IsNotGraphTitle(string attr)
      => !attr.StartsWith("graph_title ", StringComparison.Ordinal);
  }

  [Test]
  public void Build()
    => Assert.That(
      new PluginGraphAttributesBuilder("title")
        .ShowGraph()
        .WithVerticalLabel("vlabel")
        .WithCategory("sensors")
        .WithSize(200, 400)
        .AddGraphArgument("--arg1")
        .AddGraphArgument("--arg2")
        .Build()
        .EnumerateAttributes(),
      Is
        .EquivalentTo([
          "graph_title title",
          "graph yes",
          "graph_vlabel vlabel",
          "graph_category sensors",
          "graph_width 200",
          "graph_height 400",
          "graph_args --arg1 --arg2",
        ])
        .Using((IEqualityComparer<string>)StringComparer.Ordinal)
    );

  [Test]
  public void ShowGraph()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").ShowGraph(),
      ["graph_title title", "graph yes"]
    );

  [Test]
  public void HideGraph()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").HideGraph(),
      ["graph_title title", "graph no"]
    );

  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", typeof(ArgumentException))]
  [TestCase("\0", typeof(ArgumentException))]
  [TestCase("\0category", typeof(ArgumentException))]
  [TestCase("\ncategory", typeof(ArgumentException))]
  [TestCase("\rcategory", typeof(ArgumentException))]
  [TestCase("\tcategory", typeof(ArgumentException))]
  [TestCase("category\0", typeof(ArgumentException))]
  [TestCase("category\n", typeof(ArgumentException))]
  [TestCase("category\r", typeof(ArgumentException))]
  [TestCase("category\t", typeof(ArgumentException))]
  [TestCase("<category>", typeof(ArgumentException))]
  [TestCase("category/subcategory", typeof(ArgumentException))]
  [TestCase("Category", typeof(ArgumentException))]
  [TestCase("CATEGORY", typeof(ArgumentException))]
  [TestCase("カテゴリ", typeof(ArgumentException))]
  public void WithCategory_ArgumentException(
    string? category,
    Type expectedTypeOfException
  )
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithCategory(category: category!),
      Throws
        .TypeOf(expectedTypeOfException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("category")
    );

  [TestCase("c")]
  [TestCase("category")]
  [TestCase("category0")]
  [TestCase("category.subcategory")]
  public void WithCategory(string category)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithCategory(category: category),
      ["graph_title title", $"graph_category {category}"]
    );

  [TestCase(0)]
  [TestCase(-1)]
  public void WithHeight_ArgumentOutOfRangeException(int height)
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithHeight(height: height),
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With
        .Property(nameof(ArgumentOutOfRangeException.ParamName))
        .EqualTo("height")
    );

  [TestCase(1)]
  [TestCase(100)]
  public void WithHeight(int height)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithHeight(height: height),
      ["graph_title title", $"graph_height {height}"]
    );

  [Test]
  public void WithFieldOrder_ArgumentNullException()
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithFieldOrder(order: null!),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("order")
    );

  [Test]
  public void WithFieldOrder_Empty()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithFieldOrder(order: Array.Empty<string>()),
      ["graph_title title"]
    );

  private static System.Collections.IEnumerable YieldTestCases_WithFieldOrder()
  {
    yield return new object[] {
      new string[] { "field1" },
      "graph_order field1"
    };

    yield return new object[] {
      new string[] { "field1", "field2" },
      "graph_order field1 field2"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_WithFieldOrder))]
  public void WithFieldOrder(IEnumerable<string> order, string expectedGraphOrderAttribute)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithFieldOrder(order: order),
      ["graph_title title", expectedGraphOrderAttribute]
    );

  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", typeof(ArgumentException))]
  public void WithFormatString_ArgumentException(
    string? printf,
    Type expectedTypeOfException
  )
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithFormatString(printf: printf!),
      Throws
        .TypeOf(expectedTypeOfException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("printf")
    );

  [TestCase("x")]
  [TestCase("%e")]
  [TestCase("%7.2f")]
  public void WithFormatString(string printf)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithFormatString(printf: printf),
      ["graph_title title", $"graph_printf {printf}"]
    );

  [Test]
  public void EnableUnitScaling()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").EnableUnitScaling(),
      ["graph_title title", "graph_scale yes"]
    );

  [Test]
  public void DisableUnitScaling()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").DisableUnitScaling(),
      ["graph_title title", "graph_scale no"]
    );

  [TestCaseSource(nameof(YieldTestCases_WithTitle_ArgumentException))]
  public void WithTitle_ArgumentException(string? title, Type expectedTypeOfException)
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithTitle(title: title!),
      Throws
        .TypeOf(expectedTypeOfException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("title")
    );

  [TestCaseSource(nameof(YieldTestCases_WithTitle))]
  public void WithTitle(string title)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithTitle(title),
      [$"graph_title {title}"]
    );

  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", typeof(ArgumentException))]
  public void WithTotal_ArgumentException(
    string? labelForTotal,
    Type expectedTypeOfException
  )
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithTotal(labelForTotal: labelForTotal!),
      Throws
        .TypeOf(expectedTypeOfException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("labelForTotal")
    );

  [TestCase("t")]
  [TestCase("total")]
  public void WithTotal(string labelForTotal)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithTotal(labelForTotal: labelForTotal),
      ["graph_title title", $"graph_total {labelForTotal}"]
    );

  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", typeof(ArgumentException))]
  public void WithVerticalLabel_ArgumentException(
    string? verticalLabel,
    Type expectedTypeOfException
  )
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithVerticalLabel(verticalLabel: verticalLabel!),
      Throws
        .TypeOf(expectedTypeOfException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("verticalLabel")
    );

  [TestCase("t")]
  [TestCase("total")]
  public void WithVerticalLabel(string verticalLabel)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithVerticalLabel(verticalLabel: verticalLabel),
      ["graph_title title", $"graph_vlabel {verticalLabel}"]
    );

  [TestCase(0)]
  [TestCase(-1)]
  public void WithWidth_ArgumentOutOfRangeException(int width)
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithWidth(width: width),
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With
        .Property(nameof(ArgumentOutOfRangeException.ParamName))
        .EqualTo("width")
    );

  [TestCase(1)]
  [TestCase(100)]
  public void WithWidth(int width)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithWidth(width: width),
      ["graph_title title", $"graph_width {width}"]
    );

  [TestCase(-1)]
  [TestCase(0)]
  [TestCase(1)]
  [TestCase(999)]
  public void WithUpdateRate_ArgumentOutOfRangeException(int updateRateInMilliseconds)
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithUpdateRate(updateRate: TimeSpan.FromMilliseconds(updateRateInMilliseconds)),
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With
        .Property(nameof(ArgumentOutOfRangeException.ParamName))
        .EqualTo("updateRate")
    );

  [TestCase(1)]
  [TestCase(100)]
  [TestCase(300)]
  public void WithUpdateRate(int updateRateInSeconds)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithUpdateRate(updateRate: TimeSpan.FromSeconds(updateRateInSeconds)),
      ["graph_title title", $"update_rate {updateRateInSeconds}"]
    );

  [TestCase(null!, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", typeof(ArgumentException))]
  [TestCase("undefined", typeof(ArgumentException))]
  [TestCase("NORMAL", typeof(ArgumentException))]
  [TestCase("HUGE", typeof(ArgumentException))]
  [TestCase("custom", typeof(ArgumentException))]
  [TestCase("CUSTOM", typeof(ArgumentException))]
  public void WithGraphDataSize_ArgumentOutOfRangeException(string graphDataSize, Type expectedTypeOfException)
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithGraphDataSize(graphDataSize: graphDataSize),
      Throws
        .TypeOf(expectedTypeOfException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("graphDataSize")
    );

  [TestCase("normal")]
  [TestCase("huge")]
  [TestCase("custom 576, 6 432, 24 540, 288 450")] // normal, in computer-readable format
  [TestCase("custom 115200")] // huge, in computer-readable format
  [TestCase("custom 2d, 30m for 9d, 2h for 45d, 1d for 450d")] // normal, in human-readable format
  [TestCase("custom 400d")] // huge, in human-readable format
  [TestCase("custom ?")]
  public void WithGraphDataSize(string graphDataSize)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithGraphDataSize(graphDataSize: graphDataSize),
      ["graph_title title", $"graph_data_size {graphDataSize}"]
    );

  [Test]
  public void WithCategoryOther()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithCategoryOther(),
      ["graph_title title", $"graph_category other"]
    );

  private static System.Collections.IEnumerable YieldTestCases_WithCategory_WellKnownCategory()
  {
    foreach (var testCase in new[] {
      new { Category = WellKnownCategory.OneSec, StringRepresentation = "1sec" },
      new { Category = WellKnownCategory.AntiVirus, StringRepresentation = "antivirus" },
      new { Category = WellKnownCategory.ApplicationServer, StringRepresentation = "appserver" },
      new { Category = WellKnownCategory.AuthenticationServer, StringRepresentation = "auth" },
      new { Category = WellKnownCategory.Backup, StringRepresentation = "backup" },
      new { Category = WellKnownCategory.MessagingServer, StringRepresentation = "chat" },
      new { Category = WellKnownCategory.Cloud, StringRepresentation = "cloud" },
      new { Category = WellKnownCategory.ContentManagementSystem, StringRepresentation = "cms" },
      new { Category = WellKnownCategory.Cpu, StringRepresentation = "cpu" },
      new { Category = WellKnownCategory.DatabaseServer, StringRepresentation = "db" },
      new { Category = WellKnownCategory.DevelopmentTool, StringRepresentation = "devel" },
      new { Category = WellKnownCategory.Disk, StringRepresentation = "disk" },
      new { Category = WellKnownCategory.Dns, StringRepresentation = "dns" },
      new { Category = WellKnownCategory.FileTransfer, StringRepresentation = "filetransfer" },
      new { Category = WellKnownCategory.Forum, StringRepresentation = "forum" },
      new { Category = WellKnownCategory.FileSystem, StringRepresentation = "fs" },
      new { Category = WellKnownCategory.NetworkFiltering, StringRepresentation = "fw" },
      new { Category = WellKnownCategory.GameServer, StringRepresentation = "games" },
      new { Category = WellKnownCategory.HighThroughputComputing, StringRepresentation = "htc" },
      new { Category = WellKnownCategory.LoadBalancer, StringRepresentation = "loadbalancer" },
      new { Category = WellKnownCategory.Mail, StringRepresentation = "mail" },
      new { Category = WellKnownCategory.MailingList, StringRepresentation = "mailinglist" },
      new { Category = WellKnownCategory.Memory, StringRepresentation = "memory" },
      new { Category = WellKnownCategory.Munin, StringRepresentation = "munin" },
      new { Category = WellKnownCategory.Network, StringRepresentation = "network" },
      new { Category = WellKnownCategory.Other, StringRepresentation = "other" },
      new { Category = WellKnownCategory.Printing, StringRepresentation = "printing" },
      new { Category = WellKnownCategory.Process, StringRepresentation = "processes" },
      new { Category = WellKnownCategory.Radio, StringRepresentation = "radio" },
      new { Category = WellKnownCategory.StorageAreaNetwork, StringRepresentation = "san" },
      new { Category = WellKnownCategory.Search, StringRepresentation = "search" },
      new { Category = WellKnownCategory.Security, StringRepresentation = "security" },
      new { Category = WellKnownCategory.Sensor, StringRepresentation = "sensors" },
      new { Category = WellKnownCategory.SpamFilter, StringRepresentation = "spamfilter" },
      new { Category = WellKnownCategory.Streaming, StringRepresentation = "streaming" },
      new { Category = WellKnownCategory.System, StringRepresentation = "system" },
      new { Category = WellKnownCategory.TimeSynchronization, StringRepresentation = "time" },
      new { Category = WellKnownCategory.Video, StringRepresentation = "tv" },
      new { Category = WellKnownCategory.Virtualization, StringRepresentation = "virtualization" },
      new { Category = WellKnownCategory.VoIP, StringRepresentation = "voip" },
      new { Category = WellKnownCategory.WebServer, StringRepresentation = "webserver" },
      new { Category = WellKnownCategory.Wiki, StringRepresentation = "wiki" },
      new { Category = WellKnownCategory.Wireless, StringRepresentation = "wireless" },
    }) {
      yield return new object[] { testCase.Category, $"graph_category {testCase.StringRepresentation}" };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_WithCategory_WellKnownCategory))]
  public void WithCategory_WellKnownCategory(
    WellKnownCategory category,
    string expectedGraphCategoryAttribute
  )
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithCategory(category),
      ["graph_title title", expectedGraphCategoryAttribute]
    );

  [TestCase(-1, -1, "width")]
  [TestCase(-1, 0, "width")]
  [TestCase(0, -1, "width")]
  [TestCase(0, 0, "width")]
  [TestCase(1, -1, "height")]
  [TestCase(1, 0, "height")]
  public void WithSize_ArgumentOutOfRangeException(int width, int height, string expectedExceptionParamName)
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .WithSize(width: width, height: height),
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With
        .Property(nameof(ArgumentOutOfRangeException.ParamName))
        .EqualTo(expectedExceptionParamName)
    );

  [TestCase(1, 1)]
  [TestCase(100, 100)]
  public void WithSize(int width, int height)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithSize(width: width, height: height),
      ["graph_title title", $"graph_width {width}", $"graph_height {height}"]
    );
}
