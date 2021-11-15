// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Text.RegularExpressions;

namespace Smdn.Net.MuninPlugin {
  public readonly struct PluginField {
    public string ID { get; }
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
        id: GetDefaultIDFromLabel(label),
        label: label,
        value: (double?)null,
        graphStyle: graphStyle
      );

    public static PluginField CreateUnknownValueField(
      string id,
      string label,
      string graphStyle = null
    )
      => new(
        id: id,
        label: label,
        value: (double?)null,
        graphStyle
      );

    public PluginField(
      string label,
      double value,
      string graphStyle
    )
      : this(GetDefaultIDFromLabel(label), label, (double?)value, graphStyle)
    {
    }

    public PluginField(
      string id,
      string label,
      double value,
      string graphStyle = null
    )
      : this(id, label, (double?)value, graphStyle)
    {
    }

    private PluginField(
      string id,
      string label,
      double? value,
      string graphStyle = null
    )
    {
      if (id == null)
        throw new ArgumentNullException(nameof(id));
      if (id.Length == 0)
        throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(id));

      this.ID = id;
      this.Label = label ?? id;
      this.Value = value;
      this.GraphStyle = graphStyle;
    }

    private static readonly Regex regexSpecialChars = new Regex(
      pattern: $@"[{Regex.Escape("-+/\\~_ ")}]+",
      options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static string GetDefaultIDFromLabel(string label)
    {
      if (label == null)
        throw new ArgumentNullException(nameof(label));
      if (label.Length == 0)
        throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));

      return regexSpecialChars.Replace(label, "_").ToLowerInvariant();
    }
  }
}