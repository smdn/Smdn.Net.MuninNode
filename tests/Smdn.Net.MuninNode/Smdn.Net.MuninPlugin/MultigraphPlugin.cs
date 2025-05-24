// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.MuninPlugin;

[TestFixture]
public class MultigraphPluginTests {
  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", typeof(ArgumentException))]
  [TestCase("name", null)]
  public void Ctor_Name(string? name, Type? expectedArgumentExceptionType)
    => Assert.That(
      () => new MultigraphPlugin(
        name: name!,
        plugins: []
      ),
      expectedArgumentExceptionType is null
        ? Throws.Nothing
        : Throws
            .TypeOf(expectedArgumentExceptionType)
            .With
            .Property(nameof(ArgumentException.ParamName))
            .EqualTo("name")
    );

  [TestCase]
  public void Ctor_Plugins_ArgumentNull()
    => Assert.That(
      () => new MultigraphPlugin(
        name: "multigraph",
        plugins: null!
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("plugins")
    );
}
