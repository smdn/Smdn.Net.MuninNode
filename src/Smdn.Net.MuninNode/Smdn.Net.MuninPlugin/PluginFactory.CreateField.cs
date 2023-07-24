// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0040
partial class PluginFactory {
#pragma warning restore IDE0040
  public static IPluginField CreateField(
    string label,
    Func<double?> fetchValue
  )
    => new ValueFromFuncPluginField(
      label: label,
      name: null,
      graphStyle: PluginFieldGraphStyle.Default,
      normalRangeForWarning: PluginFieldNormalValueRange.None,
      normalRangeForCritical: PluginFieldNormalValueRange.None,
      fetchValue: fetchValue
    );

  public static IPluginField CreateField(
    string label,
    PluginFieldGraphStyle graphStyle,
    Func<double?> fetchValue
  )
    => new ValueFromFuncPluginField(
      label: label,
      name: null,
      graphStyle: graphStyle,
      normalRangeForWarning: PluginFieldNormalValueRange.None,
      normalRangeForCritical: PluginFieldNormalValueRange.None,
      fetchValue: fetchValue
    );

  public static IPluginField CreateField(
    string label,
    PluginFieldGraphStyle graphStyle,
    PluginFieldNormalValueRange normalRangeForWarning,
    PluginFieldNormalValueRange normalRangeForCritical,
    Func<double?> fetchValue
  )
    => new ValueFromFuncPluginField(
      label: label,
      name: null,
      graphStyle: graphStyle,
      normalRangeForWarning: normalRangeForWarning,
      normalRangeForCritical: normalRangeForCritical,
      fetchValue: fetchValue
    );

  public static IPluginField CreateField(
    string name,
    string label,
    PluginFieldGraphStyle graphStyle,
    PluginFieldNormalValueRange normalRangeForWarning,
    PluginFieldNormalValueRange normalRangeForCritical,
    Func<double?> fetchValue
  )
    => new ValueFromFuncPluginField(
      label: label,
      name: name,
      graphStyle: graphStyle,
      normalRangeForWarning: normalRangeForWarning,
      normalRangeForCritical: normalRangeForCritical,
      fetchValue: fetchValue
    );

  private sealed class ValueFromFuncPluginField : PluginFieldBase {
    private readonly Func<double?> fetchValue;

    public ValueFromFuncPluginField(
      string label,
      string? name,
      PluginFieldGraphStyle graphStyle,
      PluginFieldNormalValueRange normalRangeForWarning,
      PluginFieldNormalValueRange normalRangeForCritical,
      Func<double?> fetchValue
    )
      : base(
        label: label,
        name: name,
        graphStyle: graphStyle,
        normalRangeForWarning: normalRangeForWarning,
        normalRangeForCritical: normalRangeForCritical
      )
    {
      this.fetchValue = fetchValue ?? throw new ArgumentNullException(nameof(fetchValue));
    }

    protected override ValueTask<double?> FetchValueAsync(CancellationToken cancellationToken)
      => new(fetchValue());
  }
}
