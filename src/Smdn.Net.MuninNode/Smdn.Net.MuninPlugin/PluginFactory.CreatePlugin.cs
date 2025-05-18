// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0040
partial class PluginFactory {
#pragma warning restore IDE0040
  /// <summary>Create a plugin with one field which fetches the value from a delegate.</summary>
  public static IPlugin CreatePlugin(
    string name,
    string fieldLabel,
    Func<double?> fetchFieldValue,
    IPluginGraphAttributes graphAttributes
  )
    => CreatePlugin(
      name: name,
      fieldLabel: fieldLabel,
      fieldGraphStyle: PluginFieldGraphStyle.Default,
      fetchFieldValue: fetchFieldValue,
      graphAttributes: graphAttributes
    );

  /// <summary>Create a plugin with one field which fetches the value from a delegate.</summary>
  public static IPlugin CreatePlugin(
    string name,
    string fieldLabel,
    PluginFieldGraphStyle fieldGraphStyle,
    Func<double?> fetchFieldValue,
    IPluginGraphAttributes graphAttributes
  )
    => CreatePlugin(
      name: name,
      graphAttributes: graphAttributes,
      field: new ValueFromFuncPluginField(
        label: fieldLabel,
        name: null,
        graphStyle: fieldGraphStyle,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        negativeFieldName: null,
        fetchValue: fetchFieldValue
      )
    );

  /// <summary>Create a plugin which has one field.</summary>
  public static IPlugin CreatePlugin(
    string name,
    IPluginGraphAttributes graphAttributes,
    PluginFieldBase field
  )
    => CreatePlugin(
      name: name,
      graphAttributes: graphAttributes,
      fields: new[] { field ?? throw new ArgumentNullException(nameof(field)) }
    );

  /// <summary>Create a plugin which has multiple fields.</summary>
  public static IPlugin CreatePlugin(
    string name,
    IPluginGraphAttributes graphAttributes,
    IReadOnlyCollection<PluginFieldBase> fields
  )
    => new DefaultPlugin(
      name: name,
      graphAttributes: graphAttributes,
      fields: fields ?? throw new ArgumentNullException(nameof(fields))
    );

  /// <summary>Create a plugin which has multiple fields.</summary>
  public static IPlugin CreatePlugin(
    string name,
    IPluginGraphAttributes graphAttributes,
    IReadOnlyCollection<IPluginField> fields
  )
    => new DefaultPlugin(
      name: name,
      graphAttributes: graphAttributes,
      fields: fields ?? throw new ArgumentNullException(nameof(fields))
    );
}
