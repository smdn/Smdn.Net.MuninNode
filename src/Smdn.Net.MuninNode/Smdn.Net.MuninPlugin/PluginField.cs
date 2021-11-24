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
      if (!regexValidFieldName.IsMatch(name))
        throw new ArgumentException($"'{name}' is invalid for field name. The value of {nameof(name)} must match the following regular expression: '{regexValidFieldName}'", nameof(name));

      label ??= name;

      if (label == null)
        throw new ArgumentNullException(nameof(label));
      if (label.Length == 0)
        throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));
      if (!regexValidFieldLabel.IsMatch(label))
        throw new ArgumentException($"'{label}' is invalid for field name. The value of {nameof(label)} must match the following regular expression: '{regexValidFieldLabel}'", nameof(label));

      this.Name = name;
      this.Label = label ?? name;
      this.Value = value;
      this.GraphStyle = graphStyle;
    }

    // http://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes
    // Field name attributes
    //   Attribute:	{fieldname}.label
    //   Value:	anything except # and \
    private static readonly Regex regexValidFieldLabel = new(
      pattern: $@"^[^{Regex.Escape("#\\")}]+$",
      options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    // http://guide.munin-monitoring.org/en/latest/reference/plugin.html#notes-on-field-names
    // Notes on field names
    //   The characters must be [a-zA-Z0-9_], while the first character must be [a-zA-Z_].
    private static readonly Regex regexValidFieldName = new(
      pattern: $@"^[a-zA-Z_][a-zA-Z0-9_]*$",
      options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex regexInvalidFieldNamePrefix = new(
      pattern: $@"^[0-9_]+",
      options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );
    private static readonly Regex regexInvalidFieldNameChars = new(
      pattern: $@"[^a-zA-Z0-9_]",
      options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static string GetDefaultNameFromLabel(string label)
    {
      if (label == null)
        throw new ArgumentNullException(nameof(label));
      if (label.Length == 0)
        throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));

      return regexInvalidFieldNameChars.Replace(
        regexInvalidFieldNamePrefix.Replace(label, string.Empty),
        string.Empty
      );
    }
  }
}