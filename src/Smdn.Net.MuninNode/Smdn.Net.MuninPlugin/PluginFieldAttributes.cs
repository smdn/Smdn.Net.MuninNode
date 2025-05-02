// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Represents attributes related to the drawing of a single field.
/// Defines field attributes that should be returned when the plugin is called with the 'config' argument.
/// This type represents the collection of 'field name attributes'.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes">Plugin reference - Field name attributes</seealso>
public readonly struct PluginFieldAttributes {
  /// <summary>Gets a value for the <c>{fieldname}.label</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-label">Plugin reference - Field name attributes - {fieldname}.label</seealso>
  public string Label { get; }

  /// <summary>Gets a value for the <c>{fieldname}.draw</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-draw">Plugin reference - Field name attributes - {fieldname}.draw</seealso>
  /// <seealso cref="PluginFieldGraphStyle"/>
  public PluginFieldGraphStyle GraphStyle { get; }

  /// <summary>Gets a value for the <c>{fieldname}.warning</c>.</summary>
  /// <remarks>This property defines the upper limit, lower limit, or range of normal value, that is not treated as warning.</remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-warning">Plugin reference - Field name attributes - {fieldname}.warning</seealso>
  /// <seealso cref="PluginFieldNormalValueRange"/>
  public PluginFieldNormalValueRange NormalRangeForWarning { get; }

  /// <summary>Gets a value for the <c>{fieldname}.critical</c>.</summary>
  /// <remarks>This property defines the upper limit, lower limit, or range of normal value, that is not treated as critical.</remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-critical">Plugin reference - Field name attributes - {fieldname}.critical</seealso>
  /// <seealso cref="PluginFieldNormalValueRange"/>
  public PluginFieldNormalValueRange NormalRangeForCritical { get; }

  /// <summary>Gets a value for the <c>{fieldname}.negative</c>.</summary>
  /// <remarks>
  /// This property specifies that the specified field is drawn as the negative side of this field.
  /// If a valid field name is specified for this property, it also implicitly sets the attribute <c>{fieldname}.graph no</c>.
  /// </remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-negative">Plugin reference - Field name attributes - {fieldname}.critical</seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/develop/plugins/plugin-bcp.html#plugin-bcp-direction">Best Current Practices for good plugin graphs - Direction</seealso>
  public string? NegativeFieldName { get; }

  public PluginFieldAttributes(
    string label,
    PluginFieldGraphStyle graphStyle = PluginFieldGraphStyle.Default
  )
    : this(
      label: label,
      graphStyle: graphStyle,
      normalRangeForWarning: default,
      normalRangeForCritical: default,
      negativeFieldName: null
    )
  {
  }

  public PluginFieldAttributes(
    string label,
    PluginFieldGraphStyle graphStyle = PluginFieldGraphStyle.Default,
    PluginFieldNormalValueRange normalRangeForWarning = default,
    PluginFieldNormalValueRange normalRangeForCritical = default
  )
    : this(
      label: label,
      graphStyle: graphStyle,
      normalRangeForWarning: normalRangeForWarning,
      normalRangeForCritical: normalRangeForCritical,
      negativeFieldName: null
    )
  {
  }

  public PluginFieldAttributes(
    string label,
    PluginFieldGraphStyle graphStyle,
    PluginFieldNormalValueRange normalRangeForWarning,
    PluginFieldNormalValueRange normalRangeForCritical,
    string? negativeFieldName
  )
  {
    ArgumentExceptionShim.ThrowIfNullOrEmpty(label, nameof(label));

    Label = label;
    GraphStyle = graphStyle;
    NormalRangeForWarning = normalRangeForWarning;
    NormalRangeForCritical = normalRangeForCritical;
    NegativeFieldName = negativeFieldName;
  }
}
