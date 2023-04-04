// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0040
partial class PluginFactory {
#pragma warning restore IDE0040
  public static IPluginField CreateField(string label, Func<double?> fetchValue)
    => CreateField(label, PluginFieldGraphStyle.Default, fetchValue);

  public static IPluginField CreateField(string label, PluginFieldGraphStyle graphStyle, Func<double?> fetchValue)
    => new ValueFromFuncPluginField(
      label: label,
      name: null,
      graphStyle: graphStyle,
      fetchValue: fetchValue
    );

  private sealed class ValueFromFuncPluginField : PluginFieldBase {
    private readonly Func<double?> fetchValue;

    public ValueFromFuncPluginField(
      string label,
      Func<double?> fetchValue
    )
      : this(
        label: label,
        name: null,
        graphStyle: PluginFieldGraphStyle.Default,
        fetchValue: fetchValue
      )
    {
    }

    public ValueFromFuncPluginField(
      string label,
      string? name,
      PluginFieldGraphStyle graphStyle,
      Func<double?> fetchValue
    )
      : base(
        label: label,
        name: name,
        graphStyle: graphStyle
      )
    {
      this.fetchValue = fetchValue ?? throw new ArgumentNullException(nameof(fetchValue));
    }

    protected override ValueTask<double?> FetchValueAsync(CancellationToken cancellationToken)
      => new(fetchValue());
  }
}
