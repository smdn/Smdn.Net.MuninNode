// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Represents attributes related to the drawing of a single field.
/// Defines field attributes that should be returned when the plugin is called with the 'config' argument.
/// This type represents the collection of 'field name attributes'.
/// </summary>
/// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes">Plugin reference - Field name attributes</seealso>
public readonly struct PluginFieldAttributes {
  /// <summary>Gets a value for the <c>{fieldname}.label</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-label">Plugin reference - Field name attributes - {fieldname}.label</seealso>
  public string Label { get; }

  /// <summary>Gets a value for the <c>{fieldname}.draw</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-draw">Plugin reference - Field name attributes - {fieldname}.draw</seealso>
  /// <seealso cref="PluginFieldGraphStyle"/>
  public PluginFieldGraphStyle GraphStyle { get; }

  /// <summary>Gets a value for the <c>{fieldname}.warning</c>.</summary>
  /// <remarks>This property defines the upper limit, lower limit, or range of normal value, that is not treated as warning.</remarks>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-warning">Plugin reference - Field name attributes - {fieldname}.warning</seealso>
  /// <seealso cref="PluginFieldNormalValueRange"/>
  public PluginFieldNormalValueRange NormalRangeForWarning { get; }

  /// <summary>Gets a value for the <c>{fieldname}.critical</c>.</summary>
  /// <remarks>This property defines the upper limit, lower limit, or range of normal value, that is not treated as critical.</remarks>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-critical">Plugin reference - Field name attributes - {fieldname}.critical</seealso>
  /// <seealso cref="PluginFieldNormalValueRange"/>
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
