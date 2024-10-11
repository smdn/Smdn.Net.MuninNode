// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

using NUnit.Framework;

namespace Smdn.Net.MuninPlugin;

[TestFixture]
public class PluginFieldNormalValueRangeTests {
  private static System.Collections.IEnumerable YieldTestCases_CreateMin_CreateMax()
  {
    yield return new object?[] { 0.0, null };
    yield return new object?[] { 1.0, null };
    yield return new object?[] { -1.0, null };
    yield return new object?[] { double.MaxValue, null };
    yield return new object?[] { double.MinValue, null };
    yield return new object?[] { double.PositiveInfinity, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { double.NegativeInfinity, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { double.NaN, typeof(ArgumentOutOfRangeException) };
  }

  [TestCaseSource(nameof(YieldTestCases_CreateMin_CreateMax))]
  public void CreateMin(double value, Type? typeOfExpectedException)
  {
    PluginFieldNormalValueRange range = default;

    Assert.That(
      () => range = PluginFieldNormalValueRange.CreateMin(value),
      typeOfExpectedException is null
        ? Throws.Nothing
        : Throws.TypeOf(typeOfExpectedException)
    );

    if (typeOfExpectedException is null) {
      Assert.That(range.Min, Is.Not.Null, nameof(range.Min));
      Assert.That(range.Min!.Value, Is.EqualTo(value), nameof(range.Min));

      Assert.That(range.Max, Is.Null, nameof(range.Max));

      Assert.That(range.HasValue, Is.True, nameof(range.HasValue));
    }
  }

  [TestCaseSource(nameof(YieldTestCases_CreateMin_CreateMax))]
  public void CreateMax(double value, Type? typeOfExpectedException)
  {
    PluginFieldNormalValueRange range = default;

    Assert.That(
      () => range = PluginFieldNormalValueRange.CreateMax(value),
      typeOfExpectedException is null
        ? Throws.Nothing
        : Throws.TypeOf(typeOfExpectedException)
    );

    if (typeOfExpectedException is null) {
      Assert.That(range.Max, Is.Not.Null, nameof(range.Max));
      Assert.That(range.Max!.Value, Is.EqualTo(value), nameof(range.Max));

      Assert.That(range.Min, Is.Null, nameof(range.Min));

      Assert.That(range.HasValue, Is.True, nameof(range.HasValue));
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_CreateRange()
  {
    yield return new object?[] { 0.0, 1.0, null };
    yield return new object?[] { -1.0, 0.0, null };
    yield return new object?[] { 0.0, double.MaxValue, null };
    yield return new object?[] { double.MinValue, 0.0, null };
    yield return new object?[] { double.MinValue, double.MaxValue, null };

    yield return new object?[] { double.PositiveInfinity, 0.0, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { double.NegativeInfinity, 0.0, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { double.NaN, 0.0, typeof(ArgumentOutOfRangeException) };

    yield return new object?[] { 0.0, double.PositiveInfinity, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { 0.0, double.NegativeInfinity, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { 0.0, double.NaN, typeof(ArgumentOutOfRangeException) };

    yield return new object?[] { 0.0, 0.0, typeof(ArgumentException) };
    yield return new object?[] { 1.0, 0.0, typeof(ArgumentException) };
    yield return new object?[] { 0.0, -1.0, typeof(ArgumentException) };
  }

  [TestCaseSource(nameof(YieldTestCases_CreateRange))]
  public void CreateRange(double min, double max, Type? typeOfExpectedException)
  {
    PluginFieldNormalValueRange range = default;

    Assert.That(
      () => range = PluginFieldNormalValueRange.CreateRange(min, max),
      typeOfExpectedException is null
        ? Throws.Nothing
        : Throws.TypeOf(typeOfExpectedException)
    );

    if (typeOfExpectedException is null) {
      Assert.That(range.Min, Is.Not.Null, nameof(range.Min));
      Assert.That(range.Min!.Value, Is.EqualTo(min), nameof(range.Min));

      Assert.That(range.Max, Is.Not.Null, nameof(range.Max));
      Assert.That(range.Max!.Value, Is.EqualTo(max), nameof(range.Max));

      Assert.That(range.HasValue, Is.True, nameof(range.HasValue));
    }
  }
}
