// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.MuninPlugin;

// ref: http://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes
public readonly struct PluginFieldAttributes {
  public string Label { get; }
  public PluginFieldGraphStyle GraphStyle { get; }

  public PluginFieldNormalValueRange NormalRangeForWarning { get; }
  public PluginFieldNormalValueRange NormalRangeForCritical { get; }

  public PluginFieldAttributes(
    string label,
    PluginFieldGraphStyle graphStyle = PluginFieldGraphStyle.Default
  )
    : this(
      label: label,
      graphStyle: graphStyle,
      normalRangeForWarning: default,
      normalRangeForCritical: default
    )
  {
  }

  public PluginFieldAttributes(
    string label,
    PluginFieldGraphStyle graphStyle = PluginFieldGraphStyle.Default,
    PluginFieldNormalValueRange normalRangeForWarning = default,
    PluginFieldNormalValueRange normalRangeForCritical = default
  )
  {
    if (label is null)
      throw new ArgumentNullException(nameof(label));
    if (label.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));

    Label = label;
    GraphStyle = graphStyle;
    NormalRangeForWarning = normalRangeForWarning;
    NormalRangeForCritical = normalRangeForCritical;
  }
}
