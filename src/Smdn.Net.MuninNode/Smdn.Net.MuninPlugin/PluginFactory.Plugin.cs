// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0040
partial class PluginFactory {
#pragma warning restore IDE0040
  private sealed class DefaultPlugin : IPlugin, IPluginDataSource {
    public string Name { get; }

    public IPluginGraphAttributes GraphAttributes { get; }
    public IReadOnlyCollection<IPluginField> Fields { get; }

    public IPluginDataSource DataSource => this;
    public INodeSessionCallback? SessionCallback => null;

    public DefaultPlugin(
      string name,
      IPluginGraphAttributes graphAttributes,
      IReadOnlyCollection<IPluginField> fields
    )
    {
      ArgumentExceptionShim.ThrowIfNullOrEmpty(name, nameof(name));

      Name = name;
      GraphAttributes = graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes));
      Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }
  }
}
