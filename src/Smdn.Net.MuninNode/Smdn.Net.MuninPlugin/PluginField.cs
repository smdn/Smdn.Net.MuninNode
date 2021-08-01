// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Text.RegularExpressions;

namespace Smdn.Net.MuninPlugin {
  public readonly struct PluginField {
    public string ID { get; }
    public string Label { get; }
    public double Value { get; }
    public string GraphStyle { get; }

    public PluginField(
      string label,
      double value,
      string graphStyle
    )
      : this(GetDefaultIDFromLabel(label), label, value, graphStyle)
    {
    }

    public PluginField(
      string id,
      string label,
      double value,
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