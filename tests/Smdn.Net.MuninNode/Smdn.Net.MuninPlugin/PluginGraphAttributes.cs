// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.MuninPlugin;

[TestFixture]
public class PluginGraphAttributesTests {
  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", null)]
  public void Ctor_Title(string? title, Type? expectedArgumentExceptionType)
    => Assert.That(
      () => new PluginGraphAttributes(
        title: title!,
        category: "category",
        verticalLabel: "verticalLabel",
        scale: true,
        arguments: "arguments",
        updateRate: null,
        width: null,
        height: null,
        order: null,
        totalValueLabel: null
      ),
      expectedArgumentExceptionType is null
        ? Throws.Nothing
        : Throws
            .TypeOf(expectedArgumentExceptionType)
            .With
            .Property(nameof(ArgumentException.ParamName))
            .EqualTo("title")
    );

  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", null)]
  public void Ctor_Category(string? category, Type? expectedArgumentExceptionType)
    => Assert.That(
      () => new PluginGraphAttributes(
        title: "title",
        category: category!,
        verticalLabel: "verticalLabel",
        scale: true,
        arguments: "arguments",
        updateRate: null,
        width: null,
        height: null,
        order: null,
        totalValueLabel: null
      ),
      expectedArgumentExceptionType is null
        ? Throws.Nothing
        : Throws
            .TypeOf(expectedArgumentExceptionType)
            .With
            .Property(nameof(ArgumentException.ParamName))
            .EqualTo("category")
    );

  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", null)]
  public void Ctor_VerticalLabel(string? verticalLabel, Type? expectedArgumentExceptionType)
    => Assert.That(
      () => new PluginGraphAttributes(
        title: "title",
        category: "category",
        verticalLabel: verticalLabel!,
        scale: true,
        arguments: "arguments",
        updateRate: null,
        width: null,
        height: null,
        order: null,
        totalValueLabel: null
      ),
      expectedArgumentExceptionType is null
        ? Throws.Nothing
        : Throws
            .TypeOf(expectedArgumentExceptionType)
            .With
            .Property(nameof(ArgumentException.ParamName))
            .EqualTo("verticalLabel")
    );

  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  [TestCase(" ", null)]
  public void Ctor_Arguments(string? arguments, Type? expectedArgumentExceptionType)
    => Assert.That(
      () => new PluginGraphAttributes(
        title: "title",
        category: "category",
        verticalLabel: "verticalLabel",
        scale: true,
        arguments: arguments!,
        updateRate: null,
        width: null,
        height: null,
        order: null,
        totalValueLabel: null
      ),
      expectedArgumentExceptionType is null
        ? Throws.Nothing
        : Throws
            .TypeOf(expectedArgumentExceptionType)
            .With
            .Property(nameof(ArgumentException.ParamName))
            .EqualTo("arguments")
    );

  [TestCase(null, null)]
  [TestCase(-1, typeof(ArgumentOutOfRangeException))]
  [TestCase(0, typeof(ArgumentOutOfRangeException))]
  [TestCase(1, null)]
  public void Ctor_Width(int? width, Type? expectedArgumentExceptionType)
    => Assert.That(
      () => new PluginGraphAttributes(
        title: "title",
        category: "category",
        verticalLabel: "verticalLabel",
        scale: true,
        arguments: "arguments",
        updateRate: null,
        width: width,
        height: null,
        order: null,
        totalValueLabel: null
      ),
      expectedArgumentExceptionType is null
        ? Throws.Nothing
        : Throws
            .TypeOf(expectedArgumentExceptionType)
            .With
            .Property(nameof(ArgumentException.ParamName))
            .EqualTo("width")
    );

  [TestCase(null, null)]
  [TestCase(-1, typeof(ArgumentOutOfRangeException))]
  [TestCase(0, typeof(ArgumentOutOfRangeException))]
  [TestCase(1, null)]
  public void Ctor_Height(int? height, Type? expectedArgumentExceptionType)
    => Assert.That(
      () => new PluginGraphAttributes(
        title: "title",
        category: "category",
        verticalLabel: "verticalLabel",
        scale: true,
        arguments: "arguments",
        updateRate: null,
        width: null,
        height: height,
        order: null,
        totalValueLabel: null
      ),
      expectedArgumentExceptionType is null
        ? Throws.Nothing
        : Throws
            .TypeOf(expectedArgumentExceptionType)
            .With
            .Property(nameof(ArgumentException.ParamName))
            .EqualTo("height")
    );
}
