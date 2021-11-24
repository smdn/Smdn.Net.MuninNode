// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Text.RegularExpressions;

namespace Smdn.Net.MuninPlugin {
  public readonly struct PluginField {
    public string Name { get; }
    public string Label { get; }
    /// <summary>A value for plugin field.</summary>
    /// <remarks/>Reports 'UNKNOWN' as a plugin field value if <see cref="Value"/> is <see langword="null"/>.<remarks>
    public double? Value { get; }
    public string GraphStyle { get; }

    internal string FormattedValueString => Value.HasValue
      ? Value.Value.ToString() // TODO: format specifier
      : "U" /* UNKNOWN */;

    public static PluginField CreateUnknownValueField(
      string label,
      string graphStyle = null
    )
      => new(
        name: GetDefaultNameFromLabel(label),
        label: label,
        value: (double?)null,
        graphStyle: graphStyle
      );

    public static PluginField CreateUnknownValueField(
      string name,
      string label,
      string graphStyle = null
    )
      => new(
        name: name,
        label: label,
        value: (double?)null,
        graphStyle
      );

    public PluginField(
      string label,
      double value,
      string graphStyle = null
    )
      : this(GetDefaultNameFromLabel(label), label, (double?)value, graphStyle)
    {
    }

    public PluginField(
      string name,
      string label,
      double value,
      string graphStyle = null
    )
      : this(name, label, (double?)value, graphStyle)
    {
    }

    private PluginField(
      string name,
      string label,
      double? value,
      string graphStyle = null
    )
    {
      if (name == null)
        throw new ArgumentNullException(nameof(name));
      if (name.Length == 0)
        throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(name));

      this.Name = name;
      this.Label = label ?? name;
      this.Value = value;
      this.GraphStyle = graphStyle;
    }

    private static readonly Regex regexSpecialChars = new Regex(
      pattern: $@"[{Regex.Escape("-+/\\~_ ")}]+",
      options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static string GetDefaultNameFromLabel(string label)
    {
      if (label == null)
        throw new ArgumentNullException(nameof(label));
      if (label.Length == 0)
        throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));

      return regexSpecialChars.Replace(label, "_").ToLowerInvariant();
    }
  }
}