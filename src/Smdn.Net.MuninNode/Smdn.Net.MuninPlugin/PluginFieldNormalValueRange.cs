// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

namespace Smdn.Net.MuninPlugin;

public readonly struct PluginFieldNormalValueRange {
  private static double ValidateValue(double val, string paramName)
  {
    if (double.IsNaN(val))
      throw new ArgumentOutOfRangeException(message: "The value must be a finite number.", paramName: paramName);
    if (double.IsInfinity(val))
      throw new ArgumentOutOfRangeException(message: "The value must be a finite number.", paramName: paramName);

    return val;
  }

  private static (double Min, double Max) ValidateRange(double min, string paramNameForMin, double max, string paramNameForMax)
  {
    if (min == max) {
      throw new ArgumentException(
        message: $"The values of '{paramNameForMin}' and '{paramNameForMax}' are equal and do not compose a range.",
        paramName: paramNameForMin
      );
    }

    if (min > max) {
      throw new ArgumentException(
        message: $"The values of '{paramNameForMin}' is greater than '{paramNameForMax}' and do not compose a range.",
        paramName: paramNameForMin
      );
    }

    return (min, max);
  }

  // min:
  public static PluginFieldNormalValueRange CreateMin(double min)
    => new(min: ValidateValue(min, nameof(min)), max: null);

  // :max
  public static PluginFieldNormalValueRange CreateMax(double max)
    => new(min: null, max: ValidateValue(max, nameof(max)));

  // min:max
  public static PluginFieldNormalValueRange CreateRange(double min, double max)
    => new(
      range: ValidateRange(
        min: ValidateValue(min, nameof(min)),
        paramNameForMin: nameof(min),
        max: ValidateValue(max, nameof(max)),
        paramNameForMax: nameof(max)
      )
    );

  public static readonly PluginFieldNormalValueRange None = new(null, null);

  public double? Min { get; }
  public double? Max { get; }
  public bool HasValue => Min.HasValue || Max.HasValue;

  private PluginFieldNormalValueRange(double? min, double? max)
  {
    Min = min;
    Max = max;
  }

  private PluginFieldNormalValueRange((double? Min, double? Max) range)
  {
    Min = range.Min;
    Max = range.Max;
  }
}
