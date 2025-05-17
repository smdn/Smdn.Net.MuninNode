// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0040
partial class PluginGraphAttributesBuilderTests {
#pragma warning restore IDE0040
  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", typeof(ArgumentException))]
  public void AddGraphArgument_ArgumentException(
    string? argument,
    Type expectedTypeOfException
  )
    => Assert.That(
      () => new PluginGraphAttributesBuilder("title")
        .AddGraphArgument(argument: argument!),
      Throws
        .TypeOf(expectedTypeOfException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("argument")
    );

  [TestCase("a")]
  [TestCase("a a")]
  [TestCase("--arg")]
  [TestCase("--arg --arg")]
  public void AddGraphArgument(string argument)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").AddGraphArgument(argument: argument),
      ["graph_title title", $"graph_args {argument}"]
    );

  [TestCase("a", "b")]
  [TestCase("--arg0", "--arg1")]
  public void AddGraphArgument_Multiple(string argument0, string argument1)
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title")
        .AddGraphArgument(argument: argument0)
        .AddGraphArgument(argument: argument1),
      ["graph_title title", $"graph_args {argument0} {argument1}"]
    );

  [Test]
  public void AddGraphArgument_ClearGraphArguments()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title")
        .AddGraphArgument("1")
        .ClearGraphArguments()
        .AddGraphArgument("2")
        .AddGraphArgument("3"),
      ["graph_title title", "graph_args 2 3"]
    );

  [Test]
  public void ClearGraphArguments()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title")
        .AddGraphArgument("1")
        .AddGraphArgument("2")
        .AddGraphArgument("3")
        .ClearGraphArguments(),
      ["graph_title title"]
    );

  [Test]
  public void WithGraphLowerLimit(
    [Values(-1.0, 0.0, 1.0)] double value
  )
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithGraphLowerLimit(value: value),
      ["graph_title title", $"graph_args --lower-limit {value}"]
    );

  [Test]
  public void WithGraphUpperLimit(
    [Values(-1.0, 0.0, 1.0)] double value
  )
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithGraphUpperLimit(value: value),
      ["graph_title title", $"graph_args --upper-limit {value}"]
    );

  [Test]
  public void WithGraphUpperLimit(
    [Values(-1.0, 0.0, 1.0)] double lowerLimitValue,
    [Values(-1.0, 0.0, 1.0)] double upperLimitValue
  )
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithGraphLimit(lowerLimitValue: lowerLimitValue, upperLimitValue: upperLimitValue),
      ["graph_title title", $"graph_args --lower-limit {lowerLimitValue} --upper-limit {upperLimitValue}"]
    );

  [Test]
  public void WithGraphRigid()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithGraphRigid(),
      ["graph_title title", "graph_args --rigid"]
    );

  [Test]
  public void WithGraphDecimalBase()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithGraphDecimalBase(),
      ["graph_title title", "graph_args --base 1000"]
    );

  [Test]
  public void WithGraphBinaryBase()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithGraphBinaryBase(),
      ["graph_title title", "graph_args --base 1024"]
    );

  [Test]
  public void WithGraphLogarithmic()
    => AssertBuiltGraphAttributes(
      new PluginGraphAttributesBuilder("title").WithGraphLogarithmic(),
      ["graph_title title", "graph_args --logarithmic"]
    );
}
