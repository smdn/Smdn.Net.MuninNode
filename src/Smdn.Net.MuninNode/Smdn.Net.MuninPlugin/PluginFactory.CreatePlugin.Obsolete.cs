// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0040
partial class PluginFactory {
#pragma warning restore IDE0040
  private const string ObsoleteMessageUseOverloadWithIPluginGraphAttributes = "Use overloads that accept IPluginGraphAttributes instead.";

  /// <summary>Create a plugin with one field which fetches the value from a delegate.</summary>
  [Obsolete(ObsoleteMessageUseOverloadWithIPluginGraphAttributes)]
  public static IPlugin CreatePlugin(
    string name,
    string fieldLabel,
    Func<double?> fetchFieldValue,
    PluginGraphAttributes graphAttributes
  )
    => CreatePlugin(
      name: name,
      fieldLabel: fieldLabel,
      fetchFieldValue: fetchFieldValue,
      graphAttributes: (IPluginGraphAttributes)graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes))
    );

  /// <summary>Create a plugin with one field which fetches the value from a delegate.</summary>
  [Obsolete(ObsoleteMessageUseOverloadWithIPluginGraphAttributes)]
  public static IPlugin CreatePlugin(
    string name,
    string fieldLabel,
    PluginFieldGraphStyle fieldGraphStyle,
    Func<double?> fetchFieldValue,
    PluginGraphAttributes graphAttributes
  )
    => CreatePlugin(
      name: name,
      fieldLabel: fieldLabel,
      fieldGraphStyle: fieldGraphStyle,
      fetchFieldValue: fetchFieldValue,
      graphAttributes: (IPluginGraphAttributes)graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes))
    );

  /// <summary>Create a plugin which has one field.</summary>
  [Obsolete(ObsoleteMessageUseOverloadWithIPluginGraphAttributes)]
  public static IPlugin CreatePlugin(
    string name,
    PluginGraphAttributes graphAttributes,
    PluginFieldBase field
  )
    => CreatePlugin(
      name: name,
      graphAttributes: (IPluginGraphAttributes)graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes)),
      field: field
    );

  /// <summary>Create a plugin which has multiple fields.</summary>
  [Obsolete(ObsoleteMessageUseOverloadWithIPluginGraphAttributes)]
  public static IPlugin CreatePlugin(
    string name,
    PluginGraphAttributes graphAttributes,
    IReadOnlyCollection<PluginFieldBase> fields
  )
    => CreatePlugin(
      name: name,
      graphAttributes: (IPluginGraphAttributes)graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes)),
      fields: fields
    );

  /// <summary>Create a plugin which has multiple fields.</summary>
  [Obsolete(ObsoleteMessageUseOverloadWithIPluginGraphAttributes)]
  public static IPlugin CreatePlugin(
    string name,
    PluginGraphAttributes graphAttributes,
    IReadOnlyCollection<IPluginField> fields
  )
    => CreatePlugin(
      name: name,
      graphAttributes: (IPluginGraphAttributes)graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes)),
      fields: fields
    );
}
