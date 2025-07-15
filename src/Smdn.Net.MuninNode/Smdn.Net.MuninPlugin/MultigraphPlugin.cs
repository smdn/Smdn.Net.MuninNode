// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

public class MultigraphPlugin : IMultigraphPlugin {
  /// <inheritdoc cref="IPlugin.Name"/>
  public string Name { get; }

  /// <inheritdoc cref="IMultigraphPlugin.Plugins"/>
  public IReadOnlyCollection<IPlugin> Plugins { get; }

  /// <inheritdoc cref="IPlugin.DataSource"/>
  public IPluginDataSource DataSource => throw new NotSupportedException();

  /// <inheritdoc cref="IPlugin.GraphAttributes"/>
  public IPluginGraphAttributes GraphAttributes => throw new NotSupportedException();

  /// <inheritdoc cref="IPlugin.SessionCallback"/>
#pragma warning disable CS0618
  [Obsolete(message: INodeSessionCallback.ObsoleteMessage.SessionCallbackProperty)]
#pragma warning restore CS0618
  public INodeSessionCallback? SessionCallback => throw new NotSupportedException();

  public MultigraphPlugin(string name, IReadOnlyCollection<IPlugin> plugins)
  {
    ArgumentExceptionShim.ThrowIfNullOrWhiteSpace(name, nameof(name));

    Name = name;
    Plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
  }
}
